using System;
using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Utils.Extensions;
using Core.Utils.Math;
using Core.Utils.Patterns;
using UnityEngine;
using Utils.Patterns;

namespace Core.Combat.Scripts.Effects.Types.Poison
{
    public record PoisonRecord(float Duration, bool IsPermanent, uint DamagePerTime, float AccumulatedTime, Guid Caster) : StatusRecord(Duration, IsPermanent)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (DamagePerTime == 0)
            {
                errors.AppendLine("Invalid ", nameof(PoisonRecord), " data. ", nameof(DamagePerTime), " is 0.");
                return false;
            }
            
            foreach (CharacterRecord character in allCharacters)
            {
                if (character.Guid == Caster)
                    return true;
            }
            
            errors.AppendLine("Invalid ", nameof(PoisonRecord), " data. ", nameof(Caster), "'s Guid: ", Caster.ToString(), " could not be mapped to a character.");
            return false;
        }
    }

    public class Poison : StatusInstance
    {
        public override bool IsPositive => false;

        public readonly CharacterStateMachine Caster;
        public readonly uint DamagePerTime;
        private float _accumulatedTime;

        private Poison(float duration, bool isPermanent, CharacterStateMachine owner, CharacterStateMachine caster, uint damagePerSecond) : base(duration: duration, isPermanent: isPermanent, owner: owner)
        {
            DamagePerTime = damagePerSecond;
            Caster = caster;
        }

        public static Option<StatusInstance> CreateInstance(float duration, bool isPermanent, CharacterStateMachine owner, CharacterStateMachine caster, uint damagePerSecond)
        {
            if ((duration <= 0 && !isPermanent) || damagePerSecond <= 0)
            {
                Debug.LogWarning($"Invalid poison parameters: duration: {duration}, isPermanent: {isPermanent}, damagePerSecond: {damagePerSecond}");
                return Option<StatusInstance>.None;
            }
            
            Poison instance = new(duration, isPermanent, owner, caster, damagePerSecond);
            owner.StatusModule.AddStatus(instance, caster);
            return Option<StatusInstance>.Some(instance);
        }

        private Poison(PoisonRecord record, CharacterStateMachine owner, CharacterStateMachine caster) : base(record, owner)
        {
            DamagePerTime = record.DamagePerTime;
            _accumulatedTime = record.AccumulatedTime;
            Caster = caster;
        }

        public static Option<StatusInstance> CreateInstance(PoisonRecord record, CharacterStateMachine owner, ref CharacterEnumerator allCharacters)
        {
            CharacterStateMachine caster = null;
            foreach (CharacterStateMachine character in allCharacters)
            {
                if (character.Guid == record.Caster)
                {
                    caster = character;
                    break;
                }
            }
            
            if (caster == null)
                return Option<StatusInstance>.None;
            
            Poison instance = new(record, owner, caster);
            owner.StatusModule.AddStatus(instance, owner);
            return Option<StatusInstance>.Some(instance);
        }

        public override void Tick(float timeStep)
        {
            if (Duration <= timeStep)
            {
                _accumulatedTime += Duration;
                uint roundDamage = (_accumulatedTime * DamagePerTime).CeilToUInt();
                if (Owner.StaminaModule.IsSome && roundDamage != 0)
                    Owner.StaminaModule.Value.ReceiveDamage(roundDamage, DamageType.Poison, Caster);
                
                return;
            }

            _accumulatedTime += timeStep;
            if (_accumulatedTime >= 1)
            {
                uint roundTime = _accumulatedTime.FloorToUInt();
                _accumulatedTime -= roundTime;
                uint damage = roundTime * DamagePerTime;
                if (Owner.StaminaModule.IsSome && damage != 0)
                    Owner.StaminaModule.Value.ReceiveDamage(damage, DamageType.Poison, Caster);
            }

            base.Tick(timeStep);
        }


        public override StatusRecord GetRecord() => new PoisonRecord(Duration, IsPermanent, DamagePerTime, _accumulatedTime, Caster.Guid);

        public override Option<string> GetDescription() => StatusInstanceDescriptions.Get(this);
        public override EffectType EffectType => EffectType.Poison;
        public const int GlobalId = OvertimeHeal.OvertimeHeal.GlobalId + 1;
    }
}