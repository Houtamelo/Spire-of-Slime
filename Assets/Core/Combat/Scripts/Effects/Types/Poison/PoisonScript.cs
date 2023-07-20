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

namespace Core.Combat.Scripts.Effects.Types.Poison
{
    public record PoisonScript(bool Permanent, TSpan BaseDuration, int BaseApplyChance, int BasePoisonPerTime) : StatusScriptDurationBased(Permanent, BaseDuration)
    {
        public int BaseApplyChance { get; protected set; } = BaseApplyChance;
        public int BasePoisonPerTime { get; set; } = BasePoisonPerTime;

        [NotNull]
        public override StatusToApply GetStatusToApply(CharacterStateMachine caster, CharacterStateMachine target, bool crit, [CanBeNull] ISkill skill = null) 
            => new PoisonToApply(caster, target, crit, skill, ScriptOrigin: this, BaseDuration, Permanent, BaseApplyChance, BasePoisonPerTime);

        public override StatusResult ApplyEffect(CharacterStateMachine caster, CharacterStateMachine target, bool crit, [CanBeNull] ISkill skill = null)
        {
            PoisonToApply effectStruct = new(caster, target, crit, skill, ScriptOrigin: this, BaseDuration, Permanent, BaseApplyChance, BasePoisonPerTime);
            return ProcessModifiersAndTryApply(effectStruct);
        }

        public static void ProcessModifiers(PoisonToApply effectStruct)
        {
            IStatusApplierModule applierModule = effectStruct.Caster.StatusApplierModule;
            if (effectStruct.Caster == effectStruct.Target)
            {
                effectStruct.ApplyChance = 100;
            }
            else
            {
                effectStruct.ApplyChance += (-1 * effectStruct.Target.ResistancesModule.GetPoisonResistance()) + applierModule.BasePoisonApplyChance;
                if (effectStruct.FromCrit)
                    effectStruct.ApplyChance += BonusApplyChanceOnCrit;
            }
            
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

        public static StatusResult ProcessModifiersAndTryApply(PoisonToApply effectStruct)
        {
            ProcessModifiers(effectStruct);
            return TryApply(ref effectStruct);
        }

        private static StatusResult TryApply([NotNull] ref PoisonToApply poisonStruct)
        {
            FullCharacterState targetState = poisonStruct.Target.StateEvaluator.FullPureEvaluate();
            if (targetState.Defeated || targetState.Corpse || targetState.Grappled)
                return StatusResult.Failure(poisonStruct.Caster, poisonStruct.Target, generatesInstance: false);
            
            bool success = Save.Random.Next(100) < poisonStruct.ApplyChance;

            Option<StatusInstance> option = success ? Poison.CreateInstance(poisonStruct.Duration, poisonStruct.Permanent, 
                poisonStruct.Target, poisonStruct.Caster, poisonStruct.PoisonPerSecond) : Option.None;

            if (StatusVFXManager.AssertInstance(out StatusVFXManager statusEffectVFXManager))
                statusEffectVFXManager.Enqueue(StatusCueHandler.FromAppliedStatus(poisonStruct.Target, StatusCueHandler.StandardValidator, EffectType.Poison, option.IsSome));

            return new StatusResult(poisonStruct.Caster, poisonStruct.Target, success: option.IsSome, statusInstance: option.SomeOrDefault(), generatesInstance: true, EffectType.Poison);
        }

        public override float ComputePoints(ref SkillStruct skillStruct, [NotNull] CharacterStateMachine target)
        {
            if (target.StaminaModule.IsNone || target.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Corpse or CharacterState.Downed or CharacterState.Grappled)
                return 0f;
            
            PoisonToApply effectStruct = new(skillStruct.Caster, target, FromCrit: false, skillStruct.Skill, ScriptOrigin: this, BaseDuration, Permanent, BaseApplyChance, BasePoisonPerTime);
            ProcessModifiers(effectStruct);

            float applyChance = effectStruct.ApplyChance / 100f;
            float totalDamage = effectStruct.PoisonPerSecond * effectStruct.Duration.FloatSeconds;
            float durationMultiplier = Permanent ? HeuristicConstants.PermanentMultiplier : Mathf.Pow(HeuristicConstants.PenaltyForOvertime, effectStruct.Duration.FloatSeconds + 1);
            
            float points = applyChance * totalDamage * durationMultiplier * HeuristicConstants.DamageMultiplier;
            return points * -1f;
        }
        
        public override EffectType EffectType => EffectType.Poison;
        [NotNull]
        public override string Description => StatusScriptDescriptions.Get(this);
        
        public override bool IsPositive => false;
        
        public override bool PlaysBarkAppliedOnCaster => false;
        public override bool PlaysBarkAppliedOnEnemy => true;
        public override bool PlaysBarkAppliedOnAlly => false;
    }
}