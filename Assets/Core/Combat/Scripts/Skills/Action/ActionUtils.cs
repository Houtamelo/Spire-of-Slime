using System.Collections.Generic;
using Collections.Pooled;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Behaviour.Modules;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Managers;
using Core.Utils.Math;
using DG.Tweening;
using JetBrains.Annotations;
using UnityEngine;

namespace Core.Combat.Scripts.Skills.Action
{
    public static class ActionUtils
    {
        public static void FadeDownAllBars([NotNull] CombatManager combatManager, float duration)
        {
            foreach (CharacterStateMachine character in combatManager.Characters.GetAllFixed())
            {
                if (character.Display.AssertSome(out DisplayModule characterDisplay))
                    characterDisplay.FadeBars(endValue: 0f, duration);
            }
        }

        public static void AnimateCameraAndSplashScreen([NotNull] CombatManager combatManager, [NotNull] PlannedSkill plan, CharacterStateMachine caster, 
                                                        [CanBeNull] IReadOnlyCollection<CharacterStateMachine> targets, float lerpDuration, float animationDuration)
        {
            combatManager.ActionAnimator.FadeUpActionSplashScreenAndSpeedLines(plan, lerpDuration, animationDuration);
            
            List<CharacterStateMachine> characters = new() { caster };
            if (targets != null)
                characters.AddRange(targets);
            
            combatManager.ActionAnimator.AnimateCameraForAction(characters, lerpDuration, animationDuration);
        }

        public static void FadeDownUIAndBackground([NotNull] CombatManager combatManager, float duration) => combatManager.ActionAnimator.FadeDownUIAndBackground(duration);

        public static void LerpCharactersToAnimationPositions([NotNull] CharacterStateMachine caster, Dictionary<CharacterStateMachine, Vector3> endPositions, [NotNull] IReadOnlyCollection<CharacterStateMachine> targets,
                                                              Vector3 endScale, float duration)
        {
            if (caster.Display.AssertSome(out DisplayModule casterDisplay))
            {
                casterDisplay.SetSortingOrder(50);
                casterDisplay.AllowIndicatorsExternally(false);
                if (endPositions.TryGetValue(caster, out Vector3 casterPosition) == false)
                {
                    casterPosition = casterDisplay.transform.position;
                    Debug.LogWarning($"Caster {caster.Script.CharacterName} has no end position");
                }
                
                casterDisplay.transform.DOMove(casterPosition, duration);
                casterDisplay.transform.DOScale(endScale, duration);
                casterDisplay.AllowIdleAnimationTimeUpdateExternally(false);
                casterDisplay.AllowShadowsExternally(false);
            }

            foreach (CharacterStateMachine target in targets)
            {
                if (target == caster)
                    continue;
                
                if (target.Display.AssertSome(out DisplayModule targetDisplay) == false)
                    continue;

                targetDisplay.SetSortingOrder(value: 30 - Mathf.CeilToInt(target.PositionHandler.GetAveragePosition()));
                targetDisplay.AllowIndicatorsExternally(false);
                if (endPositions.TryGetValue(target, out Vector3 targetPosition) == false)
                {
                    targetPosition = targetDisplay.transform.position;
                    Debug.LogWarning($"Target {target.Script.CharacterName} has no end position");
                }
                
                targetDisplay.transform.DOMove(targetPosition, duration);
                targetDisplay.transform.DOScale(endScale, duration);
                targetDisplay.AllowIdleAnimationTimeUpdateExternally(false);
                targetDisplay.AllowShadowsExternally(false);
            }
        }
        
        public static void FadeDownOutsideCharacters([NotNull] IReadOnlyCollection<CharacterStateMachine> outsiders, float duration)
        {
            foreach (CharacterStateMachine character in outsiders)
            {
                if (character.Display.AssertSome(out DisplayModule display))
                {
                    display.FadeRenderer(endValue: 0f, duration);
                    display.AllowIndicatorsExternally(false);
                }
            }
        }
        
        public static void FadeDownCasterAndTargets([NotNull] CharacterStateMachine caster, [NotNull] PooledSet<CharacterStateMachine> targets, float duration)
        {
            if (caster.Display.AssertSome(out DisplayModule casterDisplay))
                casterDisplay.FadeRenderer(endValue: 0f, duration);

            foreach (CharacterStateMachine target in targets)
            {
                if (target.Display.AssertSome(out DisplayModule display))
                    display.FadeRenderer(endValue: 0f, duration);
            }
        }
        
        public static void FadeUpCasterAndTargets([NotNull] CharacterStateMachine caster, [NotNull] PooledSet<CharacterStateMachine> targets, float duration)
        {
            if (caster.Display.TrySome(out DisplayModule casterDisplay) && casterDisplay.AnimationStatus is not AnimationStatus.Defeated and not AnimationStatus.Grappled)
                casterDisplay.FadeRenderer(endValue: 1f, duration);

            foreach (CharacterStateMachine target in targets)
            {
                if (target.Display.TrySome(out DisplayModule display) && display.AnimationStatus is not AnimationStatus.Defeated and not AnimationStatus.Grappled)
                    display.FadeRenderer(endValue: 1f, duration);
            }
        }
        
        public static void LerpCharactersToOriginalPositions(Dictionary<CharacterStateMachine, Vector3> startPositions, [NotNull] CharacterStateMachine caster, [NotNull] IReadOnlyCollection<CharacterStateMachine> targets, float duration)
        {
            foreach (CharacterStateMachine target in targets)
            {
                if (target == caster || target.StateEvaluator.PureEvaluate() is CharacterState.Defeated ||
                    target.Display.TrySome(out DisplayModule targetDisplay) == false ||
                    startPositions.TryGetValue(target, out Vector3 targetPosition) == false)
                    continue;
                
                targetDisplay.transform.DOMove(endValue: targetPosition, duration);
                targetDisplay.transform.DOScale(endValue: Vector3.one, duration);
            }
            
            if (caster.StateEvaluator.PureEvaluate() is not CharacterState.Defeated && caster.Display.TrySome(out DisplayModule casterDisplay) && startPositions.TryGetValue(caster, out Vector3 casterPosition))
            {
                casterDisplay.transform.DOMove(endValue: casterPosition, duration);
                casterDisplay.transform.DOScale(endValue: Vector3.one, duration);
            }
        }
        
        public static void FadeUpOutsideCharacters([NotNull] IReadOnlyCollection<CharacterStateMachine> outsiders, float duration)
        {
            foreach (CharacterStateMachine character in outsiders)
            {
                if (character.Display.AssertSome(out DisplayModule display) && display.AnimationStatus is not AnimationStatus.Defeated and not AnimationStatus.Grappled)
                    display.FadeRenderer(endValue: 1f, duration);
            }
        }

        public static void FadeUpActionSplashScreenAndSpeedLines([NotNull] CombatManager combatManager, float duration)
        {
            combatManager.ActionAnimator.FadeOutActionSplashScreenAndSpeedLines(duration);
        }

        public static void FadeUpUIAndBackground([NotNull] CombatManager combatManager, float duration)
        {
            combatManager.ActionAnimator.FadeUpUIAndBackground(duration);
        }
        
        public static void MoveAllToDefaultAnimationAndFadeUpBars([NotNull] CombatManager combatManager, [NotNull] IReadOnlyCollection<CharacterStateMachine> targets, [NotNull] CharacterStateMachine caster, float barsFadeDuration)
        {
            if (caster.Display.TrySome(out DisplayModule casterDisplay))
            {
                casterDisplay.AllowIdleAnimationTimeUpdateExternally(true);
                casterDisplay.AllowShadowsExternally(true);
                casterDisplay.SetSortingOrder(Mathf.CeilToInt(caster.PositionHandler.GetAveragePosition()));

                if (casterDisplay.AnimationStatus is AnimationStatus.Common)
                {
                    CharacterState state = caster.StateEvaluator.PureEvaluate();
                    casterDisplay.MatchAnimationWithState(state);
                }
            }
            
            foreach (CharacterStateMachine target in targets)
            {
                if (caster == target || target.Display.TrySome(out DisplayModule targetDisplay) == false)
                    continue;
                
                targetDisplay.AllowIdleAnimationTimeUpdateExternally(true);
                targetDisplay.AllowShadowsExternally(true);
                targetDisplay.SetSortingOrder(Mathf.CeilToInt(target.PositionHandler.GetAveragePosition()));

                if (targetDisplay.AnimationStatus is AnimationStatus.Common)
                {
                    CharacterState state = target.StateEvaluator.PureEvaluate();
                    targetDisplay.MatchAnimationWithState(state);
                }
            }

            foreach (CharacterStateMachine character in combatManager.Characters.GetAllFixed())
            {
                character.AfterSkillDisplayUpdate();
                if (character.Display.TrySome(out DisplayModule characterDisplay))
                {
                    characterDisplay.FadeBars(endValue: 1f, barsFadeDuration);
                    characterDisplay.AllowIndicatorsExternally(true);
                    characterDisplay.SetBaseSpeed(1f);
                }
            }
            
            combatManager.PositionManager.MoveAllToDefaultPosition(baseDuration: PositionManager.CharacterMoveDuration);
        }
        
        public static void SetRecoveryAndNotifyFinished(ListPool.ListPool<ActionResult> results, TSpan recovery, [NotNull] CharacterStateMachine caster, [NotNull] PlannedSkill plan)
        {
            if (plan.CostFree == false && caster.StateEvaluator.PureEvaluate() is not CharacterState.Defeated 
                                                                                  and not CharacterState.Corpse
                                                                                  and not CharacterState.Downed
                                                                                  and not CharacterState.Grappled)
            {
                caster.RecoveryModule.SetInitial(recovery);
                if (caster.Display.TrySome(out DisplayModule display))
                    caster.RecoveryModule.ForceUpdateDisplay(display);
            }

            plan.NotifyDone();
            caster.Events.OnActionCompleted(results);
        }

        public static void IncrementSkillCounter([NotNull] PlannedSkill plan, [NotNull] CharacterStateMachine caster)
        {
            ISkillModule casterSkillModule = caster.SkillModule;
            casterSkillModule.SkillUseCounters.TryGetValue(plan.Skill, out int current);
            casterSkillModule.SkillUseCounters[plan.Skill] = current + 1;
        }

        public static void FillDefaultEndPositions([NotNull] CombatManager combatManager, [NotNull] IReadOnlyCollection<CharacterStateMachine> targets, [NotNull] PlannedSkill plan,
                                                   [NotNull] CharacterStateMachine caster, Dictionary<CharacterStateMachine, Vector3> endPositionsToFill)
        {
            HashSet<CharacterStateMachine> charactersToCalculate = new(targets) { caster };
            combatManager.PositionManager.FillDefaultAnimationPositions(endPositionsToFill, caster: caster, charactersToCalculate, skill: plan.Skill);
        }
        
        public static void FillEndPositionsForOverlay(CombatManager combatManager, [NotNull] IReadOnlyCollection<CharacterStateMachine> targets, [NotNull] CharacterStateMachine caster, [NotNull] Dictionary<CharacterStateMachine, Vector3> endPositionsToFill)
        {
            HashSet<CharacterStateMachine> charactersToCalculate = new(targets) { caster };
            PositionManager.FillTemptAnimationPositions(endPositionsToFill, caster: caster, charactersToCalculate);
        }

        public static void AnimateIndicators([NotNull] CharacterStateMachine caster, [NotNull] IReadOnlyCollection<CharacterStateMachine> targets, Dictionary<CharacterStateMachine, Vector3> startPositionsToFill)
        {
            if (caster.Display.AssertSome(out DisplayModule casterDisplay))
            {
                startPositionsToFill[caster] = casterDisplay.transform.position;
                casterDisplay.AnimateIndicatorsForAction();
            }

            foreach (CharacterStateMachine target in targets)
            {
                if (target.Display.AssertSome(out DisplayModule targetDisplay) == false)
                    continue;

                startPositionsToFill[target] = targetDisplay.transform.position;
                targetDisplay.AnimateIndicatorsForAction();
            }
        }

        public static bool TryFillTargetList([NotNull] PlannedSkill plan, [NotNull] CharacterStateMachine caster, PooledSet<CharacterStateMachine> outsiders, [NotNull] PooledSet<CharacterStateMachine> targets)
        {
            caster.ChargeModule.Reset();
            plan.FillTargetList(targets, outsiders);
            if (targets.Count == 0)
            {
                caster.RecoveryModule.Reset();
                plan.NotifyDone();
                return false;
            }

            return true;
        }

        public static bool Validate(CombatManager combatManager, PlannedSkill plan, CharacterStateMachine caster)
        {
            if (combatManager == null)
            {
                plan.NotifyDone();
                return false;
            }

            if (caster.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Corpse or CharacterState.Grappled)
            {
                plan.NotifyDone();
                return false;
            }

            return true;
        }
    }
}