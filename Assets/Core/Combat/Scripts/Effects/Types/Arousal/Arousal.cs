using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Utils.Extensions;
using Core.Utils.Patterns;
using UnityEngine;
using Utils.Patterns;

namespace Core.Combat.Scripts.Effects.Types.Arousal
{
    public record ArousalRecord(float Duration, bool IsPermanent, uint LustPerTime, float AccumulatedTime) : StatusRecord(Duration, IsPermanent)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (LustPerTime == 0)
            {
                errors.AppendLine("Invalid ", nameof(ArousalRecord), " data. ", nameof(LustPerTime), " cannot be 0.");
                return false;
            }

            return true;
        }
    }
    
    public class Arousal : StatusInstance
    {
        public override bool IsPositive => false;

        public uint LustPerTime { get; }
        private float _accumulatedTime;

        private Arousal(ArousalToApply s) : base(duration: s.Duration, isPermanent: s.IsPermanent, owner: s.Target)
        {
            LustPerTime = s.LustPerTime;
        }

        public static Option<StatusInstance> CreateInstance(ref ArousalToApply effectStruct)
        {
            if ((effectStruct.Duration <= 0 && !effectStruct.IsPermanent) || effectStruct.LustPerTime <= 0)
            {
                Debug.LogWarning($"Invalid parameters for {nameof(Arousal)} effect. Duration: {effectStruct.Duration.ToString()}, IsPermanent: {effectStruct.IsPermanent.ToString()}, LustPerTime: {effectStruct.LustPerTime.ToString()}");
                return Option<StatusInstance>.None;
            }

            Arousal instance = new(effectStruct);
            effectStruct.Target.StatusModule.AddStatus(instance, effectStruct.Caster);
            return Option<StatusInstance>.Some(instance);
        }

        private Arousal(ArousalRecord record, CharacterStateMachine owner) : base(record, owner)
        {
            LustPerTime = record.LustPerTime;
            _accumulatedTime = record.AccumulatedTime;
        }

        public static Option<StatusInstance> CreateInstance(ArousalRecord record, CharacterStateMachine owner)
        {
            Arousal instance = new(record, owner);
            owner.StatusModule.AddStatus(instance, owner);
            return Option<StatusInstance>.Some(instance);
        }

        public override void Tick(float timeStep)
        {
            if (Duration > timeStep)
            {
                _accumulatedTime += timeStep;
                if (_accumulatedTime >= 1f)
                {
                    int roundTime = Mathf.FloorToInt(_accumulatedTime);
                    _accumulatedTime -= roundTime;
                    if (Owner.LustModule.IsSome)
                        Owner.LustModule.Value.ChangeLust((int)(roundTime * LustPerTime));
                }
            }
            else
            {
                _accumulatedTime += Duration;
                int roundLust = Mathf.CeilToInt(_accumulatedTime * LustPerTime);
                if (Owner.LustModule.IsSome)
                    Owner.LustModule.Value.ChangeLust(roundLust);
                
                _accumulatedTime = 0f;
            }

            base.Tick(timeStep);
        }
        
        public override StatusRecord GetRecord() => new ArousalRecord(Duration, IsPermanent, LustPerTime, _accumulatedTime);

        public override Option<string> GetDescription() => StatusInstanceDescriptions.Get(this);
        public override EffectType EffectType => EffectType.Arousal;
        public const int GlobalId = 1;
    }
}