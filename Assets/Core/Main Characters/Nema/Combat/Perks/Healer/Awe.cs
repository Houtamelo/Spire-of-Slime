using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Behaviour.Modules;
using Core.Combat.Scripts.Effects;
using Core.Combat.Scripts.Effects.Types.Heal;
using Core.Combat.Scripts.Effects.Types.OvertimeHeal;
using Core.Combat.Scripts.Interfaces;
using Core.Combat.Scripts.Interfaces.Events;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Combat.Scripts.Skills.Action;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using Core.Utils.Math;
using JetBrains.Annotations;

namespace Core.Main_Characters.Nema.Combat.Perks.Healer
{
    public class Awe : PerkScriptable
    {
        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine character)
        {
            AweInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record AweRecord(CleanString Key, int StackCount, TSpan AccumulatedDelay) : PerkRecord(Key)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (PerkDatabase.GetPerk(Key).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(AweRecord), " data. ", nameof(Key), " with key: ", Key.ToString(), " does not exist in database.");
                return false;
            }
            
            return true;
        }

        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine owner, DirectCharacterEnumerator allCharacters)
        {
            AweInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class AweInstance : PerkInstance, ITick, IHealModifier, IOvertimeHealModifier, IStatusAppliedListener
    {
        private static readonly TSpan DelayPerStackGain = TSpan.FromSeconds(1.5);
        
        private const int HealPowerPerStack = 10;
        private const int OvertimeHealPowerPerStack = 7;
        private const int MaxStackCount = 5;

        private int _stackCount;
        private TSpan _accumulatedDelay;

        public AweInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }

        public AweInstance(CharacterStateMachine owner, [NotNull] AweRecord record) : base(owner, record)
        {
            _stackCount = record.StackCount;
            _accumulatedDelay = record.AccumulatedDelay;
        }

        protected override void OnSubscribe()
        {
            Owner.SubscribedTickers.Add(this);
            Owner.Events.StatusAppliedListeners.Add(this);
            
            IStatusApplierModule applierModule = Owner.StatusApplierModule;
            applierModule.HealApplyModifiers.Add(this);
            applierModule.OvertimeHealApplyModifiers.Add(this);
        }

        protected override void OnUnsubscribe()
        {
            Owner.SubscribedTickers.Remove(this);
            Owner.Events.StatusAppliedListeners.Remove(this);
            
            IStatusApplierModule applierModule = Owner.StatusApplierModule;
            applierModule.HealApplyModifiers.Remove(this);
            applierModule.OvertimeHealApplyModifiers.Remove(this);
        }

        [NotNull]
        public override PerkRecord GetRecord() => new AweRecord(Key, _stackCount, _accumulatedDelay);

        public void OnStatusApplied(ref StatusResult statusResult)
        {
            if (statusResult is { IsSuccess: true, EffectType: EffectType.Heal or EffectType.OvertimeHeal })
                _stackCount = 0;
        }

        public void Tick(TSpan timeStep)
        {
            _accumulatedDelay += timeStep;
            if (_accumulatedDelay >= DelayPerStackGain)
            {
                _accumulatedDelay -= DelayPerStackGain;
                _stackCount = (_stackCount + 1).Clamp(0, MaxStackCount);
            }
        }

        public void Modify([NotNull] ref OvertimeHealToApply effectStruct)
        {
            effectStruct.HealPerSecond = ((100 + (OvertimeHealPowerPerStack * _stackCount)) * effectStruct.HealPerSecond) / 100;
        }

        public void Modify([NotNull] ref HealToApply effectStruct)
        {
            effectStruct.Power = ((100 + (HealPowerPerStack * _stackCount)) * effectStruct.Power) / 100;
        }

        [NotNull]
        public string SharedId => nameof(AweInstance);
        public int Priority => 6;
    }
}