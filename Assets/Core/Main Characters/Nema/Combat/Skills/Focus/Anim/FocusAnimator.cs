using Core.Combat.Scripts.Skills.Action;
using DG.Tweening;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils.Extensions;

namespace Core.Main_Characters.Nema.Combat.Skills.Focus.Anim
{
    public class FocusAnimator : MonoBehaviour
    {
        private static readonly int Middle = Shader.PropertyToID("_Middle");
        private static readonly int Min = Shader.PropertyToID("_Min");
        private static readonly int Pow = Shader.PropertyToID("_Pow");

        [SerializeField, Required]
        private SpriteRenderer spriteRenderer;

        [SerializeField]
        private float duration = 2f;
        private float GetActualDuration() => duration * IActionSequence.DurationMultiplier;

        [SerializeField]
        private float middleStart = -0.3f, middleEnd = 2f;

        [SerializeField]
        private float minStart = 0.2f, minEnd = 0f;
        
        [SerializeField]
        private float powStart = 0.5f, powEnd = 0.75f;
        
        [SerializeField, Required]
        private AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        private Tween _tween;
        private TweenCallback<float> _onProgress;

        [UsedImplicitly]
        private void AnimateFocus()
        {
            _tween.KillIfActive();

            spriteRenderer.material.SetFloat(Middle, middleStart);
            spriteRenderer.material.SetFloat(Min,    minStart);
            spriteRenderer.material.SetFloat(Pow,    powStart);

            _tween = DOVirtual.Float(from: 0f, to: 1f, GetActualDuration(), _onProgress).SetEase(curve);
        }

        private void OnProgress(float progress)
        {
            spriteRenderer.material.SetFloat(Middle, Mathf.Lerp(middleStart, middleEnd, progress));
            spriteRenderer.material.SetFloat(Min,    Mathf.Lerp(minStart,    minEnd,    progress));
            spriteRenderer.material.SetFloat(Pow,    Mathf.Lerp(powStart,    powEnd,    progress));
        }

        private void Awake()
        {
            _onProgress = OnProgress;
        }

        private void OnDisable()
        {
            _tween.KillIfActive();
        }
    }
}
