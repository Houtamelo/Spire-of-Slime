using System;
using System.Collections.Generic;
using Core.Pause_Menu.Scripts;
using Core.Utils.Extensions;
using Core.Utils.Handlers;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using Yarn.Unity;

namespace Core.Visual_Novel.Scripts
{
    public class DialogueDisplay : DialogueViewBase
    {
        private const float AutoPlayDelay = 3f;
        private static float DelayBetweenCharacters => PauseMenuManager.TextDelayHandler.Value;

        [SerializeField, Required]
        private TMP_Text dialogueText, rightSpeakerText, leftSpeakerText;

        [SerializeField, Required]
        private GameObject dialogueUI;

        [SerializeField, Required]
        private GameObject leftSpeakerUI, rightSpeakerUI;

        [SerializeField, Required]
        private PortraitDisplay leftCharacterPortrait, rightCharacterPortrait;

        [SerializeField, Required]
        private DialogueRunner dialogueRunner;

        [SerializeField, Required]
        private OptionButton optionButtonPrefab;

        [SerializeField, Required]
        private Transform optionButtonParent;

        [SerializeField, Required]
        private CanvasGroup dialogueBoxOnlyCanvasGroup;

        [SerializeField, Required]
        private AudioSource typewriterAudioSource;

        [SerializeField, Required]
        private AudioClip typewriterClip;

        [NonSerialized]
        public static readonly ValueHandler<bool> AutoPlayHandler = new();

        [NonSerialized]
        public static readonly ValueHandler<bool> SkipHandler = new();

        private readonly List<OptionButton> _optionButtons = new();

        private float _autoPlayTimer;

        private Tween _fadeUITween;
        private Tween _textTween;
        private TweenCallback _onTextTweenUpdate;
        private Action _onLineFinished;

        private bool _allowTypeWriterSound;
        private bool _waitingForOptionSelection;
        
        private void Awake()
        {
            _onTextTweenUpdate = OnTextTweenUpdate;
        }

        private void Start()
        {
            AutoPlayHandler.Changed += AutoPlayToggled;
            SkipHandler.Changed += SkipToggled;
            
            _allowTypeWriterSound = PauseMenuManager.TypeWriterSoundHandler.Value;
            PauseMenuManager.TypeWriterSoundHandler.Changed += TypeWriterSoundChanged;

            if (InputManager.AssertInstance(out InputManager inputManager))
                inputManager.PerformedActionsCallbacks[InputEnum.AdvanceDialogue].Add(OnAdvanceDialogueAction);
        }

        private void OnDestroy()
        {
            AutoPlayHandler.Changed -= AutoPlayToggled;
            SkipHandler.Changed -= SkipToggled;
            
            PauseMenuManager.TypeWriterSoundHandler.Changed -= TypeWriterSoundChanged;
            
            if (InputManager.Instance.TrySome(out InputManager inputManager))
                inputManager.PerformedActionsCallbacks[InputEnum.AdvanceDialogue].Remove(OnAdvanceDialogueAction);
        }

        private void TypeWriterSoundChanged(bool active) => _allowTypeWriterSound = active;

        private void OnAdvanceDialogueAction()
        {
            if (dialogueRunner.IsDialogueRunning == false)
                return;

            UserRequestedViewAdvancement();
        }

        private void Update()
        {
            if (!AutoPlayHandler.Value || !dialogueUI.activeInHierarchy || _textTween is { active: true } || dialogueRunner.IsRunningIEnumeratorCommand)
                return;
            
            _autoPlayTimer += Time.deltaTime;
            if (_autoPlayTimer < AutoPlayDelay)
                return;
            
            _autoPlayTimer = 0f;
            UserRequestedViewAdvancement();
        }

        private void ResetAutoPlayTimer() => _autoPlayTimer = 0f;

        public override void DialogueComplete()
        {
            _textTween.KillIfActive();
            _fadeUITween.KillIfActive();
            dialogueUI.SetActive(false);
            dialogueText.text = string.Empty;
            rightSpeakerText.text = string.Empty;
            leftSpeakerText.text = string.Empty;
            DisableSpeakerUI();
            leftCharacterPortrait.HideAll();
            rightCharacterPortrait.HideAll();
        }

        public override void DialogueStarted()
        {
            if (dialogueUI.activeSelf && dialogueBoxOnlyCanvasGroup.alpha >= 1)
                return;

            dialogueBoxOnlyCanvasGroup.DOKill();
            dialogueBoxOnlyCanvasGroup.alpha = 0f;
            dialogueUI.SetActive(true);
            dialogueBoxOnlyCanvasGroup.DOFade(endValue: 1, duration: 0.5f).SetUpdate(isIndependentUpdate: true);
        }

        public override void DismissLine(Action onDismissalComplete)
        {
            _textTween.CompleteIfActive();
            
            leftCharacterPortrait.FinishAnimation();
            rightCharacterPortrait.FinishAnimation();
            onDismissalComplete?.Invoke();
        }

        public override void InterruptLine(LocalizedLine dialogueLine, Action onDialogueLineFinished)
        {
            _textTween.CompleteIfActive();
            
            leftCharacterPortrait.FinishAnimation();
            rightCharacterPortrait.FinishAnimation();
            if (SkipHandler.Value)
            {
                onDialogueLineFinished?.Invoke();
                return;
            }
            
            _onLineFinished = onDialogueLineFinished;
            ResetAutoPlayTimer();
        }

        public override void RunLine(LocalizedLine dialogueLine, Action onDialogueLineFinished)
        {
            _textTween.KillIfActive();
            
            SetUpSpeakers(dialogueLine);
            
            dialogueText.text = "";
            string targetText = dialogueLine.TextWithoutCharacterName.Text;

            if (SkipHandler.Value)
            {
                dialogueText.text = targetText;
                leftCharacterPortrait.FinishAnimation();
                rightCharacterPortrait.FinishAnimation();
                onDialogueLineFinished?.Invoke();
                return;
            }

            if (DelayBetweenCharacters > 0f + float.Epsilon)
            {
                _textTween = dialogueText.DOText(targetText, DelayBetweenCharacters * targetText.Length);
                _textTween.OnUpdate(_onTextTweenUpdate);
            }
            else
            {
                dialogueText.text = targetText;
            }

            _onLineFinished = onDialogueLineFinished;
            ResetAutoPlayTimer();
        }

        public override void RunOptions(DialogueOption[] dialogueOptions, Action<int> onOptionSelected)
        {
            _textTween.CompleteIfActive();
            
            leftCharacterPortrait.FinishAnimation();
            rightCharacterPortrait.FinishAnimation();
            
            for (int i = _optionButtons.Count; i < dialogueOptions.Length; i++) 
                CreateOptionButton();

            _waitingForOptionSelection = true;
            int index = 0;
            for (; index < dialogueOptions.Length; index++)
            {
                OptionButton button = _optionButtons[index];
                DialogueOption option = dialogueOptions[index];
                button.Initialize(option, index, onOptionSelected, this);
            }
            
            for (; index < _optionButtons.Count; index++)
                _optionButtons[index].ResetMe();
            
            ResetAutoPlayTimer();
        }

        public void OptionButtonClicked()
        {
            _waitingForOptionSelection = false;
            foreach (OptionButton button in _optionButtons)
                button.ResetMe();
        }

        private void CreateOptionButton()
        {
            OptionButton button = optionButtonPrefab.InstantiateWithFixedLocalScale(optionButtonParent, worldPositionStays: true);
            _optionButtons.Add(button);
        }
        public override void UserRequestedViewAdvancement()
        {
            if (dialogueRunner.IsRunningIEnumeratorCommand || _waitingForOptionSelection)
                return;
            
            if (_textTween is { active: true })
                requestInterrupt.Invoke();
            else
                _onLineFinished?.Invoke();
        }

        private void DisableSpeakerUI()
        {
            leftSpeakerUI.SetActive(false);
            rightSpeakerUI.SetActive(false);
        }

        private void SetUpSpeakers(LocalizedLine dialogueLine)
        {
            string speakerName = dialogueLine.CharacterName;
            string leftData = null;
            string rightData = null;
            bool leftDataExists = false;
            bool rightDataExists = false;
            if (dialogueLine.Metadata.IsNullOrEmpty() == false)
            {
                foreach (string data in dialogueLine.Metadata)
                {
                    if (data.Contains(YarnTags.LeftPortrait))
                    {
                        leftData = data[5..];

                        leftDataExists = true;
                        if (rightDataExists)
                            break;
                    }
                    else if (data.Contains(YarnTags.RightPortrait))
                    {
                        rightData = data[6..];
                        rightDataExists = true;
                        if (leftDataExists)
                            break;
                    }
                }
            }
            
            if (speakerName.IsSome())
            {
                bool isSpeakerLeft = false;
                bool foundSpeakerSide = false;
                string speakerLowerInvariant = speakerName.ToAlphaNumericLower();
                if (leftDataExists && leftData.ToAlphaNumericLower().Contains(speakerLowerInvariant))
                {
                    isSpeakerLeft = true;
                    foundSpeakerSide = true;
                }
                
                if (rightDataExists && rightData.ToAlphaNumericLower().Contains(speakerLowerInvariant))
                {
                    isSpeakerLeft = false;
                    foundSpeakerSide = true;
                }

                leftCharacterPortrait.SetPortrait(leftData, isSpeaker: foundSpeakerSide && isSpeakerLeft);
                rightCharacterPortrait.SetPortrait(rightData, isSpeaker: foundSpeakerSide && isSpeakerLeft == false);
                switch (foundSpeakerSide)
                {
                    case true when isSpeakerLeft:
                        leftSpeakerText.text = speakerName;
                        leftSpeakerUI.SetActive(true);
                        rightSpeakerUI.SetActive(false);
                        break;
                    case true:
                        rightSpeakerText.text = speakerName;
                        rightSpeakerUI.SetActive(true);
                        leftSpeakerUI.SetActive(false);
                        break;
                    default:
                        if (rightSpeakerText.text == speakerName)
                        {
                            rightSpeakerUI.SetActive(true);
                            leftSpeakerUI.SetActive(false);
                        }
                        else if (leftSpeakerText.text == speakerName)
                        {
                            leftSpeakerUI.SetActive(true);
                            rightSpeakerUI.SetActive(false);
                        }
                        else if (speakerName is "Nema")
                        {
                            leftSpeakerText.text = "Nema";
                            leftSpeakerUI.SetActive(true);
                            rightSpeakerUI.SetActive(false);
                        }
                        else if (speakerName is "Ethel")
                        {
                            rightSpeakerText.text = "Ethel";
                            rightSpeakerUI.SetActive(true);
                            leftSpeakerUI.SetActive(false);
                        }
                        else
                        {
                            rightSpeakerText.text = speakerName;
                            rightSpeakerUI.SetActive(true);
                            leftSpeakerUI.SetActive(false);
                        }
                        break;
                }
            }
            else
            {
                leftCharacterPortrait.SetPortrait(leftData, isSpeaker: false);
                rightCharacterPortrait.SetPortrait(rightData, isSpeaker: false);
                DisableSpeakerUI();
            }
        }

        private void AutoPlayToggled(bool _)
        {
            ResetAutoPlayTimer();
        }

        private void SkipToggled(bool value)
        {
            if (value)
                UserRequestedViewAdvancement();
        }

        public void DisableAutoPlay() => AutoPlayHandler.SetValue(false);

        private int _previousTextLength;
        
        private void OnTextTweenUpdate()
        {
            if (_allowTypeWriterSound == false)
                return;
            
            int currentTextLength = dialogueText.text.Length;
            if (currentTextLength != _previousTextLength)
            {
                typewriterAudioSource.PlayOneShot(typewriterClip, 0.5f);
                _previousTextLength = currentTextLength;
            }
        }
    }
}