/*using System;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Cues;
using Core.Combat.Scripts.Managers;
using Core.Shaders;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Utils.Extensions;
using Utils.Objects;
using Random = UnityEngine.Random;

namespace Core.Combat.Scripts.VisualPrompts
{
    public class LustPromptDisplay : MonoBehaviour, IPointerDownHandler
    {
        public static bool DEBUGFIRST = true;
        
        private const float FadeDelay = 1f;
        private const float MinStartPoint = 0.25f;
        private const float MaxEndPoint = 1f;

        private const float MinOuterSize = 0.05f;
        private const float MaxOuterSize = 0.15f;

        private const float MinInnerSize = 0.016667f;
        private const float MaxInnerSize = 0.05f;

        [SerializeField, Required]
        private LighterAnimator arrowLighter;

        [SerializeField, Required]
        private Image outerFill;
        
        [SerializeField, Required]
        private LighterAnimator outerFillLighter;

        [SerializeField, Required]
        private Image innerFill;

        [SerializeField, Required]
        private LighterAnimator innerFillLighter;

        [SerializeField, Required]
        private Image passiveCharacterPortrait;

        [SerializeField, Required]
        private Image activeCharacterPortrait;

        [SerializeField, Required]
        private CanvasGroup canvasGroup;

        [SerializeField, Required]
        private CustomAudioSource startSound;

        [SerializeField, Required]
        private CustomAudioSource failSound, successSound, perfectSound;

        [SerializeField, Required]
        private Transform arrowTransform;
        
        [SerializeField, Required]
        private Transform outerFillTransform;
        
        [SerializeField, Required]
        private Transform innerFillTransform;

        [SerializeField, Required]
        private TMP_Text countdownTmp;

        [SerializeField, Required]
        private Transform mouseIcon, keyboardKey;

        private State _state = State.Idle;

        private Tween _arrowTween;
        private Tween _fadeTween;
        private Tween _mouseIconTween, _keyboardKeyTween;

        private LustPromptRequest _currentRequest;
        private float _outerStartPoint;
        private float _outerEndPoint;
        private float _innerStartPoint;
        private float _innerEndPoint;
        private float _arrowAngularSpeed;

        public bool IsBusy => _currentRequest is { IsDone: false } || _arrowTween is { active: true } || _fadeTween is { active: true };

        private TweenCallback _rotateArrow;
        private TweenCallback _setCountdownTo0, _setCountdownTo1, _setCountdownTo2;

        private void Start()
        {
            _rotateArrow = RotateArrow;
            _setCountdownTo0 = () => countdownTmp.text = "0";
            _setCountdownTo1 = () => countdownTmp.text = "1";
            _setCountdownTo2 = () => countdownTmp.text = "2";
            gameObject.SetActive(false); // the combat scene begins with this enabled in order to let it initialize everything
        }

        private void OnDisable()
        {
            _arrowTween.KillIfActive();
            _fadeTween.KillIfActive();
            _mouseIconTween.KillIfActive();
            _keyboardKeyTween.KillIfActive();
            mouseIcon.transform.localScale = Vector3.one;
            keyboardKey.transform.localScale = Vector3.one;
        }

        private void Update()
        {
            switch (_state)
            {
                case State.Idle:
                case State.WaitingForArrowToBegin:
                case State.Deactivating:
                    return;
                case State.WaitingForArrowToFinish:
                    if (Keyboard.current.zKey.wasPressedThisFrame)
                        ResolveInputPress();
                    
                    break;
                default:                            
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Play(LustPromptRequest request)
        {
            _arrowTween.KillIfActive();
            _fadeTween.KillIfActive();
            _mouseIconTween.KillIfActive();
            _keyboardKeyTween.KillIfActive();
            mouseIcon.transform.localScale = Vector3.one;
            keyboardKey.transform.localScale = Vector3.one;

            if (_state is not State.Idle)
            {
                Debug.LogWarning($"Starting new prompt but state isn't idle, state: {_state}");
                _currentRequest.SetDone(LustPromptRequest.Outcome.Success);
                _currentRequest.Resolve();
            }
            
            if (_currentRequest is { IsDone: false })
            {
                Debug.LogWarning($"Starting new prompt while previous one is still active, character: {_currentRequest.PassiveCharacter.Script.CharacterName}, lust: {_currentRequest.Lust.ToString()}");
                _currentRequest.SetDone(LustPromptRequest.Outcome.Success);
                _currentRequest.Resolve();
            }

            _currentRequest = request;
            
            float difficulty = (_currentRequest.Lust - 100) / 100f;

            if (CombatManager.DEBUGMODE == false && request.PassiveCharacter.Script.IsControlledByPlayer == false)
            {
                _state = State.Idle;
                float rand = Random.value;
                LustPromptRequest.Outcome outcome = rand switch
                {
                    _ when rand < difficulty => LustPromptRequest.Outcome.Failure,
                    _ when rand > (1 - difficulty) * (1 - MaxInnerSize / MaxOuterSize) + difficulty => LustPromptRequest.Outcome.Perfect,
                    _ => LustPromptRequest.Outcome.Success
                };
                
                request.SetDone(outcome);
                
                if (request.PassiveCharacter.Display.TrySome(out CharacterDisplay display))
                {
                    CombatCueOptions options = CombatCueOptions.Default($"{outcome.ToString()} lust prompt!", ColorReferences.Lust, display);
                    options.CanShowOnTopOfOthers = true;
                    if (CombatTextCueManager.Instance.AssertSome(out CombatTextCueManager cueManager))
                        cueManager.EnqueueAboveCharacter(ref options, display);

                    DOVirtual.DelayedCall(options.Duration, request.Resolve);
                }
                return;
            }

            _state = State.WaitingForArrowToBegin;
            gameObject.SetActive(true);
            passiveCharacterPortrait.sprite = request.PassiveCharacter.Script.LustPromptPortrait;
            activeCharacterPortrait.sprite = request.ActiveCharacter.Script.LustPromptPortrait;

            _mouseIconTween = mouseIcon.DOScale(endValue: new Vector3(1.3f, 1.3f, 1.3f), duration: 1).SetLoops(loops: -1, LoopType.Yoyo);
            _keyboardKeyTween = keyboardKey.DOScale(endValue: new Vector3(1.3f, 1.3f, 1.3f), duration: 1).SetLoops(loops: -1, LoopType.Yoyo);

            outerFill.fillAmount = 0;
            innerFill.fillAmount = 0;
            canvasGroup.alpha = 0f;
            arrowTransform.localEulerAngles = Vector3.zero;
            
            float arrowSpeedMultiplier = Random.value + 0.5f;
            _arrowAngularSpeed = arrowSpeedMultiplier * 150f * (1f + difficulty);
            
            float outerSize = Mathf.Lerp(MinOuterSize, MaxOuterSize, 1f - difficulty) * arrowSpeedMultiplier;
            _outerStartPoint = Random.Range(MinStartPoint, MaxEndPoint - outerSize);
            _outerEndPoint = _outerStartPoint + outerSize;
            outerFillTransform.localEulerAngles = new Vector3(0, 0, -_outerStartPoint * 360f);
            outerFill.fillAmount = outerSize;

            float innerSize = Mathf.Lerp(MinInnerSize, MaxInnerSize, 1f - difficulty) * arrowSpeedMultiplier;
            _innerStartPoint = Random.Range(_outerStartPoint, _outerEndPoint - innerSize);
            _innerEndPoint = _innerStartPoint + innerSize;
            innerFillTransform.localEulerAngles = new Vector3(0, 0, -_innerStartPoint * 360f);
            innerFill.fillAmount = innerSize;
            
            startSound.Play();
            countdownTmp.text = "3";
            countdownTmp.SetAlpha(0f);
            
            Sequence sequence = DOTween.Sequence();
            sequence.Append(DOVirtual.Float(from: 0f, to: 1f, FadeDelay, SetCanvasAlpha));
            
            const string firstPromptEver = "First_Prompt_Ever";

            if (PlayerPrefs.GetInt("Reset_Lust-Prompt_Once", defaultValue: 0) == 0)
            {
                PlayerPrefs.SetInt("Reset_Lust-Prompt_Once", 1);
                PlayerPrefs.SetInt(firstPromptEver, 0);
            }

            if (PlayerPrefs.GetInt(firstPromptEver, defaultValue: 0) == 0 || (DEBUGFIRST && Application.isEditor))
            {
                PlayerPrefs.SetInt(firstPromptEver, 1);
                sequence.Append(innerFillLighter.Animate(amplitude: 0.5f, fullLoopCount: 3, ColorReferences.Lust));
                sequence.AppendInterval(0.5f);
                sequence.Append(arrowLighter.Animate(amplitude: 0.5f, fullLoopCount: 3, ColorReferences.Lust));
            }
            
            sequence.Append(countdownTmp.DOFade(endValue: 1f, duration: 0.5f));
            sequence.Append(DOVirtual.DelayedCall(delay: 1f, _setCountdownTo2, ignoreTimeScale: false));
            sequence.Append(DOVirtual.DelayedCall(delay: 1f, _setCountdownTo1, ignoreTimeScale: false));
            sequence.Append(DOVirtual.DelayedCall(delay: 1f, _setCountdownTo0, ignoreTimeScale: false));
            sequence.AppendInterval(1f);
            sequence.Append(countdownTmp.DOFade(endValue: 0f, duration: 0.5f));
            sequence.onComplete += _rotateArrow;
            
            _fadeTween = sequence;
        }

        private void SetCanvasAlpha(float value) => canvasGroup.alpha = value;

        private void RotateArrow()
        {
            _arrowTween.KillIfActive();
            _fadeTween.KillIfActive();
            _state = State.WaitingForArrowToFinish;
            canvasGroup.interactable = true;
            _arrowTween = arrowTransform.DOLocalRotate(new Vector3(0f, 0f, -360f), _arrowAngularSpeed, RotateMode.FastBeyond360)
                .OnComplete(Failure)
                .SetEase(Ease.Linear)
                .SetSpeedBased();
        }

        private void Failure()
        {
            if (_currentRequest is not { IsDone: false })
            {
                Deactivate();
                return;
            }

            failSound.Play();
            canvasGroup.interactable = false;
            _currentRequest.SetDone(LustPromptRequest.Outcome.Failure);
            FadeAndDeactivate(_currentRequest);
        }

        private void Deactivate()
        {
            _arrowTween.KillIfActive();
            _fadeTween.KillIfActive();
            _mouseIconTween.KillIfActive();
            _keyboardKeyTween.KillIfActive();
            mouseIcon.transform.localScale = Vector3.one;
            keyboardKey.transform.localScale = Vector3.one;
            _state = State.Idle;
            canvasGroup.interactable = false;
            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (_currentRequest is not { IsDone: false })
            {
                Debug.LogWarning("Registering click while no prompt is active");
                Deactivate();
                return;
            }

            switch (_state)
            {
                case State.Idle: Debug.LogWarning("Registering click but prompt is idle."); break;
                case State.Deactivating:
                case State.WaitingForArrowToBegin:  
                    return;
            }
            
            ResolveInputPress();
        }

        private void ResolveInputPress()
        {
            if (_arrowTween is not { active: true })
                return;

            _arrowTween.KillIfActive();
            _mouseIconTween.KillIfActive();
            _keyboardKeyTween.KillIfActive();
            mouseIcon.transform.localScale = Vector3.one;
            keyboardKey.transform.localScale = Vector3.one;

            float angle = arrowTransform.localEulerAngles.z;
            float arrowRotation = (angle >= 0 ? angle - 360f : angle) / -360f;
            if (arrowRotation >= _innerStartPoint && arrowRotation <= _innerEndPoint)
            {
                perfectSound.Play();
                innerFillLighter.Animate(amplitude: 0.25f, fullLoopCount: 3, ColorReferences.Lust);
                _currentRequest.SetDone(LustPromptRequest.Outcome.Perfect);
            }
            else if (arrowRotation >= _outerStartPoint && arrowRotation <= _outerEndPoint)
            {
                successSound.Play();
                outerFillLighter.Animate(amplitude: 0.25f, fullLoopCount: 3, ColorReferences.Lust);
                _currentRequest.SetDone(LustPromptRequest.Outcome.Success);
            }
            else
            {
                failSound.Play();
                _currentRequest.SetDone(LustPromptRequest.Outcome.Failure);
            }

            canvasGroup.interactable = false;
            FadeAndDeactivate(_currentRequest);
        }

        private void FadeAndDeactivate(LustPromptRequest request)
        {
            _state = State.Deactivating;
            request.Resolve();
            _fadeTween.KillIfActive();
            _fadeTween = canvasGroup.DOFade(endValue: 0f, FadeDelay).SetDelay(1.5f);
            _fadeTween.onComplete += Deactivate;
        }

        private enum State
        {
            Idle,
            WaitingForArrowToBegin,
            WaitingForArrowToFinish,
            Deactivating
        }
    }
}*/