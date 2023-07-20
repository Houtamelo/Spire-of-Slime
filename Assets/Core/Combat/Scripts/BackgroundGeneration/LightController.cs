using Core.Utils.Extensions;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Core.Combat.Scripts.BackgroundGeneration
{
    public class LightController : MonoBehaviour
    {
        [SerializeField, Required]
        private Light2D light2d;

        [SerializeField]
        private float normalIntensity, skillAnimationIntensity;
        
        private Tween _tween;
        private TweenCallback<float> _onTweenUpdate;

        private void Awake()
        {
            _onTweenUpdate = intensity => light2d.intensity = intensity;
        }

        public void SwitchToSkillAnimation(float duration)
        {
            _tween.KillIfActive();
            _tween = DOVirtual.Float(from: light2d.intensity, to: skillAnimationIntensity, duration, _onTweenUpdate);
        }

        public void SwitchToNormal(float duration)
        {
            _tween.KillIfActive();
            _tween = DOVirtual.Float(from: light2d.intensity, to: normalIntensity, duration, _onTweenUpdate);
        }

#if UNITY_EDITOR
        private void Reset()
        {
            light2d = GetComponent<Light2D>();
            if (light2d != null)
                normalIntensity = light2d.intensity;
        }

        private void OnValidate()
        {
            if (light2d != null)
                return;

            light2d = GetComponent<Light2D>();
            if (light2d == null)
            {
                Debug.LogWarning(message: $"Light2D is null on {name}", context: this);
            }
            else
            {
                normalIntensity = light2d.intensity;
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif
    }
}