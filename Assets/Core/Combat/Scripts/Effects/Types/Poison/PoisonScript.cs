using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Cues;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using UnityEngine;
using Utils.Patterns;
using Random = UnityEngine.Random;

namespace Core.Combat.Scripts.Effects.Types.Poison
{
    public record PoisonScript(bool Permanent, float BaseDuration, float BaseApplyChance, uint BasePoisonPerTime) : StatusScriptDurationBased(Permanent, BaseDuration)
    {
        public float BaseApplyChance { get; protected set; } = BaseApplyChance;
        public uint BasePoisonPerTime { get; set; } = BasePoisonPerTime;

        public override bool IsPositive => false;
        
        public override bool PlaysBarkAppliedOnCaster => false;
        public override bool PlaysBarkAppliedOnEnemy => true;
        public override bool PlaysBarkAppliedOnAlly => false;

        public override StatusToApply GetStatusToApply(CharacterStateMachine caster, CharacterStateMachine target, bool crit, ISkill skill = null) 
            => new PoisonToApply(caster, target, crit, skill, this, BaseDuration, Permanent, BaseApplyChance, BasePoisonPerTime);

        public override StatusResult ApplyEffect(CharacterStateMachine caster, CharacterStateMachine target, bool crit, ISkill skill = null)
        {
            PoisonToApply effectStruct = new(caster, target, crit, skill, this, BaseDuration, Permanent, BaseApplyChance, BasePoisonPerTime);
            return ProcessModifiersAndTryApply(effectStruct);
        }

        public static void ProcessModifiers(PoisonToApply effectStruct)
        {
            IStatusApplierModule applierModule = effectStruct.Caster.StatusApplierModule;
            if (effectStruct.Caster == effectStruct.Target)
                effectStruct.ApplyChance = 1;
            else
            {
                effectStruct.ApplyChance += -effectStruct.Target.ResistancesModule.GetPoisonResistance() + applierModule.BasePoisonApplyChance;
                if (effectStruct.FromCrit)
                    effectStruct.ApplyChance += BonusApplyChanceOnCrit;
            }
            applierModule.ModifyEffectApplying(ref effectStruct);
            
            IStatusModule receiverModule = effectStruct.Target.StatusModule;
            receiverModule.ModifyEffectReceiving(ref effectStruct);
            
            if (effectStruct.FromCrit)
                effectStruct.Duration *= DurationMultiplierOnCrit;
        }

        public static StatusResult ProcessModifiersAndTryApply(PoisonToApply effectStruct)
        {
            ProcessModifiers(effectStruct);
            return TryApply(ref effectStruct);
        }

        private static StatusResult TryApply(ref PoisonToApply poisonStruct)
        {
            FullCharacterState targetState = poisonStruct.Target.StateEvaluator.FullPureEvaluate();
            if (targetState.Defeated || targetState.Corpse || targetState.Grappled)
                return StatusResult.Failure(poisonStruct.Caster, poisonStruct.Target, generatesInstance: false);
            
            bool success = Random.value < poisonStruct.ApplyChance;

            Option<StatusInstance> option = success ? Poison.CreateInstance(poisonStruct.Duration, poisonStruct.IsPermanent, poisonStruct.Target, poisonStruct.Caster, poisonStruct.PoisonPerTime) : Option.None;

            if (StatusVFXManager.AssertInstance(out StatusVFXManager statusEffectVFXManager))
                statusEffectVFXManager.Enqueue(StatusCueHandler.FromAppliedStatus(poisonStruct.Target, StatusCueHandler.StandardValidator, EffectType.Poison, option.IsSome));

            return new StatusResult(poisonStruct.Caster, poisonStruct.Target, success: option.IsSome, statusInstance: option.SomeOrDefault(), generatesInstance: true, EffectType.Poison);
        }
        
        public override float ComputePoints(ref SkillStruct skillStruct, CharacterStateMachine target)
        {
            if (target.StaminaModule.IsNone || target.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Corpse or CharacterState.Downed or CharacterState.Grappled)
                return 0f;
            
            PoisonToApply effectStruct = new(skillStruct.Caster, target, false, skillStruct.Skill, this, BaseDuration, Permanent, BaseApplyChance, BasePoisonPerTime);
            ProcessModifiers(effectStruct);

            float applyChance = effectStruct.ApplyChance;
            float totalDamage = effectStruct.PoisonPerTime * effectStruct.Duration;
            float durationMultiplier = Permanent ? HeuristicConstants.PermanentMultiplier : Mathf.Pow(HeuristicConstants.PenaltyForOvertime, effectStruct.Duration + 1);
            
            float points = applyChance * totalDamage * durationMultiplier * HeuristicConstants.DamageMultiplier;
            return points * -1f;
        }

        public override EffectType EffectType => EffectType.Poison;
        public override string Description => StatusScriptDescriptions.Get(this);
    }
}