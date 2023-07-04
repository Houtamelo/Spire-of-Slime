using DG.Tweening;
using UnityEngine;

namespace Utils.Extensions
{
    public static class TweenExtensions
    {
        public static Tween DummyTween() => DOVirtual.DelayedCall(0.0001f, () => { });

        public static Tween DOFade(this SpriteRenderer[] renderers, float endValue, float duration)
        {
            Sequence sequence = DOTween.Sequence();
            foreach (SpriteRenderer renderer in renderers)
                sequence.Join(renderer.DOFade(endValue, duration));

            return sequence;
        }
    }
}