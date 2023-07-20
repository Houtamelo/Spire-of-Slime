using System;
using Core.Combat.Scripts.Interfaces;
using Core.Save_Management;
using Core.Utils.Extensions;
using Core.Utils.Math;
using Core.Utils.Patterns;
using DG.Tweening;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Combat.Scripts.UI
{
    public class ExperienceScreenCharacterUI : MonoBehaviour
    {
        [SerializeField, Required]
        private GameObject portraitGameObject;

        [SerializeField, Required]
        private Image portrait, background;

        [SerializeField, Required]
        private Slider slider;

        [SerializeField, Required]
        private TMP_Text levelUpTmp;

        private Tween _levelUpTween;

        private void OnDestroy()
        {
            _levelUpTween.KillIfActive();
        }

        public void SetCharacter([NotNull] ICharacterScript script, int startExp)
        {
            gameObject.SetActive(true);
            _levelUpTween.KillIfActive();
            levelUpTmp.gameObject.SetActive(false);
            Option<Sprite> portraitSprite = script.GetPortrait;
            if (portraitSprite.IsSome)
            {
                portrait.sprite = portraitSprite.Value;
                portraitGameObject.SetActive(true);
            }
            else
            {
                portraitGameObject.SetActive(false);
            }

            Option<Color> backgroundColor = script.GetPortraitBackgroundColor;
            background.color = backgroundColor.IsSome ? backgroundColor.Value : Color.black;
            
            double percentage = ExperienceCalculator.GetExperiencePercentage(startExp);
            slider.value = (float) percentage;
        }
        
        /// <returns> If Character leveled up </returns>
        public bool SetProgress(int startExp, int currentExp, float progress)
        {
            float oldPercentage = slider.value;
            double newPercentage = ExperienceCalculator.GetExperiencePercentage(Mathf.Lerp(startExp, currentExp, progress).FloorToInt());
            slider.value = (float)newPercentage;
            if (newPercentage >= oldPercentage || oldPercentage is >= 1f or < 0f || levelUpTmp.gameObject.activeSelf)
                return false;
            
            _levelUpTween.KillIfActive();
            levelUpTmp.gameObject.SetActive(true);
            levelUpTmp.transform.localScale = new Vector3(1f, 0f, 1f);
            _levelUpTween = levelUpTmp.transform.DOScaleY(endValue: 1f, duration: 0.5f).SetEase(Ease.OutBounce);
            return true;

        }
    }
}