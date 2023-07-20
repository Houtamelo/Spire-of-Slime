using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Behaviour.Modules;
using Core.Combat.Scripts.Cues;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Utils.Math;
using Core.Utils.Patterns;
using JetBrains.Annotations;

namespace Core.Combat.Scripts.Effects.Types.Riposte
{
    public record RiposteScript(bool Permanent, TSpan BaseDuration, int BasePower) : StatusScriptDurationBased(Permanent, BaseDuration)
    {
        public int BasePower { get; protected set; } = BasePower;

        [NotNull]
        public override StatusToApply GetStatusToApply(CharacterStateMachine caster, CharacterStateMachine target, bool crit, [CanBeNull] ISkill skill = null) 
            => new RiposteToApply(caster, target, crit, skill, ScriptOrigin: this, BaseDuration, Permanent, BasePower);

        public override StatusResult ApplyEffect(CharacterStateMachine caster, CharacterStateMachine target, bool crit, [CanBeNull] ISkill skill = null)
        {
            RiposteToApply riposteStruct = new(caster, target, crit, skill, ScriptOrigin: this, BaseDuration, Permanent, BasePower);
            return ProcessModifiersAndTryApply(riposteStruct);
        }

        public static void ProcessModifiers(RiposteToApply effectStruct)
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

        public static StatusResult ProcessModifiersAndTryApply(RiposteToApply effectStruct)
        {
            ProcessModifiers(effectStruct);
            return TryApply(ref effectStruct);
        }

        private static StatusResult TryApply([NotNull] ref RiposteToApply riposteStruct)
        {
            FullCharacterState targetState = riposteStruct.Target.StateEvaluator.FullPureEvaluate();
            if (targetState.Defeated || targetState.Corpse || targetState.Grappled)
                return StatusResult.Failure(riposteStruct.Caster, riposteStruct.Target, generatesInstance: false);
            
            Option<StatusInstance> option = Riposte.CreateInstance(riposteStruct.Duration, riposteStruct.Permanent, riposteStruct.Target, riposteStruct.Caster, riposteStruct.Power);
            
            if (StatusVFXManager.AssertInstance(out StatusVFXManager statusEffectVFXManager))
                statusEffectVFXManager.Enqueue(StatusCueHandler.FromAppliedStatus(riposteStruct.Target, StatusCueHandler.StandardValidator, EffectType.Riposte, success: true));

            return new StatusResult(riposteStruct.Caster, riposteStruct.Target, success: option.IsSome, statusInstance: option.SomeOrDefault(), generatesInstance: option.IsSome, EffectType.Riposte);
        }

        public override float ComputePoints(ref SkillStruct skillStruct, [NotNull] CharacterStateMachine target)
        {
            if (target.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Corpse or CharacterState.Downed or CharacterState.Grappled or CharacterState.Stunned)
                return 0f;
            
            RiposteToApply effectStruct = new(skillStruct.Caster, target, false, skillStruct.Skill, this, BaseDuration, Permanent, BasePower);
            ProcessModifiers(effectStruct);

            float penaltyForAlreadyHavingRiposte = 1f;
            foreach (StatusInstance status in target.StatusReceiverModule.GetAll)
            {
                if (status.EffectType is not EffectType.Riposte || status.IsDeactivated)
                    continue;
                
                penaltyForAlreadyHavingRiposte = HeuristicConstants.AlreadyHasRiposteMultiplier / (1 + status.Duration.FloatSeconds);
                break;
            }
            
            float durationMultiplier = effectStruct.Permanent ? HeuristicConstants.PermanentMultiplier : effectStruct.Duration.FloatSeconds * HeuristicConstants.DurationMultiplier;
            float points = durationMultiplier * (effectStruct.Power / 100f) * HeuristicConstants.DurationMultiplier * HeuristicConstants.RiposteMultiplier * penaltyForAlreadyHavingRiposte;
            return points;
        }
        
        public override EffectType EffectType => EffectType.Riposte;
        [NotNull]
        public override string Description => StatusScriptDescriptions.Get(this);
        
        public override bool IsPositive => true;

        public override bool PlaysBarkAppliedOnCaster => true;
        public override bool PlaysBarkAppliedOnEnemy => false;
        public override bool PlaysBarkAppliedOnAlly => false;
    }
}