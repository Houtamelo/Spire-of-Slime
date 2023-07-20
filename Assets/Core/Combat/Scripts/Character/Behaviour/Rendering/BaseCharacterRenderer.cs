using Core.Combat.Scripts.Animations;
using Core.Utils.Extensions;
using Core.Utils.Patterns;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Combat.Scripts.Behaviour.Rendering
{
    public class BaseCharacterRenderer : MonoBehaviour, ICharacterRenderer
    {
        [SerializeField, Required]
        private SpriteRenderer spriteRenderer;

        [SerializeField, Required]
        private GameObject shadow;
                           
        [SerializeField, Required]
        private SpriteRenderer shadowRenderer;
        
        [SerializeField, Required]
        private BaseCharacterAnimatorController animatorController;

        [SerializeField]
        private float indicatorScale = 1f;
        public float IndicatorScale => indicatorScale;

        public Tween FadeTween { get; private set; }
        public CombatAnimation LastAnimationSent { get; private set; }

        private Option<float> _shadowDefaultAlpha;

        private float GetShadowDefaultAlpha
        {
            get
            {
                if (_shadowDefaultAlpha.IsNone)
                    _shadowDefaultAlpha = Option<float>.Some(shadowRenderer.color.a);
                
                return _shadowDefaultAlpha.Value;
            }
        }

        public Bounds GetBounds() => spriteRenderer.bounds;
        public Option<Transform> GetTransform() => Option<Transform>.Some(transform);
        
        private void OnDestroy()
        {
            FadeTween.KillIfActive();
            spriteRenderer.DOKill();
        }
        
        public void SetSortingOrder(int value)
        {
            spriteRenderer.sortingOrder = value;
            shadowRenderer.sortingOrder = value - 10;
        }

        public void DestroySelf() => Destroy(gameObject);

        public void ClearParameters() => animatorController.ClearParameters();

        public void AllowRendering(bool value) => animatorController.gameObject.SetActive(value);

        public Tween Fade(float endValue, float duration)
        {
            FadeTween.CompleteIfActive();
            if (shadow.activeInHierarchy == false)
            {
                shadowRenderer.SetAlpha(endValue * GetShadowDefaultAlpha);
                FadeTween = spriteRenderer.DOFade(endValue, duration);
                return FadeTween;
            }
            
            Sequence sequence = DOTween.Sequence();
            sequence.Append(spriteRenderer.DOFade(endValue, duration));
            sequence.Join(shadowRenderer.DOFade(endValue * GetShadowDefaultAlpha, duration));
            
            FadeTween = sequence;
            return sequence;
        }

        public void AllowIdleAnimationTimeUpdate(bool value) => animatorController.AllowIdleAnimationTimeUpdate(value);

        public void AllowShadows(bool value) => shadow.gameObject.SetActive(value);

        public void SetIdleSpeed(float value) => animatorController.SetIdleSpeed(value);
        
        public void SetBaseSpeed(float value) => animatorController.SetBaseSpeed(value);

        public void SetAnimation(in CombatAnimation combatAnimation)
        {
            LastAnimationSent = combatAnimation;
            animatorController.SetAnimation(combatAnimation);
        }

        public void SetAlpha(float value)
        {
            FadeTween.CompleteIfActive();
            spriteRenderer.SetAlpha(value);
            shadowRenderer.SetAlpha(value * GetShadowDefaultAlpha);
        }
    }
}