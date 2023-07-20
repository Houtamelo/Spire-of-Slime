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

namespace Core.Combat.Scripts.Effects.Types.Marked
{
    public record MarkedScript(bool Permanent, TSpan BaseDuration) : StatusScriptDurationBased(Permanent, BaseDuration)
    {
        [NotNull]
        public override StatusToApply GetStatusToApply(CharacterStateMachine caster, CharacterStateMachine target, bool crit, [CanBeNull] ISkill skill = null) 
            => new MarkedToApply(caster, target, crit, skill, ScriptOrigin: this, BaseDuration, Permanent);

        public override StatusResult ApplyEffect(CharacterStateMachine caster, CharacterStateMachine target, bool crit, [CanBeNull] ISkill skill = null)
        {
            MarkedToApply effectStruct = new(caster, target, crit, skill, ScriptOrigin: this, BaseDuration, Permanent);
            return ProcessModifiersAndTryApply(effectStruct);
        }

        public static void ProcessModifiers(MarkedToApply effectStruct)
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

        public static StatusResult ProcessModifiersAndTryApply(MarkedToApply effectStruct)
        {
            ProcessModifiers(effectStruct);
            return TryApply(ref effectStruct);
        }

        private static StatusResult TryApply([NotNull] ref MarkedToApply markedStruct)
        {
            FullCharacterState targetState = markedStruct.Target.StateEvaluator.FullPureEvaluate();
            if (targetState.Defeated || targetState.Corpse || targetState.Grappled)
                return StatusResult.Failure(markedStruct.Caster, markedStruct.Target, generatesInstance: false);
            
            Option<StatusInstance> option = Marked.CreateInstance(markedStruct.Duration, markedStruct.Permanent, markedStruct.Target, markedStruct.Caster);
            
            if (StatusVFXManager.AssertInstance(out StatusVFXManager statusEffectVFXManager))
                statusEffectVFXManager.Enqueue(StatusCueHandler.FromAppliedStatus(markedStruct.Target, StatusCueHandler.StandardValidator, EffectType.Marked, success: option.IsSome));

            return new StatusResult(markedStruct.Caster, markedStruct.Target, success: option.IsSome, statusInstance: option.SomeOrDefault(), generatesInstance: true, EffectType.Marked);
        }

        public override float ComputePoints(ref SkillStruct skillStruct, [NotNull] CharacterStateMachine target)
        {
            if (target.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Corpse or CharacterState.Grappled or CharacterState.Downed)
                return 0f;
            
            MarkedToApply effectStruct = new(skillStruct.Caster, target, FromCrit: false, skillStruct.Skill, ScriptOrigin: this, BaseDuration, Permanent);
            ProcessModifiers(effectStruct);

            float penaltyForTargetAllyAlreadyMarked = 1f;
            if (target.Display.IsSome)
                foreach (CharacterStateMachine ally in target.Display.Value.CombatManager.Characters.GetOnSide(target))
                {
                    if (ally == target || ally.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Corpse)
                        continue;
                    
                    foreach (StatusInstance status in ally.StatusReceiverModule.GetAll)
                    {
                        if (status.EffectType is not EffectType.Marked || status.IsDeactivated)
                            continue;
                        
                        penaltyForTargetAllyAlreadyMarked = HeuristicConstants.AllyAlreadyHasMarkedMultiplier / (1 + status.Duration.FloatSeconds);
                        break;
                    }

                    if (penaltyForTargetAllyAlreadyMarked < 1f)
                        break;
                }

            float durationMultiplier = effectStruct.Permanent ? HeuristicConstants.PermanentMultiplier : effectStruct.Duration.FloatSeconds * HeuristicConstants.DurationMultiplier;
            float points = durationMultiplier * HeuristicConstants.MarkedMultiplier * penaltyForTargetAllyAlreadyMarked;
            return points * -1f;
        }

        [NotNull]
        public override string Description => StatusScriptDescriptions.Get(this);
        public override EffectType EffectType => EffectType.Marked;
        
        public override bool IsPositive => false;
        
        public override bool PlaysBarkAppliedOnCaster => false;
        public override bool PlaysBarkAppliedOnEnemy => true;
        public override bool PlaysBarkAppliedOnAlly => false;
    }
}