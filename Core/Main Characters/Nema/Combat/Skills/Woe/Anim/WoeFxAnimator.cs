using Core.Combat.Scripts.Skills.Action;
using DG.Tweening;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils.Extensions;

namespace Core.Main_Characters.Nema.Combat.Skills.Woe.Anim
{
    public class WoeFxAnimator : MonoBehaviour
    {
        [SerializeField, Required]
        private SpriteRenderer woeFx;

        [SerializeField]
        private float maskXOneDuration;
        private float GetActualMaskXOneDuration() => maskXOneDuration * IActionSequence.DurationMultiplier;

        [SerializeField]
        private float interval;
        private float GetActualInterval() => interval * IActionSequence.DurationMultiplier;

        [SerializeField]
        private float maskXZeroDuration;
        private float GetActualMaskXZeroDuration() => maskXZeroDuration * IActionSequence.DurationMultiplier;

        [SerializeField]
        private float fadeOutDuration;
        private float GetActualFadeOutDuration() => fadeOutDuration * IActionSequence.DurationMultiplier;
        
        private Sequence _sequence;
        private TweenCallback<float> _maskBeginCallback;
        private TweenCallback<float> _maskEndCallback;
        private TweenCallback _deactivate;

        private static readonly int MaskXBegin = Shader.PropertyToID("_mask_x_begin");
        private static readonly int MaskXEnd = Shader.PropertyToID("_mask_x_end");

        private void Awake()
        {
            _maskBeginCallback = SetMaskBegin;
            _maskEndCallback = SetMaskEnd;
            _deactivate = Deactivate;
        }

        [UsedImplicitly]
        public void AnimateWoe()
        {
            _sequence.KillIfActive();
            _sequence = DOTween.Sequence();
            
            woeFx.gameObject.SetActive(true);
            woeFx.material.SetFloat(MaskXEnd, 0.85f);
            woeFx.material.SetFloat(MaskXBegin, 0.85f);

            Color color = woeFx.color;
            color.a = 1;
            woeFx.color = color;

            _sequence.Append(DOVirtual.Float(from: 0.85f, to: 0.45f, GetActualMaskXOneDuration(), _maskEndCallback));
            _sequence.AppendInterval(GetActualInterval());
            _sequence.Append(DOVirtual.Float(from: 0.85f, to: 0.45f, GetActualMaskXZeroDuration(), _maskBeginCallback));
            _sequence.Append(woeFx.DOFade(endValue: 0f, GetActualFadeOutDuration()));
            _sequence.AppendCallback(_deactivate);
        }
        
        private void SetMaskBegin(float value) => woeFx.material.SetFloat(MaskXBegin, value);
        private void SetMaskEnd(float value) => woeFx.material.SetFloat(MaskXEnd, value);
        private void Deactivate() => woeFx.gameObject.SetActive(false);
    }
}