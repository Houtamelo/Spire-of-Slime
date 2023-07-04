using Core.Combat.Scripts.Interfaces;
using DG.Tweening;
using Save_Management;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils.Extensions;
using Utils.Patterns;

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

        public void SetCharacter(ICharacterScript script, float startExp)
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
            
            float percentage = ExperienceCalculator.GetExperiencePercentage(startExp);
            slider.value = percentage;
        }
        
        /// <returns> If Character leveled up </returns>
        public bool SetProgress(float startExp, float currentExp, float progress)
        {
            float oldPercentage = slider.value;
            float newPercentage = ExperienceCalculator.GetExperiencePercentage(Mathf.Lerp(startExp, currentExp, progress));
            slider.value = newPercentage;
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