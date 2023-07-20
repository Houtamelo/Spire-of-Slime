using System;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Skills.Interfaces;
using JetBrains.Annotations;

namespace Core.Combat.Scripts.Skills
{
    public static class TargetingUtils
    {
    #region CasterCheck
        public static bool CastingPositionOk(this ISkill skill, [NotNull] CharacterStateMachine caster) => caster.PositionHandler.CanPositionCast(skill);
        public static bool CastingPositionOk(this ISkill skill, [NotNull] CharacterStateMachine caster, CharacterPositioning positionses) => caster.PositionHandler.CanPositionCast(skill, positionses);

        public static bool BellowUseLimit(this ISkill skill, [NotNull] CharacterStateMachine caster) => caster.SkillModule.HasChargesIfLimited(skill);

        [System.Diagnostics.Contracts.Pure]
        public static bool FullCastingOk(this ISkill skill, [NotNull] CharacterStateMachine caster)
        {
            if (skill.CastingPositionOk(caster) == false)
                return false;
            
            if (skill.BellowUseLimit(caster) == false)
                return false;
            
            return true;
        }
        
        /// <summary> Only checks caster position and use limit, you still need to check if the target is valid </summary>
        public static bool FullCastingOk(this ISkill skill, [NotNull] CharacterStateMachine caster, CharacterPositioning positionses)
        {
            if (skill.CastingPositionOk(caster, positionses) == false)
                return false;
            
            if (skill.BellowUseLimit(caster) == false)
                return false;
            
            return true;
        }
    #endregion

    #region TargetCheck
        public static bool TargetPositionOk(this ISkill skill, CharacterStateMachine caster, [NotNull] CharacterStateMachine target) => target.PositionHandler.CanPositionBeTargetedBy(skill, caster);

        public static bool TargetingTypeOk(TargetType targetType, CharacterStateMachine caster, CharacterStateMachine target)
        {
            return targetType switch
            {
                TargetType.OnlySelf => caster == target,
                TargetType.NotSelf  => caster != target,
                TargetType.CanSelf  => true,
                _                   => throw new ArgumentOutOfRangeException(nameof(targetType), targetType, null)
            };
        }
        
        public static bool TargetingTypeOk([NotNull] this ISkill skill, CharacterStateMachine caster, CharacterStateMachine target) => TargetingTypeOk(skill.TargetType, caster, target);

        public static bool TargetStateOk([NotNull] this CharacterStateMachine target) => target.StateEvaluator.CanBeTargeted();

        [System.Diagnostics.Contracts.Pure]
        public static bool FullCastingAndTargetingOk(this ISkill skill, [NotNull] CharacterStateMachine caster, CharacterStateMachine target)
        {
            if (skill.FullCastingOk(caster) == false)
                return false;
            
            if (skill.TargetingTypeOk(caster, target) == false)
                return false;
            
            if (skill.TargetPositionOk(caster, target) == false)
                return false;
            
            if (target.TargetStateOk() == false)
                return false;
            
            return true;
        }
    #endregion

        /// <returns> Will this skill hit a character by being a multi target? (for area of Effect skills) </returns>
        public static bool HitsCollateral([NotNull] this ISkill skill, CharacterStateMachine caster, CharacterStateMachine possibleTarget)
        {
            if (skill.MultiTarget == false)
                return false;

            bool isAlly = caster.PositionHandler.IsLeftSide == possibleTarget.PositionHandler.IsLeftSide;
            if (skill.IsPositive != isAlly)
                return false;

            if (possibleTarget.Display.AssertSome(out DisplayModule display) == false)
                return false;

            CharacterPositioning positions = display.CombatManager.PositionManager.ComputePositioning(possibleTarget);
            foreach (int position in positions)
            {
                if (skill.TargetPositions[position] == true)
                    return true;
            }

            return false;
        }
    }
}