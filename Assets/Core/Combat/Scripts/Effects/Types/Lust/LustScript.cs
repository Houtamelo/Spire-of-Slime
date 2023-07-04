using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Core.Combat.Scripts.Effects.Types.Lust
{
    public record LustScript(int LustLower = 1, int LustUpper = 2) : StatusScript
    {
        public int LustLower { get; set; } = LustLower;
        public int LustUpper { get; set; } = LustUpper;

        public override bool IsPositive => false;
        
        public override bool PlaysBarkAppliedOnCaster => false;
        public override bool PlaysBarkAppliedOnEnemy => true;
        public override bool PlaysBarkAppliedOnAlly => false;

        public override StatusToApply GetStatusToApply(CharacterStateMachine caster, CharacterStateMachine target, bool crit, ISkill skill = null)
            => new LustToApply(caster, target, crit, skill, this, LustLower, LustUpper);

        public static void ProcessModifiers(LustToApply effectStruct)
        {
            IStatusApplierModule applierModule = effectStruct.Caster.StatusApplierModule;
            applierModule.ModifyEffectApplying(ref effectStruct);
            
            IStatusReceiverModule receiverModule = effectStruct.Target.StatusModule;
            receiverModule.ModifyEffectReceiving(ref effectStruct);
            
            if (effectStruct.FromCrit)
                effectStruct.Multiplier *= StatusScriptDurationBased.DurationMultiplierOnCrit;
        }

        public static StatusResult ProcessModifiersAndTryApply(LustToApply effectStruct)
        {
            ProcessModifiers(effectStruct);
            return TryApply(ref effectStruct);
        }

        public override StatusResult ApplyEffect(CharacterStateMachine caster, CharacterStateMachine target, bool crit, ISkill skill = null)
        {
            LustToApply effectStruct = new(caster, target, crit, skill,this, LustLower, LustUpper);
            return ProcessModifiersAndTryApply(effectStruct);
        }

        private static StatusResult TryApply(ref LustToApply effectStruct)
        {
            FullCharacterState targetState = effectStruct.Target.StateEvaluator.FullPureEvaluate();
            if (targetState.Defeated || targetState.Corpse || targetState.Grappled)
                return StatusResult.Failure(effectStruct.Caster, effectStruct.Target, generatesInstance: false);
            
            if (effectStruct.Target.LustModule.IsNone)
                return StatusResult.Failure(effectStruct.Caster, effectStruct.Target, generatesInstance: false);

            int lust = Mathf.CeilToInt(Random.Range(effectStruct.LustLower, effectStruct.LustUpper) * effectStruct.Multiplier);
            effectStruct.Target.LustModule.Value.ChangeLustViaAction(lust); // visual cue handled by lust module
            return StatusResult.Success(effectStruct.Caster, effectStruct.Target, statusInstance: null, generatesInstance: false, EffectType.Lust);
        }

        public override float ComputePoints(ref SkillStruct skillStruct, CharacterStateMachine target)
        {
            if (target.LustModule.IsNone || target.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Corpse)
                return 0f;
            
            LustToApply effectStruct = new(skillStruct.Caster, target, false, skillStruct.Skill,this, LustLower, LustUpper);
            ProcessModifiers(effectStruct);
            
            float averageLust = (effectStruct.LustLower + effectStruct.LustUpper) / 2f;
            float points = averageLust * HeuristicConstants.LustMultiplier;
            return points * -1f;
        }

        public override string Description => StatusScriptDescriptions.Get(this);
        public override EffectType EffectType => EffectType.Lust;
    }
}