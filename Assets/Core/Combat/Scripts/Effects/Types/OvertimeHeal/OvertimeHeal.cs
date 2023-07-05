using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Utils.Extensions;
using Core.Utils.Math;
using Core.Utils.Patterns;
using UnityEngine;
using Utils.Patterns;

namespace Core.Combat.Scripts.Effects.Types.OvertimeHeal
{
    public record OvertimeHealRecord(float Duration, bool IsPermanent, uint HealPerTime, float AccumulatedTime) : StatusRecord(Duration, IsPermanent)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (HealPerTime == 0)
            {
                errors.AppendLine("Invalid ", nameof(OvertimeHealRecord), " data. ", nameof(HealPerTime), " cannot be 0.");
                return false;
            }
            
            return true;
        }
    }

    public class OvertimeHeal : StatusInstance
    {
        public override bool IsPositive => true;

        public uint HealPerTime { get; }

        private float _accumulatedTime;

        private OvertimeHeal(float duration, bool isPermanent, CharacterStateMachine owner, uint healPerTime) : base(duration: duration, isPermanent: isPermanent, owner: owner)
        {
            HealPerTime = healPerTime;
        }

        public static Option<StatusInstance> CreateInstance(float duration, bool isPermanent, CharacterStateMachine owner, CharacterStateMachine caster, uint healPerTime)
        {
            if ((duration <= 0 && !isPermanent) || healPerTime == 0)
            {
                Debug.LogWarning($"Invalid parameters for {nameof(OvertimeHeal)}. Duration: {duration}, IsPermanent: {isPermanent}, HealPerTime: {healPerTime}");
                return Option<StatusInstance>.None;
            }
            
            OvertimeHeal instance = new(duration, isPermanent, owner, healPerTime);
            owner.StatusModule.AddStatus(instance, caster);
            return Option<StatusInstance>.Some(instance);
        }

        private OvertimeHeal(OvertimeHealRecord record, CharacterStateMachine owner) : base(record, owner)
        {
            HealPerTime = record.HealPerTime;
            _accumulatedTime = record.AccumulatedTime;
        }

        public static Option<StatusInstance> CreateInstance(OvertimeHealRecord record, CharacterStateMachine owner)
        {
            OvertimeHeal instance = new(record, owner);
            owner.StatusModule.AddStatus(instance, owner);
            return Option<StatusInstance>.Some(instance);
        }

        public override void Tick(float timeStep)
        {
            if (Duration > timeStep)
            {
                _accumulatedTime += timeStep;
                if (_accumulatedTime >= 1)
                {
                    uint roundTime = _accumulatedTime.FloorToUInt();
                    _accumulatedTime -= roundTime;
                    if (Owner.StaminaModule.IsSome)
                        Owner.StaminaModule.Value.DoHeal(roundTime * HealPerTime, isOvertime: true);
                }
            }
            else
            {
                _accumulatedTime += Duration;
                uint roundHeal = (_accumulatedTime * HealPerTime).CeilToUInt();
                if (Owner.StaminaModule.IsSome && roundHeal != 0)
                    Owner.StaminaModule.Value.DoHeal(roundHeal, isOvertime: true);
            }

            base.Tick(timeStep);
        }
        
        public override StatusRecord GetRecord() => new OvertimeHealRecord(Duration, IsPermanent, HealPerTime, _accumulatedTime);

        public override Option<string> GetDescription() => StatusInstanceDescriptions.Get(this);
        public override EffectType EffectType => EffectType.OvertimeHeal;
        public const int GlobalId = Marked.Marked.GlobalId + 1;
    }
}