using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.Interfaces;
using Core.Combat.Scripts.Effects.Types.BuffOrDebuff;
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;

namespace Core.Main_Characters.Ethel.Combat.Perks.Debuffer
{
    public class HardNogging : PerkScriptable
    {
        public override PerkInstance CreateInstance(CharacterStateMachine character)
        {
            HardNoggingInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record HardNoggingRecord(CleanString Key) : PerkRecord(Key)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (PerkDatabase.GetPerk(Key).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(HardNoggingRecord), " data. ", nameof(Key), " with key: ", Key.ToString(), " does not exist in database.");
                return false;
            }
            
            return true;
        }

        public override PerkInstance CreateInstance(CharacterStateMachine owner, CharacterEnumerator allCharacters)
        {
            HardNoggingInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }

    public class HardNoggingInstance : PerkInstance, IBuffOrDebuffModifier, IBaseFloatAttributeModifier
    {
        private const float ExtraDebuffDuration = 1f;
        private const float ResilienceModifierIfStunned = 0.15f;
        public string SharedId => nameof(HardNoggingInstance);
        public int Priority => 0;

        public HardNoggingInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }
        
        public HardNoggingInstance(CharacterStateMachine owner, HardNoggingRecord record) : base(owner, record)
        {
        }

        protected override void OnSubscribe()
        {
            Owner.StatusApplierModule.BuffOrDebuffApplyModifiers.Add(this);
            
            if (Owner.StaminaModule.TrySome(out IStaminaModule staminaModule))
                staminaModule.SubscribeResilience(this, allowDuplicates: false);
        }

        protected override void OnUnsubscribe()
        {
            Owner.StatusApplierModule.BuffOrDebuffApplyModifiers.Remove(this);
            
            if (Owner.StaminaModule.TrySome(out IStaminaModule staminaModule))
                staminaModule.UnsubscribeResilience(this);
        }

        public override PerkRecord GetRecord() => new HardNoggingRecord(Key);

        public void Modify(ref BuffOrDebuffToApply effectStruct)
        {
            if (effectStruct.Delta < 0) 
                effectStruct.Duration += ExtraDebuffDuration;
        }
        
        public void Modify(ref float value, CharacterStateMachine self)
        {
            if (self.StunModule.GetRemaining() > 0)
                value += ResilienceModifierIfStunned;
        }
    }
}