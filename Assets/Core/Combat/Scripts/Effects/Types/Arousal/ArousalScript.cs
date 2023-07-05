using Core.Combat.Scripts.Animations;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Cues;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Utils.Patterns;
using UnityEngine;
using Utils.Patterns;
using Random = UnityEngine.Random;

namespace Core.Combat.Scripts.Effects.Types.Arousal
{
    public record ArousalScript(bool Permanent, float BaseDuration, float BaseApplyChance = 1, uint BaseLustPerTime = 1) : StatusScriptDurationBased(Permanent, BaseDuration)
    {
        public float BaseApplyChance { get; protected set; } = BaseApplyChance;
        public uint BaseLustPerTime { get; protected set; } = BaseLustPerTime;

        public override bool IsPositive => false;
        
        public override bool PlaysBarkAppliedOnCaster => false;
        public override bool PlaysBarkAppliedOnEnemy => true;
        public override bool PlaysBarkAppliedOnAlly => false;

        public override StatusToApply GetStatusToApply(CharacterStateMachine caster, CharacterStateMachine target, bool crit, ISkill skill = null) 
            => new ArousalToApply(caster, target, crit, skill, this, BaseDuration, Permanent, BaseApplyChance, BaseLustPerTime);

        public override StatusResult ApplyEffect(CharacterStateMachine caster, CharacterStateMachine target, bool crit, ISkill skill = null)
        {
            ArousalToApply arousalStruct = new(caster, target, crit, skill, this, BaseDuration, Permanent, BaseApplyChance, BaseLustPerTime);
            return ProcessModifiersAndTryApply(arousalStruct);
        }

        public static void ProcessModifiers(ArousalToApply effectStruct)
        {
            IStatusApplierModule applierModule = effectStruct.Caster.StatusApplierModule;
            if (effectStruct.Caster == effectStruct.Target)
                effectStruct.ApplyChance = 1;
            else
            {
                effectStruct.ApplyChance += applierModule.GetArousalApplyChance();
                if (effectStruct.FromCrit)
                    effectStruct.ApplyChance += BonusApplyChanceOnCrit;
            }
            
            applierModule.ModifyEffectApplying(ref effectStruct);
            
            IStatusModule receiverModule = effectStruct.Target.StatusModule;
            receiverModule.ModifyEffectReceiving(ref effectStruct);
            
            if (effectStruct.FromCrit)
                effectStruct.Duration *= DurationMultiplierOnCrit;
        }

        public static StatusResult ProcessModifiersAndTryApply(ArousalToApply effectStruct)
        {
            ProcessModifiers(effectStruct);
            return TryApply(ref effectStruct);
        }

        private static StatusResult TryApply(ref ArousalToApply effectStruct)
        {
            FullCharacterState targetState = effectStruct.Target.StateEvaluator.FullPureEvaluate();
            if (targetState.Defeated || targetState.Corpse || targetState.Grappled)
                return StatusResult.Failure(effectStruct.Caster, effectStruct.Target, generatesInstance: false);
            
            if (effectStruct.Target.LustModule.IsNone)
            {
                if (StatusVFXManager.AssertInstance(out StatusVFXManager statusVFXManager))
                    statusVFXManager.Enqueue(StatusCueHandler.FromAppliedStatus(effectStruct.Target, AnimationRoutineInfo.StandardValidation, EffectType.Arousal, success: false));
                
                return StatusResult.Failure(effectStruct.Caster, effectStruct.Target, generatesInstance: true);
            }
            
            bool success = Random.value < effectStruct.ApplyChance;
            Option<StatusInstance> option = success ? Arousal.CreateInstance(ref effectStruct) : Option<StatusInstance>.None;

            if (StatusVFXManager.AssertInstance(out StatusVFXManager statusEffectVFXManager))
                statusEffectVFXManager.Enqueue(StatusCueHandler.FromAppliedStatus(effectStruct.Target, StatusCueHandler.StandardValidator, EffectType.Arousal, option.IsSome));

            return new StatusResult(effectStruct.Caster, effectStruct.Target, success: option.IsSome, statusInstance: option.SomeOrDefault(), generatesInstance: true, EffectType.Arousal);
        }


        public float GetEstimatedTotalLustWithoutComposure() => BaseLustPerTime * BaseDuration;

        public override float ComputePoints(ref SkillStruct skillStruct, CharacterStateMachine target)
        {
            if (target.LustModule.IsNone || target.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Corpse)
                return 0f;
            
            ArousalToApply effectStruct = new(skillStruct.Caster, target, false, skillStruct.Skill, this, BaseDuration,
                                             Permanent, BaseApplyChance, BaseLustPerTime);
            ProcessModifiers(effectStruct);

            float applyChance = Mathf.Clamp(effectStruct.ApplyChance, 0f, 1f);

            float totalLust = effectStruct.Duration * effectStruct.LustPerTime;

            float durationMultiplier = Permanent ? HeuristicConstants.PermanentMultiplier : Mathf.Pow(HeuristicConstants.PenaltyForOvertime, effectStruct.Duration + 1);

            float points = totalLust * durationMultiplier * HeuristicConstants.LustMultiplier * applyChance;
            return -1f * points;
        }

        public override string Description => StatusScriptDescriptions.Get(this);
        public override EffectType EffectType => EffectType.Arousal;
    }
}