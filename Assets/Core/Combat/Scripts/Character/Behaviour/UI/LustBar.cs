using Core.Combat.Scripts.Interfaces.Modules;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utils.Extensions;
using Utils.Patterns;

namespace Core.Combat.Scripts.Behaviour.UI
{
    public class LustBar : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private static float BarFillLerpDuration => CharacterDisplay.BarsFillLerpDuration;

        [SerializeField, Required]
        private Image lowLustBar, highLustBar;
        
        [SerializeField, Required]
        private TMP_Text lustText;
        
        private Sequence _sequence;
        private Option<uint> _cachedLust = Option<uint>.None;
        
        public void Set(bool active, uint lust)
        {
            gameObject.SetActive(active);
            if (active == false)
            {
                _cachedLust = Option<uint>.None;
                return;
            }
            
            if (_cachedLust.TrySome(out uint cachedLust) && cachedLust == lust)
                return;

            _sequence.KillIfActive();
            _cachedLust = Option<uint>.Some(lust);
            lustText.text = $"{lust.ToString("0")} / {ILustModule.MaxLust.ToString("0")}";
            
            _sequence = DOTween.Sequence().SetEase(Ease.OutQuad);

            float lowFill = GetLowFill(lust);
            _sequence.Append(lowLustBar.DOFillAmount(lowFill, BarFillLerpDuration));
            
            float highFill = GetHighFill(lust);
            _sequence.Join(highLustBar.DOFillAmount(highFill, BarFillLerpDuration));
        }
        
        private static float GetLowFill(uint lust) => lust <= 100 ? lust / 100f : 1f;
        private static float GetHighFill(uint lust) => lust <= 100 ? 0f : (lust - 100f) / 100f;

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