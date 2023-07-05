using Core.Combat.Scripts.Skills.Action;
using Core.Utils.Extensions;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Main_Characters.Ethel.Combat.Skills.Challenge.Anim
{
    public class ChallengeParticleFx : MonoBehaviour
    {
        [SerializeField, Required]
        private SpriteRenderer spriteRenderer;

        [SerializeField]
        private Vector3 startScale = Vector3.one;

        [SerializeField]
        private Vector3 endScale = Vector3.one * 1.5f;

        [SerializeField]
        private float duration = 1f;
        private float GetActualDuration() => duration * IActionSequence.DurationMultiplier;

        [SerializeField]
        private float fadeDuration = 0.5f;
        private float GetActualFadeDuration() => fadeDuration * IActionSequence.DurationMultiplier;

        [SerializeField]
        private float fadeStart = 0.5f;

        [SerializeField]
        private float shakeStrength = 10f;

        [SerializeField]
        private int vibrato;

        [SerializeField]
        private float randomness = 90;

        private Sequence _sequence;
        private TweenCallback _deactivateCallback;
        private Transform _selfTransform;

        private void Start()
        {
            _deactivateCallback = Deactivate;
            _selfTransform = transform;
            Deactivate();
        }
        
        private void Deactivate() => gameObject.SetActive(false);

        [ContextMenu("Animation Test")]
        private void AnimationTest()
        {
            Animate(Vector3.zero);
        }

        public void Animate(Vector3 worldPosition)
        {
            _sequence.KillIfActive();
             
            _sequence = DOTween.Sequence();
            
            _selfTransform.localScale = startScale;
            _selfTransform.position = worldPosition;
            spriteRenderer.color = Color.white;
            gameObject.SetActive(true);

            _sequence.Append(_selfTransform.DOScale(endScale, GetActualDuration()));
            _sequence.Join(_selfTransform.DOShakePosition(GetActualDuration(), shakeStrength, vibrato, randomness, snapping: false, fadeOut: false));
            _sequence.Insert(fadeStart, spriteRenderer.DOFade(endValue: 0, GetActualFadeDuration()));
            _sequence.AppendCallback(_deactivateCallback);
        }

        public void Stop()
        {
            _sequence.KillIfActive();
            Deactivate();
        }

        private void Reset()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }
}