using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Utils.Patterns;
using Utils.Patterns;

namespace Core.Combat.Scripts.Effects.Types.Grappled
{
    public record LustGrappledScript(bool Permanent, float BaseDuration, string TriggerName, float GraphicalX, uint BaseLustPerTime, float BaseTemptationDeltaPerTime)
        : StatusScriptDurationBased(Permanent, BaseDuration)
    {
        public string TriggerName { get; protected set; } = TriggerName;
        public float GraphicalX { get; protected set; } = GraphicalX;
        public uint BaseLustPerTime { get; protected set; } = BaseLustPerTime;
        public float TemptationDeltaPerTime { get; protected set; } = BaseTemptationDeltaPerTime;
        
        public override bool IsPositive => false;
        
        public override bool PlaysBarkAppliedOnCaster => false;
        public override bool PlaysBarkAppliedOnEnemy => true;
        public override bool PlaysBarkAppliedOnAlly => false;

        public override StatusToApply GetStatusToApply(CharacterStateMachine caster, CharacterStateMachine target, bool crit, ISkill skill = null)
        {
            LustGrappledToApply effectStruct = new(caster, target, crit, skill, this, BaseDuration, Permanent, TriggerName, GraphicalX, BaseLustPerTime, TemptationDeltaPerTime);
            return effectStruct;
        }

        public override StatusResult ApplyEffect(CharacterStateMachine caster, CharacterStateMachine target, bool crit, ISkill skill = null)
        {
            LustGrappledToApply effectStruct = new(caster, target, crit, skill, this, BaseDuration, Permanent, TriggerName, GraphicalX, BaseLustPerTime, TemptationDeltaPerTime);
            return ProcessModifiersAndTryApply(effectStruct);
        }

        public static void ProcessModifiers(LustGrappledToApply effectStruct)
        {
            IStatusApplierModule applierModule = effectStruct.Caster.StatusApplierModule;
            applierModule.ModifyEffectApplying(ref effectStruct);
            
            IStatusReceiverModule receiverModule = effectStruct.Target.StatusModule;
            receiverModule.ModifyEffectReceiving(ref effectStruct);
            
            if (effectStruct.FromCrit)
                effectStruct.Duration *= DurationMultiplierOnCrit;
        }

        public static StatusResult ProcessModifiersAndTryApply(LustGrappledToApply effectStruct)
        {
            ProcessModifiers(effectStruct);
            return TryApply(effectStruct);
        }

        public static StatusResult TryApply(LustGrappledToApply record)
        {
            if (record.Target.LustModule.IsNone || record.Caster.PositionHandler.IsLeftSide == record.Target.PositionHandler.IsLeftSide)
                return StatusResult.Failure(record.Caster, record.Target, generatesInstance: true);
            
            Option<StatusInstance> lustGrappled = LustGrappled.CreateInstance(record.Duration, record.IsPermanent, record.Target, record.Caster,
                                                                              record.LustPerTime, record.TemptationDeltaPerTime, record.TriggerName, record.GraphicalX);
            return new StatusResult(record.Caster, record.Target, success: lustGrappled.IsSome, statusInstance: lustGrappled.SomeOrDefault(), generatesInstance: true, EffectType.LustGrappled);
        }
        
        public override float ComputePoints(ref SkillStruct skillStruct, CharacterStateMachine target) => 0.1f;

        public override string Description => StatusScriptDescriptions.Get(this);
        public override EffectType EffectType => EffectType.LustGrappled;
    }
}