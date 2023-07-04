using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using Utils.Extensions;
using Utils.Handlers;
using Utils.Patterns;

namespace Core.Visual_Novel.Scripts
{
    public class DialogueUIManager : Singleton<DialogueUIManager>
    {
        public const float FadeUIDefaultDuration = 0.5f;
        
        [SerializeField]
        private CanvasGroup canvasGroup;
        
        [NonSerialized]
        public readonly ValueHandler<bool> HideUIHandler = new();
        
        private Tween _fadeUITween;

        private void Start()
        {
            HideUIHandler.Changed += HideUIToggled;
        }
        protected override void OnDestroy()
        {
            HideUIHandler.Changed -= HideUIToggled;
            base.OnDestroy();
        }

        private void HideUIToggled(bool value)
        {
            _fadeUITween.KillIfActive();
            
            if (value)
            {
                canvasGroup.alpha = 0;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;   
            }
            else
            {
                canvasGroup.alpha = 1;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }

        public void SetUIAsync(bool active, float duration)
        {
            _fadeUITween.KillIfActive();
            
            if (active)
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;

                if (duration <= 0f)
                    canvasGroup.alpha = 1f;
                else
                    _fadeUITween = canvasGroup.DOFade(endValue: 1f, duration);
            }
            else
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                if (duration <= 0f)
                    canvasGroup.alpha = 0f;
                else
                    _fadeUITween = canvasGroup.DOFade(endValue: 0f, duration);
            }
        }
        
        public IEnumerator SetUI(bool active, float duration)
        {
            _fadeUITween.KillIfActive();
            
            if (active)
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
                if (duration <= 0f)
                    canvasGroup.alpha = 1f;
                else
                {
                    Tween tween = _fadeUITween = canvasGroup.DOFade(endValue: 1f, duration);
                    return new YieldableCommandWrapper(_fadeUITween.WaitForCompletion().AsEnumerator(), allowImmediateFinish: true, onImmediateFinish: tween.CompleteIfActive);
                }
            }
            else
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                if (duration <= 0f)
                    canvasGroup.alpha = 0f;
                else
                {
                    Tween tween = _fadeUITween = canvasGroup.DOFade(endValue: 0f, duration);
                    return new YieldableCommandWrapper(_fadeUITween.WaitForCompletion().AsEnumerator(), allowImmediateFinish: true, onImmediateFinish: tween.CompleteIfActive);
                }
            }
            
            return null;
        }
    }
}