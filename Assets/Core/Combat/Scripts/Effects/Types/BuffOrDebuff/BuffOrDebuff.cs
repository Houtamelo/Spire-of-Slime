using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Effects.Interfaces;
using Core.Combat.Scripts.Enums;
using Core.Utils.Math;
using Core.Utils.Patterns;
using JetBrains.Annotations;
using UnityEngine;

namespace Core.Combat.Scripts.Effects.Types.BuffOrDebuff
{
    public class BuffOrDebuff : StatusInstance, IBaseAttributeModifier
    {
        public override bool IsPositive => EffectType == EffectType.Buff;

        public readonly CombatStat Attribute;

        protected int Delta;
        public virtual int GetDelta => Delta;

        protected BuffOrDebuff(TSpan duration, bool isPermanent, CharacterStateMachine owner, CombatStat attribute, int delta) : base(duration, isPermanent, owner)
        {
            Attribute = attribute;
            Delta = delta;
            EffectType = delta > 0 ? EffectType.Buff : EffectType.Debuff;
        }

        public static Option<StatusInstance> CreateInstance(TSpan duration, bool isPermanent, CharacterStateMachine owner, CharacterStateMachine caster, CombatStat attribute, int delta)
        {
            if ((duration.Ticks <= 0 && !isPermanent) || delta == 0)
            {
                Debug.LogWarning($"Invalid parameters for {nameof(BuffOrDebuff)} effect. Duration: {duration.Seconds.ToString()}, Permanent: {isPermanent.ToString()}, Delta: {delta.ToString()}");
                return Option.None;
            }

            BuffOrDebuff buffOrDebuff = new(duration, isPermanent, owner, attribute, delta);
            buffOrDebuff.Subscribe();
            owner.StatusReceiverModule.AddStatus(buffOrDebuff, caster);
            return buffOrDebuff;
        }

        public BuffOrDebuff([NotNull] BuffOrDebuffRecord record, CharacterStateMachine owner) : base(record, owner)
        {
            EffectType = record.Delta > 0 ? EffectType.Buff : EffectType.Debuff;
            Attribute = record.Attribute;
            Delta = record.Delta;
        }

        public static Option<StatusInstance> CreateInstance([NotNull] BuffOrDebuffRecord record, [NotNull] CharacterStateMachine owner)
        {
            BuffOrDebuff buffOrDebuff = new(record, owner);
            owner.StatusReceiverModule.AddStatus(buffOrDebuff, owner);
            buffOrDebuff.Subscribe();
            return Option<StatusInstance>.Some(buffOrDebuff);
        }

        public override void RequestDeactivation()
        {
            this.Unsubscribe();
            base.RequestDeactivation();
        }
        
        public void Modify(ref int value, CharacterStateMachine self) => value += GetDelta;

        [NotNull]
        public override StatusRecord GetRecord() => new BuffOrDebuffRecord(Duration, Permanent, Attribute, Delta);

        public override Option<string> GetDescription() => StatusInstanceDescriptions.Get(this);
        public override EffectType EffectType { get; }
        
        public const int GlobalId = Arousal.Arousal.GlobalId + 1;
        [NotNull]
        public string SharedId => nameof(BuffOrDebuff);
        public int Priority => -1;
    }
}