using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Behaviour.Modules;
using Core.Combat.Scripts.Cues;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Save_Management.SaveObjects;
using JetBrains.Annotations;
using Random = UnityEngine.Random;

namespace Core.Combat.Scripts.Effects.Types.Move
{
    public record MoveScript(int BaseApplyChance, int MoveDelta) : StatusScript
    {
        public int BaseApplyChance { get; set; } = BaseApplyChance;
        public int MoveDelta { get; protected set; } = MoveDelta;

        [NotNull]
        public override StatusToApply GetStatusToApply(CharacterStateMachine caster, CharacterStateMachine target, bool crit, [CanBeNull] ISkill skill = null) 
            => new MoveToApply(caster, target, crit, skill, ScriptOrigin: this, BaseApplyChance, MoveDelta);

        public override StatusResult ApplyEffect(CharacterStateMachine caster, CharacterStateMachine target, bool crit, [CanBeNull] ISkill skill = null)
        {
            MoveToApply effectStruct = new(caster, target, crit, skill, ScriptOrigin: this, BaseApplyChance, MoveDelta);
            return ProcessModifiersAndTryApply(effectStruct);
        }

        public static void ProcessModifiers(MoveToApply effectStruct)
        {
            IStatusApplierModule applierModule = effectStruct.Caster.StatusApplierModule;
            if (effectStruct.Caster.PositionHandler.IsLeftSide == effectStruct.Target.PositionHandler.IsLeftSide)
            {
                effectStruct.ApplyChance = 100;
            }
            else
            {
                effectStruct.ApplyChance += (-1 * effectStruct.Target.ResistancesModule.GetMoveResistance()) + applierModule.BaseMoveApplyChance;
                if (effectStruct.FromCrit)
                    effectStruct.ApplyChance += BonusApplyChanceOnCrit;
            }
            
            applierModule.ModifyEffectApplying(ref effectStruct);
            
            IStatusReceiverModule receiverModule = effectStruct.Target.StatusReceiverModule;
            receiverModule.ModifyEffectReceiving(ref effectStruct);
        }

        public static StatusResult ProcessModifiersAndTryApply(MoveToApply effectStruct)
        {
            ProcessModifiers(effectStruct);
            return TryApply(ref effectStruct);
        }

        private static StatusResult TryApply([NotNull] ref MoveToApply moveStruct)
        {
            if (Save.Random.Next(100) >= moveStruct.ApplyChance)
            {
                if (StatusVFXManager.AssertInstance(out StatusVFXManager statusVFXManager))
                    statusVFXManager.Enqueue(StatusCueHandler.FromAppliedStatus(moveStruct.Target, StatusCueHandler.StandardValidator, EffectType.Move, success: false));
                
                return StatusResult.Failure(moveStruct.Caster, moveStruct.Target, generatesInstance: false);
            }

            bool success = false;
            if (moveStruct.Caster.Display.AssertSome(out DisplayModule casterDisplay))
                success = casterDisplay.CombatManager.PositionManager.ShiftPosition(moveStruct.Target, moveStruct.MoveDelta);

            return new StatusResult(moveStruct.Caster, moveStruct.Target, success, statusInstance: null, generatesInstance: false, EffectType.Move);
        }

        public override float ComputePoints(ref SkillStruct skillStruct, [NotNull] CharacterStateMachine target)
        {
            if (target.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Grappled)
                return 0f;
            
            if (target.Display.TrySome(out DisplayModule targetDisplay) == false)
                return 0f;

            MoveToApply effectStruct = new(skillStruct.Caster, target, FromCrit: false, skillStruct.Skill, ScriptOrigin: this, BaseApplyChance, MoveDelta);
            ProcessModifiers(effectStruct);

            float applyChance = effectStruct.ApplyChance / 100f;
            CharacterPositioning targetPositioning = targetDisplay.CombatManager.PositionManager.ComputePositioning(target);
            {
                CharacterPositioning predictedPositioning = targetDisplay.CombatManager.PositionManager.PredictPositionsOnMove(target, (byte)effectStruct.MoveDelta, out bool anyMovement);
                if (anyMovement == false)
                    return 0f;

                int validSkillsOnCurrentPosition = 0;
                foreach (ISkill skill in target.Script.Skills)
                {
                    if (skill.FullCastingOk(target, targetPositioning))
                        validSkillsOnCurrentPosition++;
                }

                int validSkillsOnPredictedPosition = 0;
                foreach (ISkill skill in target.Script.Skills)
                {
                    if (skill.FullCastingOk(target, predictedPositioning))
                        validSkillsOnPredictedPosition++;
                }

                float relativeSkillDifference = (validSkillsOnCurrentPosition - validSkillsOnPredictedPosition) / (float)target.Script.Skills.Length;
                float multiplier = skillStruct.Caster.PositionHandler.IsLeftSide == target.PositionHandler.IsLeftSide ? 1f : -1f;
                return applyChance * relativeSkillDifference * HeuristicConstants.MoveMultiplier * multiplier;
            }
        }
        
        [NotNull]
        public override string Description => StatusScriptDescriptions.Get(this);
        public override EffectType EffectType => EffectType.Move;
        
        public override bool IsPositive => false;
        
        public override bool PlaysBarkAppliedOnCaster => false;
        public override bool PlaysBarkAppliedOnEnemy => false;
        public override bool PlaysBarkAppliedOnAlly => false;
    }
}