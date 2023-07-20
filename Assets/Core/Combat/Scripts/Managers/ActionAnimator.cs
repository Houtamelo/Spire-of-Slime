using System;
using System.Collections;
using System.Collections.Generic;
using Core.Combat.Scripts.Animations;
using Core.Combat.Scripts.BackgroundGeneration;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Cues;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Misc;
using Core.Utils.Async;
using Core.Utils.Collections.Extensions;
using Core.Utils.Extensions;
using Core.Utils.Patterns;
using DG.Tweening;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Core.Combat.Scripts.Managers
{
    public class ActionAnimator : MonoBehaviour
    {
        private const float SpeedLinesMaximumAlpha = 160f / 255f;

        private const float SpeedLinesSpeedBaseMultiplier = 9f;
        private static float SpeedLinesSpeedMultiplier => SpeedLinesSpeedBaseMultiplier * IActionSequence.SpeedMultiplier;
        
        private static readonly int OrgasmCameraTrigger = Animator.StringToHash("Orgasm");

        [SerializeField, Required, SceneObjectsOnly]
        private CanvasGroup uiCanvasGroup;

        [SerializeField, Required, SceneObjectsOnly]
        private SpriteRenderer splashScreenBackground;
        
        [SerializeField, Required, SceneObjectsOnly]
        private SpriteRenderer[] leftFacingSpeedLines, rightFacingSpeedLines;
        
        [SerializeField, Required, SceneObjectsOnly]
        private Transform leftFacingSpeedLinesParent, rightFacingSpeedLinesParent;

        [SerializeField, Required, SceneObjectsOnly]
        private CombatManager combatManager;

        [SerializeField, Required, SceneObjectsOnly]
        private PixelPerfectWithZoom cameraZoom;
        
        [SerializeField, Required, SceneObjectsOnly]
        private GameObject raycastBlocker;

        [SerializeField, Required, SceneObjectsOnly]
        private SpriteRenderer grappleMistRenderer;

        private Vector3 _splashScreenDefaultPosition;
        private Vector3 _leftFacingSpeedLinesDefaultPosition, _rightFacingSpeedLinesDefaultPosition;

        private Tween _splashScreenFadeTween, _splashScreenMovementTween;
        private Tween _leftFacingSpeedLinesFadeTween, _leftFacingSpeedLinesMovementTween;
        private Tween _rightFacingSpeedLinesFadeTween, _rightFacingSpeedLinesMovementTween;
        private Tween _uiFadeTween;

        private void Start()
        {
            _splashScreenDefaultPosition = splashScreenBackground.transform.position;
            _leftFacingSpeedLinesDefaultPosition = leftFacingSpeedLinesParent.position;
            _rightFacingSpeedLinesDefaultPosition = rightFacingSpeedLinesParent.position;
        }

        private void OnDestroy()
        {
            _splashScreenFadeTween.KillIfActive();
            _splashScreenMovementTween.KillIfActive();
            _leftFacingSpeedLinesFadeTween.KillIfActive();
            _leftFacingSpeedLinesMovementTween.KillIfActive();
            _rightFacingSpeedLinesFadeTween.KillIfActive();
            _rightFacingSpeedLinesMovementTween.KillIfActive();
            _uiFadeTween.KillIfActive();
        }

        public void FadeDownUIAndBackground(float duration)
        {
            _uiFadeTween.KillIfActive();
            _uiFadeTween = uiCanvasGroup.DOFade(endValue: 0f, duration);
            if (combatManager.Background.AssertSome(out CombatBackground background))
            {
                background.Fade(alpha: 0f, duration);
                background.SwitchLightsToSkillAnimation(duration);
            }
            
            raycastBlocker.SetActive(true);
        }

        public void FadeUpUIAndBackground(float duration)
        {
            _uiFadeTween.KillIfActive();
            _uiFadeTween = uiCanvasGroup.DOFade(endValue: 1f, duration);
            if (combatManager.Background.TrySome(out CombatBackground background))
            {
                background.Fade(alpha: 1, duration);
                background.SwitchLightsToNormal(duration);
            }
            
            raycastBlocker.SetActive(false);
        }

        public void FadeUpActionSplashScreenAndSpeedLines([NotNull] PlannedSkill plan, float fadeDuration, float animationDuration)
        {
            _splashScreenFadeTween.KillIfActive();
            _splashScreenMovementTween.KillIfActive();
            _leftFacingSpeedLinesFadeTween.KillIfActive();
            _leftFacingSpeedLinesMovementTween.KillIfActive();
            _rightFacingSpeedLinesFadeTween.KillIfActive();
            _rightFacingSpeedLinesMovementTween.KillIfActive();

            splashScreenBackground.transform.position = _splashScreenDefaultPosition;
            splashScreenBackground.SetAlpha(0f);

            leftFacingSpeedLinesParent.position = _leftFacingSpeedLinesDefaultPosition;
            leftFacingSpeedLines.DoForEach(line => line.SetAlpha(0f));
            
            rightFacingSpeedLinesParent.position = _rightFacingSpeedLinesDefaultPosition;
            rightFacingSpeedLines.DoForEach(line => line.SetAlpha(0f));
            
            _splashScreenFadeTween = splashScreenBackground.DOFade(endValue: 1f, fadeDuration);

            ISkill skill = plan.Skill;
            CharacterStateMachine caster = plan.Caster;
            bool isCasterLeft = caster.PositionHandler.IsLeftSide;
            (float xSpeed, AnimationCurve curve) casterMovement = skill.GetCasterMovement(isCasterLeft);
            
            float movementDuration = (fadeDuration * 2f) + animationDuration;
            _splashScreenMovementTween = splashScreenBackground.transform.DOMoveX(endValue: casterMovement.xSpeed * Random.Range(0.9f, 1.1f) * 0.5f, movementDuration).SetRelative().SetEase(casterMovement.curve);
            
            if (casterMovement.xSpeed > 0f)
                _rightFacingSpeedLinesFadeTween = rightFacingSpeedLines.DOFade(SpeedLinesMaximumAlpha, fadeDuration);
            else if (casterMovement.xSpeed < 0f)
                _leftFacingSpeedLinesFadeTween = leftFacingSpeedLines.DOFade(SpeedLinesMaximumAlpha, fadeDuration);
        }

        public void AnimateSpeedLines([NotNull] PlannedSkill plan, float duration)
        {
            ISkill skill = plan.Skill;
            CharacterStateMachine caster = plan.Caster;
            bool isCasterLeft = caster.PositionHandler.IsLeftSide;
            (float xSpeed, AnimationCurve curve) casterMovement = skill.GetCasterMovement(isCasterLeft);

            if (casterMovement.xSpeed > 0f)
            {
                _rightFacingSpeedLinesMovementTween = rightFacingSpeedLinesParent.DOMoveX(casterMovement.xSpeed  * Random.Range(0.9f, 1.1f) * SpeedLinesSpeedMultiplier, duration);
                _rightFacingSpeedLinesMovementTween.SetRelative();
            }
            else if (casterMovement.xSpeed < 0f)
            {
                _leftFacingSpeedLinesMovementTween = leftFacingSpeedLinesParent.DOMoveX(casterMovement.xSpeed * Random.Range(0.9f, 1.1f) * SpeedLinesSpeedMultiplier, duration);
                _leftFacingSpeedLinesMovementTween.SetRelative();
            }
        }
        
        public void FadeOutActionSplashScreenAndSpeedLines(float duration)
        {
            _splashScreenFadeTween.KillIfActive();
            _leftFacingSpeedLinesFadeTween.KillIfActive();
            _rightFacingSpeedLinesFadeTween.KillIfActive();
            
            _splashScreenFadeTween = splashScreenBackground.DOFade(endValue: 0f, duration);
            _leftFacingSpeedLinesFadeTween = leftFacingSpeedLines.DOFade(endValue: 0f, duration);
            _rightFacingSpeedLinesFadeTween = rightFacingSpeedLines.DOFade(endValue: 0f, duration);
        }
        
        public void AnimateOrgasm(DisplayModule owner)
        {
            if (CombatTextCueManager.AssertInstance(out CombatTextCueManager combatTextCueManager) == false)
                return;
            
            CombatCueOptions options = CombatCueOptions.Default("Orgasm!", ColorReferences.Lust, owner);
            combatTextCueManager.EnqueueAboveCharacter(ref options, owner);

            /*Vector3 cameraDestination;
            if (owner.StateMachine.AssertSome(out CharacterStateMachine stateMachine) &&
                stateMachine.StatusModule.GetStatuses.FindType<LustGrappled>().TrySome(out LustGrappled lustGrappled) && lustGrappled.IsActive &&
                lustGrappled.Restrainer.Display.AssertSome(out CharacterDisplay restrainerDisplay))
            {
                cameraDestination = restrainerDisplay.transform.position;
            }
            else
            {
                cameraDestination = owner.transform.position;
            }
            
            CoroutineWrapper zoomRoutine = combatManager.CameraZoomAt(zoomLevel: 1.5f, cameraDestination, zoomDuration: 1f, stayDuration: 5f);
            zoomRoutine.Started += _ => cameraAnimator.SetTrigger(OrgasmCameraTrigger);
            AnimationRoutineInfo animationRoutineInfo = AnimationRoutineInfo.WithoutCharacter(zoomRoutine);
            combatManager.Animations.Enqueue(animationRoutineInfo);*/
        }

        public Sequence AnimateCameraForAction([NotNull] IReadOnlyCollection<CharacterStateMachine> presentCharacters, float lerpDuration, float stayDuration)
        {
            const float defaultZoomOffset = 0.3f; // for maximum height of 3.6f
            float maxHeight = 0f;
            foreach (CharacterStateMachine character in presentCharacters)
            {
                if (character.Display.AssertSome(out DisplayModule display) == false)
                    continue;

                Option<Bounds> boundsOption = display.GetBounds();
                if (boundsOption.TrySome(out Bounds bounds) == false)
                    continue;

                float height = bounds.size.y;
                if (height > maxHeight)
                    maxHeight = height;
            }
            
            float zoomOffset = defaultZoomOffset * 3.6f / maxHeight;
            float zoomLevel = 1f + zoomOffset;
            
            Sequence sequence = DOTween.Sequence();
            sequence.Append(DOVirtual.Float(1f, zoomLevel, lerpDuration, cameraZoom.SetScale));
            sequence.AppendInterval(stayDuration);
            sequence.Append(DOVirtual.Float(zoomLevel, 1f, lerpDuration, cameraZoom.SetScale));
            return sequence;
        }

        [NotNull]
        public AnimationRoutineInfo AnimateOverlayMist(Action underTheMist, Option<CharacterStateMachine> owner, float fadeDuration = 1f, float stayDuration = 1f)
        {
            // AUTO START IS TRUE ON THIS ONE!
            CoroutineWrapper wrapper = new(GrappleMistRoutine(grappleMistRenderer, underTheMist, fadeDuration, stayDuration, combatManager), nameof(GrappleMistRoutine), context: this, autoStart: true);
            AnimationRoutineInfo info = owner.IsSome ? AnimationRoutineInfo.WithCharacter(wrapper, owner.Value, AnimationRoutineInfo.NoValidation) : AnimationRoutineInfo.WithoutCharacter(wrapper);
            combatManager.Animations.PriorityEnqueue(info);
            return info;
        }

        private static IEnumerator GrappleMistRoutine(SpriteRenderer grappleMistRenderer, Action underTheMist, float fadeDuration, float stayDuration, CombatManager combatManager)
        {
            if (grappleMistRenderer == null)
                yield break;

            grappleMistRenderer.gameObject.SetActive(true);
            grappleMistRenderer.SetAlpha(0f);
            yield return grappleMistRenderer.DOFade(endValue: 1f, fadeDuration).WaitForCompletion();

            if (grappleMistRenderer == null)
                yield break;

            underTheMist?.Invoke();
            if (combatManager != null)
                combatManager.PositionManager.MoveAllToDefaultPosition(baseDuration: Option<float>.None);
            
            yield return new WaitForSeconds(stayDuration);

            if (grappleMistRenderer == null)
                yield break;

            yield return grappleMistRenderer.DOFade(endValue: 0f, fadeDuration).WaitForCompletion();

            if (grappleMistRenderer != null)
                grappleMistRenderer.gameObject.SetActive(false);
        }
    }
}