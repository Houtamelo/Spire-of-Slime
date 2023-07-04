using DG.Tweening;
using UnityEngine;
using Utils.Extensions;

namespace UI
{
    public class ScaleLerpOnEnable : MonoBehaviour
    {
        [SerializeField]
        private Vector3 startScale = Vector3.zero;
        private Vector3 _endScale = Vector3.one;
        
        [SerializeField] 
        private float duration = 1f;

        private Tween _scaleTween;

        private void Awake()
        {
            _endScale = transform.localScale;
        }

        private void OnEnable()
        {
            transform.localScale = startScale;
            _scaleTween = transform.DOScale(_endScale, duration);
        }

        private void OnDisable()
        {
            _scaleTween.KillIfActive();
        }
    }
}