using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Behaviour.Modules;
using Core.Combat.Scripts.Effects.Interfaces;
using Core.Combat.Scripts.Effects.Types.BuffOrDebuff;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using Core.Utils.Math;
using JetBrains.Annotations;

namespace Core.Main_Characters.Ethel.Combat.Perks.Debuffer
{
    public class HardNogging : PerkScriptable
    {
        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine character)
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

        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine owner, DirectCharacterEnumerator allCharacters)
        {
            HardNoggingInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }

    public class HardNoggingInstance : PerkInstance, IBuffOrDebuffModifier, IBaseAttributeModifier
    {
        private static readonly TSpan ExtraDebuffDuration = TSpan.FromSeconds(1.0);
        private const int ResilienceModifierIfStunned = 15;

        public HardNoggingInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }

        public HardNoggingInstance(CharacterStateMachine owner, [NotNull] HardNoggingRecord record) : base(owner, record)
        {
        }

        protected override void OnSubscribe()
        {
            Owner.StatusApplierModule.BuffOrDebuffApplyModifiers.Add(this);
            
            if (Owner.StaminaModule.TrySome(out IStaminaModule staminaModule))
                staminaModule.SubscribeResilience(modifier: this, allowDuplicates: false);
        }

        protected override void OnUnsubscribe()
        {
            Owner.StatusApplierModule.BuffOrDebuffApplyModifiers.Remove(this);
            
            if (Owner.StaminaModule.TrySome(out IStaminaModule staminaModule))
                staminaModule.UnsubscribeResilience(modifier: this);
        }

        [NotNull]
        public override PerkRecord GetRecord() => new HardNoggingRecord(Key);

        public void Modify([NotNull] ref BuffOrDebuffToApply effectStruct)
        {
            if (effectStruct.Delta >= 0)
                return;
            
            effectStruct.Duration += ExtraDebuffDuration;
        }

        public void Modify(ref int value, [NotNull] CharacterStateMachine self)
        {
            if (self.StunModule.GetRemaining().Ticks > 0)
                value += ResilienceModifierIfStunned;
        }

        [NotNull]
        public string SharedId => nameof(HardNoggingInstance);
        public int Priority => 0;
    }
}