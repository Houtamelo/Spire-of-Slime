using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Utils.Extensions;

namespace Core.Shaders
{
    public class LighterAnimator : MonoBehaviour
    {
        private static readonly int ColorToLerp = Shader.PropertyToID("_ColorToLerp");
        private static readonly int LerpAmount = Shader.PropertyToID("_LerpAmount");

        [SerializeField, Required]
        private Graphic graphic;
        
        private Tween _tween;
        private TweenCallback _onComplete;

        private void Awake()
        {
            _onComplete = () => graphic.material.SetFloat(LerpAmount, 0f);
        }

        private void OnDestroy()
        {
            _tween.KillIfActive();
        }
        
        public void ResetMaterial()
        {
            if (graphic.material == null)
            {
                Debug.LogWarning("No material found on graphic", context: this);
                return;
            }
            
            graphic.material.SetFloat(LerpAmount, 0f);
        }

        public Tween Animate(float amplitude, int fullLoopCount, Color color)
        {
            if (gameObject.activeInHierarchy == false)
            {
                Debug.LogWarning("Cannot animate inactive object", context: this);
                return Utils.Extensions.TweenExtensions.DummyTween();
            }
            
            if (graphic.material == null)
            {
                Debug.LogWarning("No material found on graphic", context: this);
                return Utils.Extensions.TweenExtensions.DummyTween();
            }

            fullLoopCount *= 2;
            
            _tween.KillIfActive();
            
            graphic.material.SetColor(ColorToLerp, color);
            graphic.material.SetFloat(LerpAmount, 0f);
            _tween = graphic.material.DOFloat(endValue: 1f, propertyID: LerpAmount, duration: amplitude).SetLoops(fullLoopCount, LoopType.Yoyo);
            _tween.onComplete += _onComplete;
            return _tween;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (graphic == null)
                return;
            
            if (graphic.material == null)
                Debug.LogWarning("No material found on graphic", context: this);
        }

        private void Reset()
        {
            graphic = GetComponent<Graphic>();
        }
        
        [Button]
        private void DebugAnimation()
        {
            Animate(1f, 5, Color.white);
        }
        
#endif
    }
}