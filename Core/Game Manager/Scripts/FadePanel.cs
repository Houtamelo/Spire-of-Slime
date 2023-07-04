using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Utils.Extensions;

namespace Core.Game_Manager.Scripts
{
    public class FadePanel : MonoBehaviour
    {
        private const float FadeDuration = 2f;
        
        [SerializeField, SceneObjectsOnly, Required]
        private Image fadePanel;
        
        private Tween _fadeInTween, _fadeOutTween;
        private TweenCallback _onFadeOutComplete;
        
        private void Start()
        {
            _onFadeOutComplete = OnFadeOutComplete;
        }

        private void OnFadeOutComplete() => fadePanel.gameObject.SetActive(false);
        
        public Tween FadeUp()
        {
            _fadeOutTween.KillIfActive();
            if (_fadeInTween is { active: true })
                return _fadeInTween;

            if (fadePanel.gameObject.activeSelf && fadePanel.color.a >= 1f)
            {
                fadePanel.raycastTarget = true;
                return fadePanel.DOColor(Color.black, 0.1f).SetUpdate(isIndependentUpdate: true); // redundant tween
            }
            
            fadePanel.gameObject.SetActive(true);
            fadePanel.raycastTarget = true;
            _fadeInTween = fadePanel.DOColor(endValue: Color.black, duration: FadeDuration).SetUpdate(isIndependentUpdate: true);
            return _fadeInTween;
        }

        public Tween FadeDown()
        {
            _fadeInTween.KillIfActive();
            if (_fadeOutTween is { active: true })
                return _fadeOutTween;

            if (fadePanel.gameObject.activeSelf == false || fadePanel.color.a <= 0f)
            {
                fadePanel.gameObject.SetActive(false);
                fadePanel.raycastTarget = false;
                return fadePanel.DOColor(Color.clear, duration: 0.1f).SetUpdate(isIndependentUpdate: true); // redundant tween
            }
            
            fadePanel.gameObject.SetActive(true);
            _fadeOutTween = fadePanel.DOColor(endValue: Color.clear, duration: FadeDuration).SetUpdate(UpdateType.Manual).SetEase(Ease.InCubic);
            _fadeOutTween.onComplete += _onFadeOutComplete;
            return _fadeOutTween;
        }

        private void Update()
        {
            if (_fadeOutTween is { active: true })
                _fadeOutTween.ManualUpdate(Time.deltaTime, Time.unscaledDeltaTime);
        }
    }
}