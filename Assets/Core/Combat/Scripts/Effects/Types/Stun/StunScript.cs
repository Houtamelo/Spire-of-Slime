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
using UnityEngine;

namespace Core.Combat.Scripts.Effects.Types.Stun
{
    public record StunScript(int StunPower) : StatusScript
    {
        private const int CritPowerDelta = 50;

        private static int GetCritStunPower(int baseValue)
        {
            // ReSharper disable once ArrangeRedundantParentheses // not actually redundant! if 2 is divided by 3 first, it will be 0, I have no idea what the compiler is going to do first so I'm being explicit
            int twoThirds = (baseValue * 2) / 3;
            return twoThirds < CritPowerDelta ? (baseValue + twoThirds) : (baseValue + CritPowerDelta);
        }
        
        [NotNull]
        public override StatusToApply GetStatusToApply(CharacterStateMachine caster, CharacterStateMachine target, bool crit, [CanBeNull] ISkill skill = null) 
            => new StunToApply(caster, target, crit, skill, ScriptOrigin: this, StunPower, Permanent: false);

        public override StatusResult ApplyEffect(CharacterStateMachine caster, CharacterStateMachine target, bool crit, [CanBeNull] ISkill skill = null)
        {
            StunToApply effectStruct = new(caster, target, crit, skill, ScriptOrigin: this, StunPower, Permanent: false);
            return ProcessModifiersAndTryApply(effectStruct);
        }

        public static void ProcessModifiers(StunToApply effectStruct)
        {
            IStatusApplierModule applierModule = effectStruct.Caster.StatusApplierModule;
            applierModule.ModifyEffectApplying(ref effectStruct);
            
            IStatusReceiverModule receiverModule = effectStruct.Target.StatusReceiverModule;
            receiverModule.ModifyEffectReceiving(ref effectStruct);
            
            if (effectStruct.FromCrit)
                effectStruct.StunPower = GetCritStunPower(effectStruct.StunPower);
        }

        public static StatusResult ProcessModifiersAndTryApply(StunToApply effectStruct)
        {
            ProcessModifiers(effectStruct);
            return TryApply(ref effectStruct);
        }

        private static StatusResult TryApply([NotNull] ref StunToApply stunStruct)
        {
            FullCharacterState targetState = stunStruct.Target.StateEvaluator.FullPureEvaluate();
            if (targetState.Defeated || targetState.Corpse || targetState.Grappled)
                return StatusResult.Failure(stunStruct.Caster, stunStruct.Target, generatesInstance: false);

            CharacterStateMachine target = stunStruct.Target;
            target.StunModule.AddFromPower(stunStruct.StunPower);
            
            //todo! add failure cue
            
            if (StatusVFXManager.AssertInstance(out StatusVFXManager statusVFXManager) && target.Display.AssertSome(out DisplayModule display))
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

        public override float ComputePoints(ref SkillStruct skillStruct, [NotNull] CharacterStateMachine target)
        {
            if (target.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Corpse or CharacterState.Grappled)
                return 0f;
            
            StunToApply effectStruct = new(skillStruct.Caster, target, FromCrit: false, skillStruct.Skill, ScriptOrigin: this, StunPower, Permanent: false);
            ProcessModifiers(effectStruct);

            TSpan duration = IStunModule.CalculateDuration(StunPower, target.StunModule.GetStunMitigation()).duration;
            if (duration.Ticks <= 0)
                return 0;
            
            float durationMultiplier = duration.FloatSeconds * HeuristicConstants.DurationMultiplier;
            float points = durationMultiplier * HeuristicConstants.StunMultiplier;
            return points * -1f;
        }
        
        public override EffectType EffectType => EffectType.Stun;
        [NotNull]
        public override string Description => StatusScriptDescriptions.Get(this);
        
        public override bool IsPositive => false;
        
        public override bool PlaysBarkAppliedOnCaster => false;
        public override bool PlaysBarkAppliedOnEnemy => true;
        public override bool PlaysBarkAppliedOnAlly => false;
    }
}