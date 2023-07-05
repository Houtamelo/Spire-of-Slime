using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Utils.Math;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Core.Combat.Scripts.Effects.Types.Heal
{
    public record HealScript(float Power = 1) : StatusScript
    {
        public float Power { get; set; } = Power;

        public override bool IsPositive => true;
        
        public override bool PlaysBarkAppliedOnCaster => true;
        public override bool PlaysBarkAppliedOnEnemy => false;
        public override bool PlaysBarkAppliedOnAlly => true;
        
        public override StatusToApply GetStatusToApply(CharacterStateMachine caster, CharacterStateMachine target, bool crit, ISkill skill = null) => new HealToApply(caster, target, crit, skill, ScriptOrigin: this, Power);

        public override StatusResult ApplyEffect(CharacterStateMachine caster, CharacterStateMachine target, bool crit, ISkill skill = null)
        {
            HealToApply effectStruct = new(caster, target, crit, skill, this, Power);
            return ProcessModifiersAndTryApply(effectStruct);
        }

        public static void ProcessModifiers(HealToApply effectStruct)
        {
            IStatusApplierModule applierModule = effectStruct.Caster.StatusApplierModule;
            applierModule.ModifyEffectApplying(ref effectStruct);
            
            IStatusReceiverModule receiverModule = effectStruct.Target.StatusModule;
            receiverModule.ModifyEffectReceiving(ref effectStruct);
            
            if (effectStruct.FromCrit)
                effectStruct.Power *= StatusScriptDurationBased.DurationMultiplierOnCrit;
        }

        public static StatusResult ProcessModifiersAndTryApply(HealToApply effectStruct)
        {
            ProcessModifiers(effectStruct);
            return TryApply(ref effectStruct);
        }

        private static StatusResult TryApply(ref HealToApply effectStruct)
        {
            FullCharacterState targetState = effectStruct.Target.StateEvaluator.FullPureEvaluate();
            if (targetState.Defeated || targetState.Corpse || targetState.Grappled)
                return StatusResult.Failure(effectStruct.Caster, effectStruct.Target, generatesInstance: false);
            
            uint casterLowerBound = effectStruct.Caster.StatsModule.BaseDamageLower;
            uint casterUpperBound = effectStruct.Caster.StatsModule.BaseDamageUpper;
            uint heal = (uint) Random.Range(casterLowerBound, casterUpperBound + 1);
            heal = (heal * effectStruct.Power).CeilToUInt();

            effectStruct.Target.StaminaModule.Value.DoHeal(heal, isOvertime: false);
            return StatusResult.Success(effectStruct.Caster, effectStruct.Target, statusInstance: null, generatesInstance: false, EffectType.Heal);
        }

        public override float ComputePoints(ref SkillStruct skillStruct, CharacterStateMachine target)
        {
            if (target.StaminaModule.IsNone || target.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Corpse or CharacterState.Grappled)
                return 0f;
            
            HealToApply effectStruct = new(skillStruct.Caster, target, false, skillStruct.Skill, this, Power);
            ProcessModifiers(effectStruct);

            IStaminaModule targetStamina = target.StaminaModule.Value;
            float averageHeal = effectStruct.Power * (skillStruct.Caster.StatsModule.BaseDamageLower + skillStruct.Caster.StatsModule.BaseDamageUpper) / 2f;
            averageHeal = Mathf.Min(averageHeal, targetStamina.ActualMax - targetStamina.GetCurrent());
            float staminaPercentage = Mathf.Clamp((float)targetStamina.GetCurrent() / targetStamina.ActualMax, 0.165f, 0.75f);
            float healPriority = HeuristicConstants.GetHealPriority(staminaPercentage);
            
            return averageHeal * healPriority * HeuristicConstants.HealMultiplier;
        }

        public override string Description => StatusScriptDescriptions.Get(this);
        public override EffectType EffectType => EffectType.Heal;
    }
}