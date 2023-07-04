using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Cues;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using UnityEngine;

namespace Core.Combat.Scripts.Effects.Types.Stun
{
    public record StunScript(float BaseDuration = 0.5f) : StatusScriptDurationBased(false, BaseDuration)
    {
        public override bool IsPositive => false;
        
        public override bool PlaysBarkAppliedOnCaster => false;
        public override bool PlaysBarkAppliedOnEnemy => true;
        public override bool PlaysBarkAppliedOnAlly => false;

        public override StatusToApply GetStatusToApply(CharacterStateMachine caster, CharacterStateMachine target, bool crit, ISkill skill = null) 
            => new StunToApply(caster, target, crit, skill, this, BaseDuration, false);

        public override StatusResult ApplyEffect(CharacterStateMachine caster, CharacterStateMachine target, bool crit, ISkill skill = null)
        {
            StunToApply effectStruct = new(caster, target, crit, skill, this, BaseDuration, false);
            return ProcessModifiersAndTryApply(effectStruct);
        }

        public static void ProcessModifiers(StunToApply effectStruct)
        {
            IStatusApplierModule applierModule = effectStruct.Caster.StatusApplierModule;
            applierModule.ModifyEffectApplying(ref effectStruct);
            
            IStatusReceiverModule receiverModule = effectStruct.Target.StatusModule;
            receiverModule.ModifyEffectReceiving(ref effectStruct);
            
            if (effectStruct.FromCrit)
                effectStruct.Duration *= DurationMultiplierOnCrit;
        }

        public static StatusResult ProcessModifiersAndTryApply(StunToApply effectStruct)
        {
            ProcessModifiers(effectStruct);
            return TryApply(ref effectStruct);
        }

        private static StatusResult TryApply(ref StunToApply stunStruct)
        {
            FullCharacterState targetState = stunStruct.Target.StateEvaluator.FullPureEvaluate();
            if (targetState.Defeated || targetState.Corpse || targetState.Grappled)
                return StatusResult.Failure(stunStruct.Caster, stunStruct.Target, generatesInstance: false);

            CharacterStateMachine target = stunStruct.Target;
            target.StunModule.SetInitial(stunStruct.Duration);
            
            if (StatusVFXManager.AssertInstance(out StatusVFXManager statusVFXManager) && target.Display.AssertSome(out CharacterDisplay display))
            {
                StatusCueHandler handler = StatusCueHandler.FromAppliedStatus(target, StatusCueHandler.StandardValidator, EffectType.Stun, success: true);
                statusVFXManager.Enqueue(handler);
                handler.Started += () =>
                {
                    if (display != null)
                        target.StunModule.ForceUpdateDisplay(display);
                };
            }

            return StatusResult.Success(stunStruct.Caster, stunStruct.Target, statusInstance: null, generatesInstance: false, EffectType.Stun);
        }

        public override float ComputePoints(ref SkillStruct skillStruct, CharacterStateMachine target)
        {
            if (target.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Corpse or CharacterState.Grappled)
                return 0f;
            
            StunToApply effectStruct = new(skillStruct.Caster, target, false, skillStruct.Skill, this, BaseDuration, false);
            ProcessModifiers(effectStruct);

            float duration = effectStruct.Duration - target.StunModule.GetRemaining(); // stun is not additive, only the biggest one remains
            if (duration <= 0f)
                return 0f;

            float durationPoints = Mathf.Pow(duration, 0.85f); // magic to account for StunRecoverySpeed increases overtime, this is not 100% accurate but it's close enough
            
            float durationMultiplier = durationPoints * HeuristicConstants.DurationMultiplier / target.ResistancesModule.GetStunRecoverySpeed();
            float points = durationMultiplier * HeuristicConstants.StunMultiplier;
            return points * -1f;
        }

        public override EffectType EffectType => EffectType.Stun;
        public override string Description => StatusScriptDescriptions.Get(this);
    }
}