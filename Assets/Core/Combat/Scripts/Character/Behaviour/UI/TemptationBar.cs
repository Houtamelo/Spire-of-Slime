using Core.Utils.Extensions;
using Core.Utils.Patterns;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utils.Patterns;

namespace Core.Combat.Scripts.Behaviour.UI
{
    public class TemptationBar : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private static float BarFillLerpDuration => CharacterDisplay.BarsFillLerpDuration;

        [SerializeField, Required]
        private Image bar;
        
        [SerializeField, Required]
        private TMP_Text tmp;
        
        private Tween _tween;
        private Option<ClampedPercentage> _cachedTemptation = Option.None;
        
        public void Set(bool active, ClampedPercentage temptation)
        {
            gameObject.SetActive(active);
            if (active == false)
            {
                _cachedTemptation = Option.None;
                return;
            }
            
            if (_cachedTemptation.TrySome(out ClampedPercentage cached) && cached == temptation)
                return;

            _tween.KillIfActive();
            _cachedTemptation = Option<ClampedPercentage>.Some(temptation);
            tmp.text = temptation.ToString();

            _tween = bar.DOFillAmount(endValue: temptation, duration: BarFillLerpDuration);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            tmp.gameObject.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            tmp.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _tween.KillIfActive();
        }
    }
}