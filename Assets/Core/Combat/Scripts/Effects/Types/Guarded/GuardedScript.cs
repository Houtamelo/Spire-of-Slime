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

namespace Core.Combat.Scripts.Effects.Types.Guarded
{
    public record GuardedScript(bool Permanent, TSpan BaseDuration) : StatusScriptDurationBased(Permanent, BaseDuration)
    {
        [NotNull]
        public override StatusToApply GetStatusToApply(CharacterStateMachine caster, CharacterStateMachine target, bool crit, [CanBeNull] ISkill skill = null) 
            => new GuardedToApply(caster, target, crit, skill, ScriptOrigin: this, BaseDuration, Permanent);

        public override StatusResult ApplyEffect(CharacterStateMachine caster, CharacterStateMachine target, bool crit, [CanBeNull] ISkill skill = null)
        {
            GuardedToApply effectStruct = new(caster, target, crit, skill, ScriptOrigin: this, BaseDuration, Permanent);
            return ProcessModifiersAndTryApply(effectStruct);
        }

        public static void ProcessModifiers(GuardedToApply effectStruct)
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

        public static StatusResult ProcessModifiersAndTryApply(GuardedToApply effectStruct)
        {
            ProcessModifiers(effectStruct);

            return TryApply(ref effectStruct);
        }

        private static StatusResult TryApply([NotNull] ref GuardedToApply effectStruct)
        {
            FullCharacterState targetState = effectStruct.Target.StateEvaluator.FullPureEvaluate();
            if (targetState.Defeated || targetState.Corpse || targetState.Grappled)
                return StatusResult.Failure(effectStruct.Caster, effectStruct.Target, generatesInstance: false);
            
            CharacterStateMachine caster = effectStruct.Caster;
            CharacterStateMachine target = effectStruct.Target;
            if (caster.PositionHandler.IsLeftSide != target.PositionHandler.IsLeftSide)
                return StatusResult.Failure(caster, target, generatesInstance: true);
            
            Option<StatusInstance> option = Guarded.CreateInstance(effectStruct.Duration, effectStruct.Permanent, owner: target, caster);
            if (StatusVFXManager.AssertInstance(out StatusVFXManager statusEffectVFXManager))
                statusEffectVFXManager.Enqueue(StatusCueHandler.FromAppliedStatus(effectStruct.Target, StatusCueHandler.StandardValidator, EffectType.Guarded, success: option.IsSome));

            if (option.IsSome && caster.Display.AssertSome(out DisplayModule targetDisplay)) // only one guarded allowed per team
                foreach (CharacterStateMachine character in targetDisplay.CombatManager.Characters.GetOnSide(effectStruct.Target))
                {
                    if (character != target)
                        character.StatusReceiverModule.DeactivateStatusByType(effectType: EffectType.Guarded);
                }

            return new StatusResult(caster, target, success: option.IsSome, statusInstance: option.SomeOrDefault(), generatesInstance: true, EffectType.Guarded);
        }

        public override float ComputePoints(ref SkillStruct skillStruct, [NotNull] CharacterStateMachine target)
        {
            if (target.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Corpse or CharacterState.Grappled)
                return 0f;
            
            GuardedToApply effectStruct = new(skillStruct.Caster, target, FromCrit: false, skillStruct.Skill, ScriptOrigin: this, BaseDuration, Permanent);
            ProcessModifiers(effectStruct);

            float penaltyForAllyAlreadyGuarded = 1f;
            if (target.Display.TrySome(out DisplayModule targetDisplay))
                foreach (CharacterStateMachine ally in targetDisplay.CombatManager.Characters.GetOnSide(target))
                {
                    if (ally == target || ally.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Corpse)
                        continue;
                    
                    foreach (StatusInstance status in ally.StatusReceiverModule.GetAll)
                    {
                        if (status.EffectType is not EffectType.Guarded || status.IsDeactivated)
                            continue;

                        penaltyForAllyAlreadyGuarded = HeuristicConstants.AllyAlreadyHasGuardedMultiplier / (1 + status.Duration.FloatSeconds);
                        break;
                    }

                    if (penaltyForAllyAlreadyGuarded < 1f)
                        break;
                }

            float durationMultiplier = effectStruct.Permanent ? HeuristicConstants.PermanentMultiplier : effectStruct.Duration.FloatSeconds * HeuristicConstants.DurationMultiplier;
            float guardedPoints = durationMultiplier * HeuristicConstants.GuardedMultiplier * penaltyForAllyAlreadyGuarded;
            return guardedPoints;
        }


        [NotNull]
        public override string Description => StatusScriptDescriptions.Get(this);
        public override EffectType EffectType => EffectType.Guarded;
        
        public override bool IsPositive => true;
        
        public override bool PlaysBarkAppliedOnCaster => false;
        public override bool PlaysBarkAppliedOnEnemy => false;
        public override bool PlaysBarkAppliedOnAlly => false;
    }
}