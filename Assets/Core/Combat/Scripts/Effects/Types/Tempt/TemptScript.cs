using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Effects.Types.Grappled;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Utils.Patterns;
using Utils.Patterns;

namespace Core.Combat.Scripts.Effects.Types.Tempt
{
    public record TemptScript(float Power) : StatusScript
    {
        public const int MinLustForIncrease = 25;
        
        public float Power { get; set; } = Power;
        
        public override bool IsPositive => false;

        public override bool PlaysBarkAppliedOnCaster => false;
        public override bool PlaysBarkAppliedOnEnemy => true;
        public override bool PlaysBarkAppliedOnAlly => false;

        public override StatusToApply GetStatusToApply(CharacterStateMachine caster, CharacterStateMachine target, bool crit, ISkill skill = null)
            => new TemptToApply(caster, target, crit, skill, this, Power);

        public static void ProcessModifiers(TemptToApply toApply)
        {
            IStatusApplierModule applierModule = toApply.Caster.StatusApplierModule;
            applierModule.ModifyEffectApplying(ref toApply);
            
            IStatusReceiverModule receiverModule = toApply.Target.StatusModule;
            receiverModule.ModifyEffectReceiving(ref toApply);
        }

        public static StatusResult ProcessModifiersAndTryApply(TemptToApply effectStruct)
        {
            ProcessModifiers(effectStruct);
            return TryApply(ref effectStruct);
        }

        public override StatusResult ApplyEffect(CharacterStateMachine caster, CharacterStateMachine target, bool crit, ISkill skill = null)
        {
            TemptToApply effectStruct = new(caster, target, crit, skill, ScriptOrigin: this, Power);
            return ProcessModifiersAndTryApply(effectStruct);
        }

        private static StatusResult TryApply(ref TemptToApply effectStruct)
        {
            FullCharacterState targetState = effectStruct.Target.StateEvaluator.FullPureEvaluate();
            if (targetState.Defeated || targetState.Corpse || targetState.Grappled || effectStruct.Target.LustModule.TrySome(out ILustModule lustModule) == false)
                return StatusResult.Failure(effectStruct.Caster, effectStruct.Target, generatesInstance: false);

            float temptationDelta = CalculateTemptationDelta(effectStruct.Power, lustModule);
            lustModule.ChangeTemptation(temptationDelta);
            lustModule.IncrementSexualExp(effectStruct.Caster.Script.Race, amount: 1);

            ClampedPercentage updatedTemptation = lustModule.GetTemptation();
            if (updatedTemptation >= 1f && effectStruct.Caster != effectStruct.Target &&
                effectStruct.Caster.Script.DoesActiveSex(effectStruct.Target).TrySome(out (string parameter, float graphicalX) tuple))
            {
                LustGrappledScript grappledScript = new(Permanent: false, BaseDuration: 5, tuple.parameter, tuple.graphicalX, BaseLustPerTime: 45, BaseTemptationDeltaPerTime: -0.05f);
                grappledScript.ApplyEffect(effectStruct.Caster, effectStruct.Target, crit: effectStruct.FromCrit, effectStruct.FromSkill ? effectStruct.GetSkill() : null);
                lustModule.ChangeTemptation(-0.15f);
            }
            
            return StatusResult.Success(effectStruct.Caster, effectStruct.Target, statusInstance: null, generatesInstance: false, EffectType.Temptation);
        }

        public override float ComputePoints(ref SkillStruct skillStruct, CharacterStateMachine target)
        {
            if (target.LustModule.IsNone || target.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Corpse)
                return 0f;
            
            TemptToApply effectStruct = new(skillStruct.Caster, target, FromCrit: false, skillStruct.Skill, ScriptOrigin: this, Power);
            ProcessModifiers(effectStruct);
            
            float temptationDelta = CalculateTemptationDelta(effectStruct.Power, target.LustModule.Value);
            float points = temptationDelta * HeuristicConstants.TemptationMultiplier;
            return points * -1f;
        }

        public static float CalculateTemptationDelta(float power, ILustModule target)
        {
            uint lust = target.GetLust();

            return (power > 0) switch
            {
                true when lust < MinLustForIncrease => 0,
                true                                => (float)(0.2 * ((lust - 25.0) / 175.0) * ((lust - 25.0) / 175.0)),
                false                               => power * 0.2f
            };
        }
        
        public override EffectType EffectType => EffectType.Temptation;
        public override string Description => StatusScriptDescriptions.Get(this);
    }
}