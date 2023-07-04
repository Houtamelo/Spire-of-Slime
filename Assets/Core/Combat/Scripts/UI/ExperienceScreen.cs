using System;
using System.Collections.Generic;
using System.Linq;
using Core.Combat.Scripts.Interfaces;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils.Extensions;

namespace Core.Combat.Scripts.UI
{
    public class ExperienceScreen : MonoBehaviour
    {
        [SerializeField, Required]
        private CanvasGroup canvasGroup;

        [SerializeField, Required]
        private TMP_Text experienceText;

        [SerializeField, Required]
        private ExperienceScreenCharacterUI[] charactersUI = new ExperienceScreenCharacterUI[4];

        [SerializeField, Required]
        private Button continueButton;

        [SerializeField, Required]
        private AudioSource experienceSound;
        
        [SerializeField, Required]
        private AudioSource levelUpSound;

        [SerializeField, Required]
        private AudioSource continueSound;
        
        private IReadOnlyList<(ICharacterScript script, float startExp, float currentExp)> _allies;
        private float _totalEarnedExp;
        private Sequence _sequence;
        private Action _onContinueClicked;

        private void Start()
        {
            continueButton.onClick.AddListener(() =>
            {
                continueButton.interactable = false;
                _sequence.KillIfActive();
                experienceSound.Stop();

                for (int i = 0; i < _allies.Count; i++)
                {
                    (_, float startExp, float currentExp) = _allies[i];
                    ExperienceScreenCharacterUI characterUI = charactersUI[i];
                    characterUI.SetProgress(startExp, currentExp, 1f);
                }
                
                continueSound.Play();
                _onContinueClicked?.Invoke();
            });
        }

        private void OnDestroy()
        {
            _sequence.KillIfActive();
        }

        public void Play(IReadOnlyList<(ICharacterScript script, float startExp, float currentExp)> allies, Action onContinueClicked)
        {
            _sequence.KillIfActive();
            Debug.Assert(allies.Count <= 4, "ExperienceScreen can only handle up to 4 allies", context: this);

            if (_onContinueClicked != null)
            {
                Debug.LogWarning("ExperienceScreen is already playing, this is probably a bug, however things might work normally", context: this);
            }

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            _allies = allies;
            gameObject.SetActive(true);
            continueButton.interactable = true;
            _onContinueClicked = onContinueClicked;
            
            _totalEarnedExp = allies.Sum(tuple => tuple.currentExp - tuple.startExp);
            experienceText.text = (_totalEarnedExp * 100f).ToString("000");

            int index = 0;
            for (; index < allies.Count; index++)
            {
                (ICharacterScript script, float startExp, _) = allies[index];
                ExperienceScreenCharacterUI characterUI = charactersUI[index];
                characterUI.SetCharacter(script, startExp);
            }
            
            for (; index < charactersUI.Length; index++)
                charactersUI[index].gameObject.SetActive(false);

            _sequence = DOTween.Sequence().SetEase(Ease.OutQuad);
            _sequence.Append(canvasGroup.DOFade(endValue: 1f, duration: 1f));
            _sequence.AppendCallback(() => canvasGroup.interactable = true);
            _sequence.AppendInterval(0.5f);
            _sequence.AppendCallback(experienceSound.Play);
            _sequence.Append(DOVirtual.Float(from: 0f, to: 1f, duration: 2f + _totalEarnedExp * 0.5f, OnVirtualFloat));
            _sequence.AppendCallback(experienceSound.Stop);
        }

        private void OnVirtualFloat(float progress)
        {
            bool levelUpOnThisFrame = false;
            for (int i = 0; i < _allies.Count; i++)
            {
                (_, float startExp, float currentExp) = _allies[i];
                ExperienceScreenCharacterUI characterUI = charactersUI[i];
                levelUpOnThisFrame |= characterUI.SetProgress(startExp, currentExp, progress);
            }
            
            if (levelUpOnThisFrame)
                levelUpSound.Play();

            experienceText.text = (Mathf.Lerp(_totalEarnedExp, 0f, progress) * 100f).ToString("000");
        }
    }
}