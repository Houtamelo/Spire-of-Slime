using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.Types.Stun;
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;

namespace Core.Main_Characters.Ethel.Combat.Perks.Debuffer
{
    public class GoForTheEyes : PerkScriptable
    {
        public override PerkInstance CreateInstance(CharacterStateMachine character)
        {
            GoForTheEyesInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record GoForTheEyesRecord(CleanString Key) : PerkRecord(Key)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (PerkDatabase.GetPerk(Key).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(GoForTheEyesRecord), " data. ", nameof(Key), " with key: ", Key.ToString(), " does not exist in database.");
                return false;
            }
            
            return true;
        }

        public override PerkInstance CreateInstance(CharacterStateMachine owner, CharacterEnumerator allCharacters)
        {
            GoForTheEyesInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class GoForTheEyesInstance : PerkInstance, IStunModifier
    {
        private const float DebuffApplyChanceModifier = 0.4f;
        private const float StunDurationMultiplier = 1.2f;
        private const float DamageModifier = -0.1f;
        public string SharedId => nameof(GoForTheEyesInstance);
        public int Priority => 5;
        
        public GoForTheEyesInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }
        
        public GoForTheEyesInstance(CharacterStateMachine owner, GoForTheEyesRecord record) : base(owner, record)
        {
        }

        protected override void OnSubscribe()
        {
            IStatusApplierModule statusApplierModule = Owner.StatusApplierModule;
            statusApplierModule.StunApplyModifiers.Add(this);
            
            if (CreatedFromLoad)
                return;
            
            Owner.StatsModule.BasePower += DamageModifier;
            statusApplierModule.BaseDebuffApplyChance += DebuffApplyChanceModifier;
        }

        protected override void OnUnsubscribe()
        {
            Owner.StatsModule.BasePower -= DamageModifier;
            IStatusApplierModule statusApplierModule = Owner.StatusApplierModule;
            statusApplierModule.BaseDebuffApplyChance -= DebuffApplyChanceModifier;
            statusApplierModule.StunApplyModifiers.Remove(this);
        }

        public override PerkRecord GetRecord() => new GoForTheEyesRecord(Key);

        public void Modify(ref StunToApply effectStruct)
        {
            effectStruct.Duration *= StunDurationMultiplier;
        }
    }
}