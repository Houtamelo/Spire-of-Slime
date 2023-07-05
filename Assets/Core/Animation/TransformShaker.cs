using DG.Tweening;
using JetBrains.Annotations;
using UnityEngine;

namespace Core.Animation
{
    public class TransformShaker : MonoBehaviour
    {
        [SerializeField] private Transform targetTransform;
        [SerializeField] private float duration;
        [SerializeField] private float strength;
        [SerializeField] private int vibrato;
        [SerializeField] private float randomness;
        [SerializeField] private bool snapping;
        [SerializeField] private bool fadeOut;

        private Tween _tween;
        
        [UsedImplicitly]
        public void Shake()
        {
            if (_tween is { active: true })
                _tween.Kill();
            
            _tween = targetTransform.DOShakePosition(duration, strength, vibrato, randomness, snapping, fadeOut);
        }
    }
}