using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Cues;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Utils.Patterns;
using Utils.Patterns;

namespace Core.Combat.Scripts.Effects.Types.Marked
{
    public record MarkedScript(bool Permanent, float BaseDuration) : StatusScriptDurationBased(Permanent, BaseDuration)
    {
        public override bool IsPositive => false;
        
        public override bool PlaysBarkAppliedOnCaster => false;
        public override bool PlaysBarkAppliedOnEnemy => true;
        public override bool PlaysBarkAppliedOnAlly => false;
        
        public override StatusToApply GetStatusToApply(CharacterStateMachine caster, CharacterStateMachine target, bool crit, ISkill skill = null) => new MarkedToApply(caster, target, crit, skill, this, BaseDuration, Permanent);

        public override StatusResult ApplyEffect(CharacterStateMachine caster, CharacterStateMachine target, bool crit, ISkill skill = null)
        {
            MarkedToApply effectStruct = new(caster, target, crit, skill, this, BaseDuration, Permanent);
            return ProcessModifiersAndTryApply(effectStruct);
        }

        public static void ProcessModifiers(MarkedToApply effectStruct)
        {
            IStatusApplierModule applierModule = effectStruct.Caster.StatusApplierModule;
            applierModule.ModifyEffectApplying(ref effectStruct);
            
            IStatusReceiverModule receiverModule = effectStruct.Target.StatusModule;
            receiverModule.ModifyEffectReceiving(ref effectStruct);
            
            if (effectStruct.FromCrit)
                effectStruct.Duration *= DurationMultiplierOnCrit;
        }

        public static StatusResult ProcessModifiersAndTryApply(MarkedToApply effectStruct)
        {
            ProcessModifiers(effectStruct);
            return TryApply(ref effectStruct);
        }

        private static StatusResult TryApply(ref MarkedToApply markedStruct)
        {
            FullCharacterState targetState = markedStruct.Target.StateEvaluator.FullPureEvaluate();
            if (targetState.Defeated || targetState.Corpse || targetState.Grappled)
                return StatusResult.Failure(markedStruct.Caster, markedStruct.Target, generatesInstance: false);
            
            Option<StatusInstance> option = Marked.CreateInstance(markedStruct.Duration, markedStruct.IsPermanent, markedStruct.Target, markedStruct.Caster);
            
            if (StatusVFXManager.AssertInstance(out StatusVFXManager statusEffectVFXManager))
                statusEffectVFXManager.Enqueue(StatusCueHandler.FromAppliedStatus(markedStruct.Target, StatusCueHandler.StandardValidator, EffectType.Marked, success: option.IsSome));

            return new StatusResult(markedStruct.Caster, markedStruct.Target, success: option.IsSome, statusInstance: option.SomeOrDefault(), generatesInstance: true, EffectType.Marked);
        }

        public override float ComputePoints(ref SkillStruct skillStruct, CharacterStateMachine target)
        {
            if (target.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Corpse or CharacterState.Grappled or CharacterState.Downed)
                return 0f;
            
            MarkedToApply effectStruct = new(skillStruct.Caster, target, false, skillStruct.Skill, this, BaseDuration, Permanent);
            ProcessModifiers(effectStruct);

            float penaltyForTargetAllyAlreadyMarked = 1f;
            if (target.Display.IsSome)
            {
                foreach (CharacterStateMachine ally in target.Display.Value.CombatManager.Characters.GetOnSide(target))
                {
                    if (ally == target || ally.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Corpse)
                        continue;
                    
                    foreach (StatusInstance status in ally.StatusModule.GetAll)
                    {
                        if (status.EffectType is not EffectType.Marked || status.IsDeactivated)
                            continue;
                        
                        penaltyForTargetAllyAlreadyMarked = HeuristicConstants.AllyAlreadyHasMarkedMultiplier / (1 + status.Duration);
                        break;
                    }

                    if (penaltyForTargetAllyAlreadyMarked < 1f)
                        break;
                }
            }
            
            float durationMultiplier = effectStruct.IsPermanent ? HeuristicConstants.PermanentMultiplier : effectStruct.Duration * HeuristicConstants.DurationMultiplier;
            float points = durationMultiplier * HeuristicConstants.MarkedMultiplier * penaltyForTargetAllyAlreadyMarked;
            return points * -1f;
        }

        public override string Description => StatusScriptDescriptions.Get(this);
        public override EffectType EffectType => EffectType.Marked;
    }
}