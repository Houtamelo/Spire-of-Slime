using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Utils.Math;
using Core.Utils.Patterns;
using JetBrains.Annotations;
using UnityEngine;

namespace Core.Combat.Scripts.Effects.Types.Marked
{
    public class Marked : StatusInstance
    {
        private Marked(TSpan duration, bool isPermanent, CharacterStateMachine owner) : base(duration, isPermanent, owner)
        {
        }

        public static Option<StatusInstance> CreateInstance(TSpan duration, bool isPermanent, CharacterStateMachine owner, CharacterStateMachine caster)
        {
            if (duration.Ticks <= 0 && isPermanent == false)
            {
                Debug.LogWarning($"Invalid parameters for {nameof(Marked)}. Duration:{duration.Seconds.ToString()}, Permanent: false");
                return Option<StatusInstance>.None;
            }
            
            Marked instance = new(duration, isPermanent, owner);
            owner.StatusReceiverModule.AddStatus(instance, caster);
            return Option<StatusInstance>.Some(instance);
        }

        public Marked([NotNull] MarkedRecord record, CharacterStateMachine owner) : base(record, owner)
        {
        }

        [NotNull]
        public override StatusRecord GetRecord() => new MarkedRecord(Duration, Permanent);

        public override Option<string> GetDescription() => StatusInstanceDescriptions.Get(this);
        public override EffectType EffectType => EffectType.Marked;
        public override bool IsPositive => false;
        public const int GlobalId = Guarded.Guarded.GlobalId + 1;
    }
}