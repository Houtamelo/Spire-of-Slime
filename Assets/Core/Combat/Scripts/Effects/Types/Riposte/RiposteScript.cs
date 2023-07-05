using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Cues;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Utils.Patterns;
using Utils.Patterns;

namespace Core.Combat.Scripts.Effects.Types.Riposte
{
    public record RiposteScript(bool Permanent, float BaseDuration, float BasePower)
        : StatusScriptDurationBased(Permanent, BaseDuration)
    {
        public float BasePower { get; protected set; } = BasePower;
        
        public override bool IsPositive => true;

        public override bool PlaysBarkAppliedOnCaster => true;
        public override bool PlaysBarkAppliedOnEnemy => false;
        public override bool PlaysBarkAppliedOnAlly => false;

        public override StatusToApply GetStatusToApply(CharacterStateMachine caster, CharacterStateMachine target, bool crit, ISkill skill = null) 
            => new RiposteToApply(caster, target, crit, skill, this, BaseDuration, Permanent, BasePower);

        public override StatusResult ApplyEffect(CharacterStateMachine caster, CharacterStateMachine target, bool crit, ISkill skill = null)
        {
            RiposteToApply riposteStruct = new(caster, target, crit, skill, this, BaseDuration, Permanent, BasePower);
            return ProcessModifiersAndTryApply(riposteStruct);
        }

        public static void ProcessModifiers(RiposteToApply effectStruct)
        {
            IStatusApplierModule applierModule = effectStruct.Caster.StatusApplierModule;
            applierModule.ModifyEffectApplying(ref effectStruct);
            
            IStatusModule receiverModule = effectStruct.Target.StatusModule;
            receiverModule.ModifyEffectReceiving(ref effectStruct);
            
            if (effectStruct.FromCrit)
                effectStruct.Duration *= DurationMultiplierOnCrit;
        }

        public static StatusResult ProcessModifiersAndTryApply(RiposteToApply effectStruct)
        {
            ProcessModifiers(effectStruct);
            return TryApply(ref effectStruct);
        }

        private static StatusResult TryApply(ref RiposteToApply riposteStruct)
        {
            FullCharacterState targetState = riposteStruct.Target.StateEvaluator.FullPureEvaluate();
            if (targetState.Defeated || targetState.Corpse || targetState.Grappled)
                return StatusResult.Failure(riposteStruct.Caster, riposteStruct.Target, generatesInstance: false);
            
            Option<StatusInstance> option = Riposte.CreateInstance(riposteStruct.Duration, riposteStruct.IsPermanent, riposteStruct.Target,
                riposteStruct.Caster, riposteStruct.Power);
            
            if (StatusVFXManager.AssertInstance(out StatusVFXManager statusEffectVFXManager))
                statusEffectVFXManager.Enqueue(StatusCueHandler.FromAppliedStatus(riposteStruct.Target, StatusCueHandler.StandardValidator, EffectType.Riposte, success: true));

            return new StatusResult(riposteStruct.Caster, riposteStruct.Target, success: option.IsSome, statusInstance: option.SomeOrDefault(), generatesInstance: option.IsSome, EffectType.Riposte);
        }

        public override float ComputePoints(ref SkillStruct skillStruct, CharacterStateMachine target)
        {
            if (target.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Corpse or CharacterState.Downed or CharacterState.Grappled or CharacterState.Stunned)
                return 0f;
            
            RiposteToApply effectStruct = new(skillStruct.Caster, target, false, skillStruct.Skill, this, BaseDuration, Permanent, BasePower);
            ProcessModifiers(effectStruct);

            float penaltyForAlreadyHavingRiposte = 1f;
            foreach (StatusInstance status in target.StatusModule.GetAll)
            {
                if (status.EffectType is not EffectType.Riposte || status.IsDeactivated)
                    continue;
                
                penaltyForAlreadyHavingRiposte = HeuristicConstants.AlreadyHasRiposteMultiplier / (1 + status.Duration);
                break;
            }
            
            
            float durationMultiplier = effectStruct.IsPermanent ? HeuristicConstants.PermanentMultiplier : effectStruct.Duration * HeuristicConstants.DurationMultiplier;
            float points = durationMultiplier * effectStruct.Power * HeuristicConstants.DurationMultiplier * HeuristicConstants.RiposteMultiplier * penaltyForAlreadyHavingRiposte;
            return points;
        }

        public override EffectType EffectType => EffectType.Riposte;
        public override string Description => StatusScriptDescriptions.Get(this);
    }
}