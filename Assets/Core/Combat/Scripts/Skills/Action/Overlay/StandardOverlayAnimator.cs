using Core.Combat.Scripts.Managers;
using Core.Utils.Extensions;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Combat.Scripts.Skills.Action.Overlay
{
    public class StandardOverlayAnimator : OverlayAnimator
    {
        [SerializeField, Required]
        private SpriteRenderer spriteRenderer;
        
        private Tween _tween;

        private void OnDestroy() => _tween.KillIfActive();

        public override Sequence Announce(Announcer announcer, PlannedSkill plan, float startDuration, float popDuration, float speed) => throw new System.NotImplementedException();

        public override void FadeUp(float duration, PlannedSkill plan)
        {
            _tween.KillIfActive();
            spriteRenderer.SetAlpha(0f);
            _tween = spriteRenderer.DOFade(endValue: 1f, duration);
        }

        public override void FadeDown(float duration)
        {
            _tween.KillIfActive();
            _tween = spriteRenderer.DOFade(endValue: 0f, duration);
        }
    }
}