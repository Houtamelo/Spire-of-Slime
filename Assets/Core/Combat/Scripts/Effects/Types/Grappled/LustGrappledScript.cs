using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Behaviour.Modules;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Utils.Math;
using Core.Utils.Patterns;
using JetBrains.Annotations;

namespace Core.Combat.Scripts.Effects.Types.Grappled
{
    public record LustGrappledScript(bool Permanent, TSpan BaseDuration, string TriggerName, float GraphicalX, int BaseLustPerTime, int TemptationDeltaPerTime)
        : StatusScriptDurationBased(Permanent, BaseDuration)
    {
        public string TriggerName { get; protected set; } = TriggerName;
        public float GraphicalX { get; protected set; } = GraphicalX;
        public int BaseLustPerTime { get; protected set; } = BaseLustPerTime;
        public int TemptationDeltaPerTime { get; protected set; } = TemptationDeltaPerTime;

        [NotNull]
        public override StatusToApply GetStatusToApply(CharacterStateMachine caster, CharacterStateMachine target, bool crit, [CanBeNull] ISkill skill = null)
        {
            LustGrappledToApply effectStruct = new(caster, target, crit, skill, this, BaseDuration, Permanent, TriggerName, GraphicalX, BaseLustPerTime, TemptationDeltaPerTime);
            return effectStruct;
        }

        public override StatusResult ApplyEffect(CharacterStateMachine caster, CharacterStateMachine target, bool crit, [CanBeNull] ISkill skill = null)
        {
            LustGrappledToApply effectStruct = new(caster, target, crit, skill, this, BaseDuration, Permanent, TriggerName, GraphicalX, BaseLustPerTime, TemptationDeltaPerTime);
            return ProcessModifiersAndTryApply(effectStruct);
        }

        public static void ProcessModifiers(LustGrappledToApply effectStruct)
        {
            IStatusApplierModule applierModule = effectStruct.Caster.StatusApplierModule;
            applierModule.ModifyEffectApplying(ref effectStruct);
            
            IStatusReceiverModule receiverModule = effectStruct.Target.StatusReceiverModule;
            receiverModule.ModifyEffectReceiving(ref effectStruct);
            
            if (effectStruct.FromCrit)
            {
                TSpan duration = effectStruct.Duration;
                duration.Multiply(DurationMultiplierOnCrit);
                effectStruct.Duration = duration;
            }
        }

        public static StatusResult ProcessModifiersAndTryApply([NotNull] LustGrappledToApply effectStruct)
        {
            ProcessModifiers(effectStruct);
            return TryApply(effectStruct);
        }

        public static StatusResult TryApply([NotNull] LustGrappledToApply record)
        {
            if (record.Target.LustModule.IsNone || record.Caster.PositionHandler.IsLeftSide == record.Target.PositionHandler.IsLeftSide)
                return StatusResult.Failure(record.Caster, record.Target, generatesInstance: true);
            
            Option<StatusInstance> lustGrappled = LustGrappled.CreateInstance(record.Duration, record.Permanent, owner: record.Target, restrainer: record.Caster,
                                                                              record.LustPerSecond, record.TemptationDeltaPerTime, record.TriggerName, record.GraphicalX);
            return new StatusResult(record.Caster, record.Target, success: lustGrappled.IsSome, statusInstance: lustGrappled.SomeOrDefault(), generatesInstance: true, EffectType.LustGrappled);
        }

        public override float ComputePoints(ref SkillStruct skillStruct, CharacterStateMachine target) => 0.1f;

        [NotNull]
        public override string Description => StatusScriptDescriptions.Get(this);
        public override EffectType EffectType => EffectType.LustGrappled;
        
        public override bool IsPositive => false;
        
        public override bool PlaysBarkAppliedOnCaster => false;
        public override bool PlaysBarkAppliedOnEnemy => true;
        public override bool PlaysBarkAppliedOnAlly => false;
    }
}