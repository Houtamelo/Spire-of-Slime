using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects;
using Core.Combat.Scripts.Effects.Types.Heal;
using Core.Combat.Scripts.Effects.Types.OvertimeHeal;
using Core.Combat.Scripts.Interfaces;
using Core.Combat.Scripts.Interfaces.Events;
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Combat.Scripts.Skills.Action;
using Main_Database.Combat;
using Save_Management;
using Utils.Extensions;
using Utils.Math;

namespace Core.Main_Characters.Nema.Combat.Perks.Healer
{
    public class Awe : PerkScriptable
    {
        public override PerkInstance CreateInstance(CharacterStateMachine character)
        {
            AweInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record AweRecord(CleanString Key, uint StackCount, float AccumulatedDelay) : PerkRecord(Key)
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

        public override PerkInstance CreateInstance(CharacterStateMachine owner, CharacterEnumerator allCharacters)
        {
            AweInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class AweInstance : PerkInstance, ITick, IHealModifier, IOvertimeHealModifier, IStatusAppliedListener
    {
        public string SharedId => nameof(AweInstance);
        public int Priority => 6;
        
        private const float DelayPerStackGain = 1.5f;
        private const float HealPowerPerStack = 0.1f;
        private const float OvertimeHealPowerPerStack = 0.7f;
        private const uint MaxStackCount = 5;

        private uint _stackCount;
        private float _accumulatedDelay;

        public AweInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }
        
        public AweInstance(CharacterStateMachine owner, AweRecord record) : base(owner, record)
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

        public override PerkRecord GetRecord() => new AweRecord(Key, _stackCount, _accumulatedDelay);

        public void OnStatusApplied(ref StatusResult statusResult)
        {
            if (statusResult is { IsSuccess: true, EffectType: EffectType.Heal or EffectType.OvertimeHeal })
                _stackCount = 0;
        }

        public void Tick(float timeStep)
        {
            _accumulatedDelay += timeStep;
            if (_accumulatedDelay >= DelayPerStackGain)
            {
                _accumulatedDelay -= DelayPerStackGain;
                _stackCount = (_stackCount + 1).Clamp(0, MaxStackCount);
            }
        }

        public void Modify(ref OvertimeHealToApply effectStruct)
        {
            effectStruct.HealPerTime = ((1 + OvertimeHealPowerPerStack * _stackCount) * effectStruct.HealPerTime).CeilToUInt();
        }

        public void Modify(ref HealToApply effectStruct)
        {
            effectStruct.Power *= 1 + HealPowerPerStack * _stackCount;
        }
    }
}