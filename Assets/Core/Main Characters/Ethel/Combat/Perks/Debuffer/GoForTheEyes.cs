using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Behaviour.Modules;
using Core.Combat.Scripts.Effects.Types.Stun;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using Core.Utils.Math;
using JetBrains.Annotations;

namespace Core.Main_Characters.Ethel.Combat.Perks.Debuffer
{
    public class GoForTheEyes : PerkScriptable
    {
        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine character)
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

        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine owner, DirectCharacterEnumerator allCharacters)
        {
            GoForTheEyesInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class GoForTheEyesInstance : PerkInstance, IStunModifier
    {
        private const int DebuffApplyChanceModifier = 40;
        private const int DamageModifier = -10;
        private const int StunPowerMultiplier = 120;

        public GoForTheEyesInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }

        public GoForTheEyesInstance(CharacterStateMachine owner, [NotNull] GoForTheEyesRecord record) : base(owner, record)
        {
        }

        protected override void OnSubscribe()
        {
            IStatusApplierModule statusApplierModule = Owner.StatusApplierModule;
            statusApplierModule.StunApplyModifiers.Add(this);
            
            if (CreatedFromLoad)
                return;
            
            Owner.StatsModule.BasePower = Owner.StatsModule.BasePower + DamageModifier;
            statusApplierModule.BaseDebuffApplyChance += DebuffApplyChanceModifier;
        }

        protected override void OnUnsubscribe()
        {
            Owner.StatsModule.BasePower = Owner.StatsModule.BasePower - DamageModifier;
            IStatusApplierModule statusApplierModule = Owner.StatusApplierModule;
            statusApplierModule.BaseDebuffApplyChance -= DebuffApplyChanceModifier;
            statusApplierModule.StunApplyModifiers.Remove(this);
        }

        [NotNull]
        public override PerkRecord GetRecord() => new GoForTheEyesRecord(Key);

        public void Modify([NotNull] ref StunToApply effectStruct)
        {
            effectStruct.StunPower = (effectStruct.StunPower * StunPowerMultiplier) / 100;
        }

        [NotNull]
        public string SharedId => nameof(GoForTheEyesInstance);
        public int Priority => 5;
    }
}