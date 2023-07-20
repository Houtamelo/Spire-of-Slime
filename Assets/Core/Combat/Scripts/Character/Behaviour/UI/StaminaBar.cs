using Core.Utils.Extensions;
using Core.Utils.Patterns;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Core.Combat.Scripts.Behaviour.UI
{
    public class StaminaBar : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private static float BarFillLerpDuration => DisplayModule.BarsFillLerpDuration;

        [SerializeField, Required]
        private Image staminaBar;
        
        [SerializeField, Required]
        private TMP_Text tmp;

        private Tween _tween;
        private Option<int> _cachedCurrent = Option.None;
        private Option<int> _cachedMax = Option.None;

        public void Set(bool active, int current, int max)
        {
            gameObject.SetActive(active);
            if (active == false)
            {
                _cachedCurrent = Option<int>.None;
                _cachedMax = Option<int>.None;
                return;
            }
            
            if (_cachedCurrent.TrySome(out int currentCached) && currentCached == current && _cachedMax.TrySome(out int maxCached) && maxCached == max)
                return;

            _tween.KillIfActive();
            _tween = staminaBar.DOFillAmount(endValue: (float)current / max, BarFillLerpDuration).SetEase(Ease.OutQuad);
            tmp.text = $"{current.ToString("0")}/{max.ToString("0")}";
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