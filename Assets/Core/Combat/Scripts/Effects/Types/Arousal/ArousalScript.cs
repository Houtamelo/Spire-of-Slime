using Core.Combat.Scripts.Animations;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Behaviour.Modules;
using Core.Combat.Scripts.Cues;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Save_Management.SaveObjects;
using Core.Utils.Math;
using Core.Utils.Patterns;
using JetBrains.Annotations;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Core.Combat.Scripts.Effects.Types.Arousal
{
    public record ArousalScript(bool Permanent, TSpan BaseDuration, int BaseApplyChance, int BaseLustPerSecond = 1) : StatusScriptDurationBased(Permanent, BaseDuration)
    {
        public int BaseApplyChance { get; protected set; } = BaseApplyChance;
        public int BaseLustPerSecond { get; protected set; } = BaseLustPerSecond;

        [NotNull]
        public override StatusToApply GetStatusToApply(CharacterStateMachine caster, CharacterStateMachine target, bool crit, [CanBeNull] ISkill skill = null) 
            => new ArousalToApply(caster, target, crit, skill, ScriptOrigin: this, BaseDuration, Permanent, BaseApplyChance, BaseLustPerSecond);

        public override StatusResult ApplyEffect(CharacterStateMachine caster, CharacterStateMachine target, bool crit, [CanBeNull] ISkill skill = null)
        {
            ArousalToApply arousalStruct = new(caster, target, crit, skill, ScriptOrigin: this, BaseDuration, Permanent, BaseApplyChance, BaseLustPerSecond);
            return ProcessModifiersAndTryApply(arousalStruct);
        }

        public static void ProcessModifiers(ArousalToApply effectStruct)
        {
            IStatusApplierModule applierModule = effectStruct.Caster.StatusApplierModule;

            if (effectStruct.Caster == effectStruct.Target)
                effectStruct.ApplyChance = 100;
            else
                effectStruct.ApplyChance += applierModule.GetArousalApplyChance();

            if (effectStruct.FromCrit)
                effectStruct.ApplyChance += BonusApplyChanceOnCrit;

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

        public static StatusResult ProcessModifiersAndTryApply(ArousalToApply effectStruct)
        {
            ProcessModifiers(effectStruct);
            return TryApply(ref effectStruct);
        }

        private static StatusResult TryApply([NotNull] ref ArousalToApply effectStruct)
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
            
            bool success = Save.Random.Next(100) < effectStruct.ApplyChance;
            Option<StatusInstance> option = success ? Arousal.CreateInstance(ref effectStruct) : Option<StatusInstance>.None;

            if (StatusVFXManager.AssertInstance(out StatusVFXManager statusEffectVFXManager))
                statusEffectVFXManager.Enqueue(StatusCueHandler.FromAppliedStatus(effectStruct.Target, StatusCueHandler.StandardValidator, EffectType.Arousal, option.IsSome));

            return new StatusResult(effectStruct.Caster, effectStruct.Target, success: option.IsSome, statusInstance: option.SomeOrDefault(), generatesInstance: true, EffectType.Arousal);
        }

        public override float ComputePoints(ref SkillStruct skillStruct, [NotNull] CharacterStateMachine target)
        {
            if (target.LustModule.IsNone || target.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Corpse)
                return 0f;

            ArousalToApply effectStruct = new(skillStruct.Caster, target, FromCrit: false, skillStruct.Skill, ScriptOrigin: this, BaseDuration,
                                              Permanent, BaseApplyChance, BaseLustPerSecond);
            ProcessModifiers(effectStruct);

            float applyChancePercentage = Mathf.Clamp01(effectStruct.ApplyChance / 100f);

            int totalLust = Mathd.CeilToInt(effectStruct.Duration.Seconds * effectStruct.LustPerSecond);

            float durationMultiplier = Permanent ? HeuristicConstants.PermanentMultiplier : Mathf.Pow(HeuristicConstants.PenaltyForOvertime, effectStruct.Duration.FloatSeconds + 1.0f);

            float points = totalLust * durationMultiplier * HeuristicConstants.LustMultiplier * applyChancePercentage;
            return -1f * points;
        }

        [NotNull]
        public override string Description => StatusScriptDescriptions.Get(this);
        public override EffectType EffectType => EffectType.Arousal;
        
        public override bool IsPositive => false;
        
        public override bool PlaysBarkAppliedOnCaster => false;
        public override bool PlaysBarkAppliedOnEnemy => true;
        public override bool PlaysBarkAppliedOnAlly => false;
    }
}