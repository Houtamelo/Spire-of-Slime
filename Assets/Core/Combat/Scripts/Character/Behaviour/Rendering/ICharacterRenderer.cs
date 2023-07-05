using Core.Combat.Scripts.Animations;
using Core.Combat.Scripts.Skills.Action;
using Core.Utils.Patterns;
using DG.Tweening;
using UnityEngine;
using Utils.Patterns;

namespace Core.Combat.Scripts.Behaviour.Rendering
{
    public interface ICharacterRenderer
    {
        private const float FadeToDownedHalfBaseDuration = 0.25f;
        public static float FadeToDownedHalfDuration => FadeToDownedHalfBaseDuration * IActionSequence.DurationMultiplier;
        
        private const float FadeToCorpseHalfBaseDuration = 0.25f;
        public static float FadeToCorpseHalfDuration => FadeToCorpseHalfBaseDuration * IActionSequence.DurationMultiplier;
        
        private const float FadeToDeathBaseDuration = 1f;
        public static float FadeToDeathDuration => FadeToDeathBaseDuration * IActionSequence.DurationMultiplier;
        
        float IndicatorScale { get; }
        Bounds GetBounds();
        Tween FadeTween { get; }
        CombatAnimation LastAnimationSent { get; }
        void SetSortingOrder(int value);
        void AllowRendering(bool value);
        
        /// <summary> This instantly completes any ongoing fade tweens. OnComplete callbacks are called. </summary>
        Tween Fade(float endValue, float duration);
        
        void AllowIdleAnimationTimeUpdate(bool value);
        void AllowShadows(bool value);
        void SetIdleSpeed(float value);
        void SetBaseSpeed(float value);
        void SetAnimation(in CombatAnimation combatAnimation);
        Option<Transform> GetTransform();
        void DestroySelf();
        void ClearParameters();
        void SetAlpha(float alpha);
    }
}