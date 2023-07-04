using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Cues;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using Random = UnityEngine.Random;

namespace Core.Combat.Scripts.Effects.Types.Move
{
    public record MoveScript(float BaseApplyChance, int MoveDelta) : StatusScript
    {
        public float BaseApplyChance { get; set; } = BaseApplyChance;
        public int MoveDelta { get; protected set; } = MoveDelta;

        public override bool IsPositive => false;
        
        public override bool PlaysBarkAppliedOnCaster => false;
        public override bool PlaysBarkAppliedOnEnemy => false;
        public override bool PlaysBarkAppliedOnAlly => false;

        public override StatusToApply GetStatusToApply(CharacterStateMachine caster, CharacterStateMachine target, bool crit, ISkill skill = null) 
            => new MoveToApply(caster, target, crit, skill, this, BaseApplyChance, MoveDelta);

        public override StatusResult ApplyEffect(CharacterStateMachine caster, CharacterStateMachine target, bool crit, ISkill skill = null)
        {
            MoveToApply effectStruct = new(caster, target, crit, skill, this, BaseApplyChance, MoveDelta);
            return ProcessModifiersAndTryApply(effectStruct);
        }

        public static void ProcessModifiers(MoveToApply effectStruct)
        {
            IStatusApplierModule applierModule = effectStruct.Caster.StatusApplierModule;
            if (effectStruct.Caster.PositionHandler.IsLeftSide == effectStruct.Target.PositionHandler.IsLeftSide)
            {
                effectStruct.ApplyChance = 1;
            }
            else
            {
                effectStruct.ApplyChance += -1f * effectStruct.Target.ResistancesModule.GetMoveResistance() + applierModule.BaseMoveApplyChance;
                if (effectStruct.FromCrit)
                    effectStruct.ApplyChance += BonusApplyChanceOnCrit;
            }
            applierModule.ModifyEffectApplying(ref effectStruct);
            
            IStatusModule receiverModule = effectStruct.Target.StatusModule;
            receiverModule.ModifyEffectReceiving(ref effectStruct);
        }

        public static StatusResult ProcessModifiersAndTryApply(MoveToApply effectStruct)
        {
            ProcessModifiers(effectStruct);
            return TryApply(ref effectStruct);
        }

        private static StatusResult TryApply(ref MoveToApply moveStruct)
        {
            if (Random.value >= moveStruct.ApplyChance)
            {
                if (StatusVFXManager.AssertInstance(out StatusVFXManager statusVFXManager))
                    statusVFXManager.Enqueue(StatusCueHandler.FromAppliedStatus(moveStruct.Target, StatusCueHandler.StandardValidator, EffectType.Move, success: false));
                
                return StatusResult.Failure(moveStruct.Caster, moveStruct.Target, generatesInstance: false);
            }

            bool success = false;
            if (moveStruct.Caster.Display.AssertSome(out CharacterDisplay casterDisplay))
            {
                success = casterDisplay.CombatManager.PositionManager.ShiftPosition(target: moveStruct.Target, delta: moveStruct.MoveDelta);
            }

            return new StatusResult(moveStruct.Caster, moveStruct.Target, success, statusInstance: null, generatesInstance: false, EffectType.Move);
        }

        public override float ComputePoints(ref SkillStruct skillStruct, CharacterStateMachine target)
        {
            if (target.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Grappled)
                return 0f;
            
            if (target.Display.TrySome(out CharacterDisplay targetDisplay) == false)
                return 0f;

            MoveToApply effectStruct = new(skillStruct.Caster, target, false, skillStruct.Skill, this, BaseApplyChance, MoveDelta);
            ProcessModifiers(effectStruct);


            float applyChance = effectStruct.ApplyChance;
            CharacterPositioning targetPositioning = targetDisplay.CombatManager.PositionManager.ComputePositioning(target);
            {
                CharacterPositioning predictedPositioning = targetDisplay.CombatManager.PositionManager.PredictPositionsOnMove(target, (byte)effectStruct.MoveDelta, out bool anyMovement);
                if (!anyMovement)
                    return 0f;

                int validSkillsOnCurrentPosition = 0;
                foreach (ISkill skill in target.Script.Skills)
                    if (skill.FullCastingOk(target, targetPositioning))
                        validSkillsOnCurrentPosition++;
                
                int validSkillsOnPredictedPosition = 0;
                foreach (ISkill skill in target.Script.Skills)
                    if (skill.FullCastingOk(target, predictedPositioning))
                        validSkillsOnPredictedPosition++;

                float relativeSkillDifference = (float) (validSkillsOnCurrentPosition - validSkillsOnPredictedPosition) / target.Script.Skills.Count;
                float multiplier = skillStruct.Caster.PositionHandler.IsLeftSide == target.PositionHandler.IsLeftSide ? 1f : -1f;
                return applyChance * relativeSkillDifference * HeuristicConstants.MoveMultiplier * multiplier;
            }
        }

        public override string Description => StatusScriptDescriptions.Get(this);
        public override EffectType EffectType => EffectType.Move;
    }
}