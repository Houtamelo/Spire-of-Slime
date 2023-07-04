using System.Collections;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Utils.Extensions;
using Utils.Patterns;

namespace Core.Visual_Novel.Scripts
{
    public sealed class SceneFader : Singleton<SceneFader>
    {
        [SerializeField, Required, SceneObjectsOnly] 
        private Image fadePanel;

        private TweenCallback _onFadeOutComplete;
        private Tween _fadeTween;

        private void Start()
        {
            _onFadeOutComplete = () =>
            {
                fadePanel.gameObject.SetActive(false);
                fadePanel.color = Color.clear;
            };
        }

        public IEnumerator FadeIn(float duration)
        {
            _fadeTween?.Complete(withCallbacks: true);
            fadePanel.gameObject.SetActive(true);
            if (fadePanel.gameObject.activeInHierarchy == false)
            {
                fadePanel.SetAlpha(1f);
                return null;
            }

            if (duration <= 0)
            {
                fadePanel.SetAlpha(1f);
                return null;
            }
            
            Tween tween = _fadeTween = fadePanel.DOFade(endValue: 1f, duration);
            return new YieldableCommandWrapper(_fadeTween.WaitForCompletion().AsEnumerator(), allowImmediateFinish: true, onImmediateFinish: tween.CompleteIfActive);
        }

        public void FadeInAsync(float duration)
        {
            _fadeTween?.Complete(withCallbacks: true);
            fadePanel.gameObject.SetActive(true);

            if (duration <= 0)
                fadePanel.SetAlpha(1f);
            else
                _fadeTween = fadePanel.DOFade(endValue: 1f, duration).SetUpdate(isIndependentUpdate: false);
        }

        public IEnumerator FadeOut(float duration)
        {
            _fadeTween?.Complete(withCallbacks: true);
            if (fadePanel.gameObject.activeSelf == false || duration <= 0f)
            {
                _onFadeOutComplete();
                return null;
            }
            
            Tween tween = _fadeTween = fadePanel.DOFade(endValue: 0f, duration).SetUpdate(isIndependentUpdate: false);
            _fadeTween.onComplete += _onFadeOutComplete;
            return new YieldableCommandWrapper(_fadeTween.WaitForCompletion().AsEnumerator(), allowImmediateFinish: true, onImmediateFinish: tween.CompleteIfActive);
        }

        public void FadeOutAsync(float duration)
        {
            _fadeTween?.Complete(withCallbacks: true);
            if (fadePanel.gameObject.activeSelf == false || duration <= 0f)
            {
                _onFadeOutComplete();
                return;
            }
            
            _fadeTween = fadePanel.DOFade(endValue: 0f, duration).SetUpdate(isIndependentUpdate: false);
            _fadeTween.onComplete += _onFadeOutComplete;
        }
    }
}