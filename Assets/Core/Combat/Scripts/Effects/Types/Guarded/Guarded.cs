using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Utils.Math;
using Core.Utils.Patterns;
using JetBrains.Annotations;
using UnityEngine;

namespace Core.Combat.Scripts.Effects.Types.Guarded
{
    public class Guarded : StatusInstance
    {
        public readonly CharacterStateMachine Caster;

        private Guarded(TSpan duration, bool isPermanent, CharacterStateMachine owner, CharacterStateMachine caster) : base(duration, isPermanent, owner) => Caster = caster;

        public static Option<StatusInstance> CreateInstance(TSpan duration, bool isPermanent, CharacterStateMachine owner, CharacterStateMachine caster)
        {
            if (duration.Ticks <= 0 && !isPermanent)
            {
                Debug.LogWarning($"Invalid parameters for {nameof(Guarded)}. Duration:{duration.Seconds.ToString()}, Permanent: false");
                return Option<StatusInstance>.None;
            }

            Guarded instance = new(duration, isPermanent, owner, caster);
            owner.StatusReceiverModule.AddStatus(instance, caster);
            return instance;
        }

        public Guarded([NotNull] GuardedRecord record, CharacterStateMachine owner, CharacterStateMachine caster) : base(record, owner) => Caster = caster;

        public override void CharacterDefeated(CharacterStateMachine character, bool becomesCorpseOnDefeat)
        {
            if (character == Caster || character == Owner)
                RequestDeactivation();
        }

        [NotNull]
        public override StatusRecord GetRecord() => new GuardedRecord(Duration, Permanent, Caster.Guid);

        public override Option<string> GetDescription() => StatusInstanceDescriptions.Get(this);
        public override EffectType EffectType => EffectType.Guarded;
        public override bool IsPositive => true;
        public const int GlobalId = BuffOrDebuff.BuffOrDebuff.GlobalId + 1;
    }
}