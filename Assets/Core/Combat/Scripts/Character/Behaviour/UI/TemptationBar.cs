using System.Text;
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
    public class TemptationBar : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private static float BarFillLerpDuration => DisplayModule.BarsFillLerpDuration;
        private static readonly StringBuilder Builder = new();

        [SerializeField, Required]
        private Image bar;
        
        [SerializeField, Required]
        private TMP_Text tmp;
        
        private Tween _tween;
        private Option<int> _cachedTemptation = Option.None;
        
        public void Set(bool active, int temptation)
        {
            gameObject.SetActive(active);
            if (active == false)
            {
                _cachedTemptation = Option.None;
                return;
            }
            
            if (_cachedTemptation.TrySome(out int cached) && cached == temptation)
                return;

            _tween.KillIfActive();
            _cachedTemptation = Option<int>.Some(temptation);
            tmp.text = Builder.Override(temptation.ToString(), "/100").ToString();

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