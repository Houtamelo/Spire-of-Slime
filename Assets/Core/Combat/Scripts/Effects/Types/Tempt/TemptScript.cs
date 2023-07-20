using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Behaviour.Modules;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Effects.Types.Grappled;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Utils.Math;
using Core.Utils.Patterns;
using JetBrains.Annotations;

namespace Core.Combat.Scripts.Effects.Types.Tempt
{
    public record TemptScript(int Power) : StatusScript
    {
        public int Power { get; set; } = Power;

        [NotNull]
        public override StatusToApply GetStatusToApply(CharacterStateMachine caster, CharacterStateMachine target, bool crit, [CanBeNull] ISkill skill = null)
            => new TemptToApply(caster, target, crit, skill, ScriptOrigin: this, Power);

        public static void ProcessModifiers(TemptToApply toApply)
        {
            IStatusApplierModule applierModule = toApply.Caster.StatusApplierModule;
            applierModule.ModifyEffectApplying(ref toApply);
            
            IStatusReceiverModule receiverModule = toApply.Target.StatusReceiverModule;
            receiverModule.ModifyEffectReceiving(ref toApply);
        }

        public static StatusResult ProcessModifiersAndTryApply(TemptToApply effectStruct)
        {
            ProcessModifiers(effectStruct);
            return TryApply(ref effectStruct);
        }

        public override StatusResult ApplyEffect(CharacterStateMachine caster, CharacterStateMachine target, bool crit, [CanBeNull] ISkill skill = null)
        {
            TemptToApply effectStruct = new(caster, target, crit, skill, ScriptOrigin: this, Power);
            return ProcessModifiersAndTryApply(effectStruct);
        }

        private static StatusResult TryApply([NotNull] ref TemptToApply effectStruct)
        {
            FullCharacterState targetState = effectStruct.Target.StateEvaluator.FullPureEvaluate();
            if (targetState.Defeated || targetState.Corpse || targetState.Grappled || effectStruct.Target.LustModule.TrySome(out ILustModule lustModule) == false)
                return StatusResult.Failure(effectStruct.Caster, effectStruct.Target, generatesInstance: false);

            lustModule.ApplyTemptationPower(effectStruct.Power);
            lustModule.IncrementSexualExp(effectStruct.Caster.Script.Race, amount: 1);

            int updatedTemptation = lustModule.GetTemptation();
            if (updatedTemptation >= 100 && effectStruct.Caster != effectStruct.Target &&
                effectStruct.Caster.Script.DoesActiveSex(effectStruct.Target).TrySome(out (string parameter, float graphicalX) tuple))
            {
                LustGrappledScript grappledScript = new(Permanent: false, BaseDuration: TSpan.FromSeconds(5), tuple.parameter, tuple.graphicalX, BaseLustPerTime: 45, TemptationDeltaPerTime: -5);
                grappledScript.ApplyEffect(effectStruct.Caster, effectStruct.Target, crit: effectStruct.FromCrit, effectStruct.FromSkill ? effectStruct.GetSkill() : null);
                lustModule.ChangeTemptation(-15);
            }
            
            return StatusResult.Success(effectStruct.Caster, effectStruct.Target, statusInstance: null, generatesInstance: false, EffectType.Temptation);
        }

        public override float ComputePoints(ref SkillStruct skillStruct, [NotNull] CharacterStateMachine target)
        {
            if (target.LustModule.TrySome(out ILustModule targetLust) == false || target.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Corpse)
                return 0f;
            
            TemptToApply effectStruct = new(skillStruct.Caster, target, FromCrit: false, skillStruct.Skill, ScriptOrigin: this, Power);
            ProcessModifiers(effectStruct);

            int temptationDelta = ILustModule.CalculateAppliedTemptation(effectStruct.Power, targetLust.GetComposure(), targetLust.GetLust());
            float points = (temptationDelta / 100f) * HeuristicConstants.TemptationMultiplier;
            return points * -1f;
        }

        public override EffectType EffectType => EffectType.Temptation;
        [NotNull]
        public override string Description => StatusScriptDescriptions.Get(this);
        
        public override bool IsPositive => false;

        public override bool PlaysBarkAppliedOnCaster => false;
        public override bool PlaysBarkAppliedOnEnemy => true;
        public override bool PlaysBarkAppliedOnAlly => false;
    }
}