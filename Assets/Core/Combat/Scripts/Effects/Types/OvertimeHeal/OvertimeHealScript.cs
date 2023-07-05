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

namespace Core.Combat.Scripts.Effects.Types.OvertimeHeal
{
    public record OvertimeHealScript(bool Permanent, float BaseDuration, uint BaseHealPerTime = 1) : StatusScriptDurationBased(Permanent, BaseDuration)
    {
        public uint BaseHealPerTime { get; protected set; } = BaseHealPerTime;
        
        public override bool IsPositive => true;
        
        public override bool PlaysBarkAppliedOnCaster => true;
        public override bool PlaysBarkAppliedOnEnemy => false;
        public override bool PlaysBarkAppliedOnAlly => true;

        public override StatusToApply GetStatusToApply(CharacterStateMachine caster, CharacterStateMachine target, bool crit, ISkill skill = null) 
            => new OvertimeHealToApply(caster, target, crit, skill, this, BaseDuration, Permanent, BaseHealPerTime);

        public override StatusResult ApplyEffect(CharacterStateMachine caster, CharacterStateMachine target, bool crit, ISkill skill = null)
        {
            OvertimeHealToApply overtimeHealStruct = new(caster, target, crit, skill, this, BaseDuration, Permanent, BaseHealPerTime);
            return ProcessModifiersAndTryApply(overtimeHealStruct);
        }

        public static void ProcessModifiers(OvertimeHealToApply effectStruct)
        {
            IStatusApplierModule applierModule = effectStruct.Caster.StatusApplierModule;
            applierModule.ModifyEffectApplying(ref effectStruct);

            IStatusModule receiverModule = effectStruct.Target.StatusModule;
            receiverModule.ModifyEffectReceiving(ref effectStruct);
            
            if (effectStruct.FromCrit)
                effectStruct.Duration *= DurationMultiplierOnCrit;
        }

        public static StatusResult ProcessModifiersAndTryApply(OvertimeHealToApply effectStruct)
        {
            ProcessModifiers(effectStruct);
            return TryApply(ref effectStruct);
        }

        private static StatusResult TryApply(ref OvertimeHealToApply overtimeHealStruct)
        {
            FullCharacterState targetState = overtimeHealStruct.Target.StateEvaluator.FullPureEvaluate();
            if (targetState.Defeated || targetState.Corpse || targetState.Grappled)
                return StatusResult.Failure(overtimeHealStruct.Caster, overtimeHealStruct.Target, generatesInstance: false);
            
            Option<StatusInstance> option = OvertimeHeal.CreateInstance(overtimeHealStruct.Duration, overtimeHealStruct.IsPermanent, overtimeHealStruct.Target, overtimeHealStruct.Caster, overtimeHealStruct.HealPerTime);

            if (StatusVFXManager.AssertInstance(out StatusVFXManager statusEffectVFXManager))
                statusEffectVFXManager.Enqueue(StatusCueHandler.FromAppliedStatus(overtimeHealStruct.Target, StatusCueHandler.StandardValidator, EffectType.OvertimeHeal, success: option.IsSome));

            return new StatusResult(overtimeHealStruct.Caster, overtimeHealStruct.Target, success: option.IsSome, statusInstance: option.SomeOrDefault(), generatesInstance: true, EffectType.OvertimeHeal);
        }

        public override float ComputePoints(ref SkillStruct skillStruct, CharacterStateMachine target)
        {
            if (target.StaminaModule.IsNone || target.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Corpse or CharacterState.Grappled)
                return 0f;

            OvertimeHealToApply effectStruct = new(skillStruct.Caster, target, false, skillStruct.Skill, this, BaseDuration, Permanent, BaseHealPerTime);
            ProcessModifiers(effectStruct);


            IStaminaModule targetStamina = target.StaminaModule.Value;
            float totalHeal = effectStruct.HealPerTime * effectStruct.Duration;
            totalHeal = Mathf.Min(totalHeal, targetStamina.ActualMax - targetStamina.GetCurrent());
            float staminaPercentage = Mathf.Clamp((float)targetStamina.GetCurrent() / targetStamina.ActualMax, 0.165f, 0.75f);
            float healPriority = HeuristicConstants.GetHealPriority(staminaPercentage);
            
            float durationMultiplier = Permanent ? HeuristicConstants.PermanentMultiplier : Mathf.Pow(HeuristicConstants.PenaltyForOvertime, effectStruct.Duration + 1);
            
            return totalHeal * healPriority * HeuristicConstants.HealMultiplier * durationMultiplier;
        }

        public override EffectType EffectType => EffectType.OvertimeHeal;
        public override string Description => StatusScriptDescriptions.Get(this);
    }
}