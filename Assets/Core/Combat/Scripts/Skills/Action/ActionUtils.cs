using System.Collections.Generic;
using Collections.Pooled;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Combat.Scripts.Managers;
using DG.Tweening;
using UnityEngine;

namespace Core.Combat.Scripts.Skills.Action
{
    public static class ActionUtils
    {
        public static void FadeDownAllBars(CombatManager combatManager, float duration)
        {
            foreach (CharacterStateMachine character in combatManager.Characters.GetAllFixed())
                if (character.Display.AssertSome(out CharacterDisplay characterDisplay))
                    characterDisplay.FadeBars(endValue: 0f, duration);
        }

        public static void AnimateCameraAndSplashScreen(CombatManager combatManager, PlannedSkill plan, CharacterStateMachine caster, 
                                                        IReadOnlyCollection<CharacterStateMachine> targets, float lerpDuration, float animationDuration)
        {
            combatManager.ActionAnimator.FadeUpActionSplashScreenAndSpeedLines(plan, lerpDuration, animationDuration);
            
            List<CharacterStateMachine> characters = new() { caster };
            if (targets != null)
                characters.AddRange(targets);
            
            combatManager.ActionAnimator.AnimateCameraForAction(characters, lerpDuration, animationDuration);
        }

        public static void FadeDownUIAndBackground(CombatManager combatManager, float duration) => combatManager.ActionAnimator.FadeDownUIAndBackground(duration);

        public static void LerpCharactersToAnimationPositions(CharacterStateMachine caster, Dictionary<CharacterStateMachine, Vector3> endPositions, IReadOnlyCollection<CharacterStateMachine> targets,
                                                              Vector3 endScale, float duration)
        {
            if (caster.Display.AssertSome(out CharacterDisplay casterDisplay))
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
                
                if (target.Display.AssertSome(out CharacterDisplay targetDisplay) == false)
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
        
        public static void FadeDownOutsideCharacters(IReadOnlyCollection<CharacterStateMachine> outsiders, float duration)
        {
            foreach (CharacterStateMachine character in outsiders)
            {
                if (character.Display.AssertSome(out CharacterDisplay display))
                {
                    display.FadeRenderer(endValue: 0f, duration);
                    display.AllowIndicatorsExternally(false);
                }
            }
        }
        
        public static void FadeDownCasterAndTargets(CharacterStateMachine caster, PooledSet<CharacterStateMachine> targets, float duration)
        {
            if (caster.Display.AssertSome(out CharacterDisplay casterDisplay))
                casterDisplay.FadeRenderer(endValue: 0f, duration);

            foreach (CharacterStateMachine target in targets)
                if (target.Display.AssertSome(out CharacterDisplay display))
                    display.FadeRenderer(endValue: 0f, duration);
        }
        
        public static void FadeUpCasterAndTargets(CharacterStateMachine caster, PooledSet<CharacterStateMachine> targets, float duration)
        {
            if (caster.Display.TrySome(out CharacterDisplay casterDisplay) && casterDisplay.AnimationStatus is not AnimationStatus.Defeated and not AnimationStatus.Grappled)
                casterDisplay.FadeRenderer(endValue: 1f, duration);

            foreach (CharacterStateMachine target in targets)
                if (target.Display.TrySome(out CharacterDisplay display) && display.AnimationStatus is not AnimationStatus.Defeated and not AnimationStatus.Grappled)
                    display.FadeRenderer(endValue: 1f, duration);
        }
        
        public static void LerpCharactersToOriginalPositions(Dictionary<CharacterStateMachine, Vector3> startPositions, CharacterStateMachine caster, IReadOnlyCollection<CharacterStateMachine> targets, float duration)
        {
            foreach (CharacterStateMachine target in targets)
            {
                if (target == caster || target.StateEvaluator.PureEvaluate() is CharacterState.Defeated ||
                    target.Display.TrySome(out CharacterDisplay targetDisplay) == false ||
                    startPositions.TryGetValue(target, out Vector3 targetPosition) == false)
                    continue;
                
                targetDisplay.transform.DOMove(endValue: targetPosition, duration);
                targetDisplay.transform.DOScale(endValue: Vector3.one, duration);
            }
            
            if (caster.StateEvaluator.PureEvaluate() is not CharacterState.Defeated && caster.Display.TrySome(out CharacterDisplay casterDisplay) && startPositions.TryGetValue(caster, out Vector3 casterPosition))
            {
                casterDisplay.transform.DOMove(endValue: casterPosition, duration);
                casterDisplay.transform.DOScale(endValue: Vector3.one, duration);
            }
        }
        
        public static void FadeUpOutsideCharacters(IReadOnlyCollection<CharacterStateMachine> outsiders, float duration)
        {
            foreach (CharacterStateMachine character in outsiders)
                if (character.Display.AssertSome(out CharacterDisplay display) && display.AnimationStatus is not AnimationStatus.Defeated and not AnimationStatus.Grappled)
                    display.FadeRenderer(endValue: 1f, duration);
        }

        public static void FadeUpActionSplashScreenAndSpeedLines(CombatManager combatManager, float duration)
        {
            combatManager.ActionAnimator.FadeOutActionSplashScreenAndSpeedLines(duration);
        }

        public static void FadeUpUIAndBackground(CombatManager combatManager, float duration)
        {
            combatManager.ActionAnimator.FadeUpUIAndBackground(duration);
        }
        
        public static void MoveAllToDefaultAnimationAndFadeUpBars(CombatManager combatManager, IReadOnlyCollection<CharacterStateMachine> targets, CharacterStateMachine caster, float barsFadeDuration)
        {
            if (caster.Display.TrySome(out CharacterDisplay casterDisplay))
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
                if (caster == target || target.Display.TrySome(out CharacterDisplay targetDisplay) == false)
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
                if (character.Display.TrySome(out CharacterDisplay characterDisplay))
                {
                    characterDisplay.FadeBars(endValue: 1f, barsFadeDuration);
                    characterDisplay.AllowIndicatorsExternally(true);
                    characterDisplay.SetBaseSpeed(1f);
                }
            }
            
            combatManager.PositionManager.MoveAllToDefaultPosition(baseDuration: PositionManager.CharacterMoveDuration);
        }
        
        public static void SetRecoveryAndNotifyFinished(ListPool.ListPool<ActionResult> results, float recovery, CharacterStateMachine caster, PlannedSkill plan)
        {
            if (plan.CostFree == false && caster.StateEvaluator.PureEvaluate() is not CharacterState.Defeated 
                                                                                  and not CharacterState.Corpse
                                                                                  and not CharacterState.Downed
                                                                                  and not CharacterState.Grappled)
            {
                caster.RecoveryModule.SetInitial(recovery);
                if (caster.Display.TrySome(out CharacterDisplay display))
                    caster.RecoveryModule.ForceUpdateDisplay(display);
            }

            plan.NotifyDone();
            caster.Events.OnActionCompleted(results);
        }

        public static void IncrementSkillCounter(PlannedSkill plan, CharacterStateMachine caster)
        {
            ISkillModule casterSkillModule = caster.SkillModule;
            casterSkillModule.SkillUseCounters.TryGetValue(plan.Skill, out uint current);
            casterSkillModule.SkillUseCounters[plan.Skill] = current + 1;
        }

        public static void FillDefaultEndPositions(CombatManager combatManager, IReadOnlyCollection<CharacterStateMachine> targets, PlannedSkill plan,
                                                         CharacterStateMachine caster, Dictionary<CharacterStateMachine, Vector3> endPositionsToFill)
        {
            HashSet<CharacterStateMachine> charactersToCalculate = new(targets) { caster };
            combatManager.PositionManager.FillDefaultAnimationPositions(endPositionsToFill, caster: caster, charactersToCalculate, skill: plan.Skill);
        }
        
        public static void FillEndPositionsForOverlay(CombatManager combatManager, IReadOnlyCollection<CharacterStateMachine> targets, CharacterStateMachine caster, Dictionary<CharacterStateMachine, Vector3> endPositionsToFill)
        {
            HashSet<CharacterStateMachine> charactersToCalculate = new(targets) { caster };
            combatManager.PositionManager.FillTemptAnimationPositions(endPositionsToFill, caster: caster, charactersToCalculate);
        }

        public static void AnimateIndicators(CharacterStateMachine caster, IReadOnlyCollection<CharacterStateMachine> targets, Dictionary<CharacterStateMachine, Vector3> startPositionsToFill)
        {
            if (caster.Display.AssertSome(out CharacterDisplay casterDisplay))
            {
                startPositionsToFill[caster] = casterDisplay.transform.position;
                casterDisplay.AnimateIndicatorsForAction();
            }

            foreach (CharacterStateMachine target in targets)
            {
                if (target.Display.AssertSome(out CharacterDisplay targetDisplay) == false)
                    continue;

                startPositionsToFill[target] = targetDisplay.transform.position;
                targetDisplay.AnimateIndicatorsForAction();
            }
        }

        public static bool TryFillTargetList(PlannedSkill plan, CharacterStateMachine caster, PooledSet<CharacterStateMachine> outsiders, PooledSet<CharacterStateMachine> targets)
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