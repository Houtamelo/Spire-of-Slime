using Core.Combat.Scripts.Behaviour.Modules;
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
    public class LustBar : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private static float BarFillLerpDuration => DisplayModule.BarsFillLerpDuration;

        [SerializeField, Required]
        private Image lowLustBar, highLustBar;
        
        [SerializeField, Required]
        private TMP_Text lustText;
        
        private Sequence _sequence;
        private Option<int> _cachedLust = Option<int>.None;
        
        public void Set(bool active, int lust)
        {
            gameObject.SetActive(active);
            if (active == false)
            {
                _cachedLust = Option<int>.None;
                return;
            }
            
            if (_cachedLust.TrySome(out int cachedLust) && cachedLust == lust)
                return;

            _sequence.KillIfActive();
            _cachedLust = Option<int>.Some(lust);
            lustText.text = $"{lust.ToString("0")} / {ILustModule.MaxLust.ToString("0")}";
            
            _sequence = DOTween.Sequence().SetEase(Ease.OutQuad);

            float lowFill = GetLowFill(lust);
            _sequence.Append(lowLustBar.DOFillAmount(lowFill, BarFillLerpDuration));
            
            float highFill = GetHighFill(lust);
            _sequence.Join(highLustBar.DOFillAmount(highFill, BarFillLerpDuration));
        }
        
        private static float GetLowFill(int lust) => lust <= 100 ? lust / 100f : 1f;
        private static float GetHighFill(int lust) => lust <= 100 ? 0f : (lust - 100f) / 100f;

        public void OnPointerEnter(PointerEventData eventData)
        {
            lustText.gameObject.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            lustText.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _sequence.KillIfActive();
        }
    }
}