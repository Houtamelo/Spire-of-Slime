using System.Collections;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils.Patterns;

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace Misc
{
    [ExecuteInEditMode]
    public class PixelPerfectWithZoom : MonoBehaviour
    {
        private const float ReferenceAspectRatio = ReferenceWidth / ReferenceHeight;
        private const float ReferenceWidth = 1920f;
        private const float ReferenceHeight = 1080f;
        private const float LowerConstant = 0.889f;
        private const float MinSize = 1f, MaxSize = 30f;

        [SerializeField]
        private float pixelsPerUnit = 100;

        [SerializeField]
        private float scale = 1;

        [SerializeField]
        private Camera cameraComponent;
        
        private bool _shouldUpdate;
        private float _currentCameraSize;
        private Tween _tween;

        private void LateUpdate()
        {
            if (_tween is { active: true })
                return;
                
            Option<float> sizeOption = CalculateDesiredCameraSize(scale, pixelsPerUnit);
            float cameraSize = sizeOption.TrySome(out cameraSize) ? cameraSize : MinSize;

            if (_currentCameraSize != cameraSize)
            {
                _currentCameraSize = cameraSize;
                SetSize(_currentCameraSize);
            }
        }

        [Button("UpdateCameraScale")]
        private void UpdateCameraScale()
        {
            Option<float> sizeOption = CalculateDesiredCameraSize(scale, pixelsPerUnit);
            if (sizeOption.IsNone)
                return;
            
            _currentCameraSize = sizeOption.Value;
            SetSize(_currentCameraSize);
        }

        private void SetSize(float value)
        {
            if (cameraComponent.orthographic)
                cameraComponent.orthographicSize = value;
            else
                cameraComponent.fieldOfView = value;
        }

        public void SetScale(float value)
        {
            scale = value;
        }

        private void OnValidate()
        {
            Option<float> sizeOption = CalculateDesiredCameraSize(scale, pixelsPerUnit);
            if (sizeOption.IsNone)
                return;

            float cameraSize = sizeOption.Value;
            if (_currentCameraSize != cameraSize)
            {
                _currentCameraSize = cameraSize;
                SetSize(_currentCameraSize);
            }
        }

        private void Reset()
        {
            cameraComponent = gameObject.GetComponent<Camera>();
        }
        
        private static Option<float> CalculateDesiredCameraSize(float desiredScale, float pixelsPerUnit)
        {
            float currentAspectRatio = (float) Screen.width / Screen.height;
            
            float size;
            if (currentAspectRatio > ReferenceAspectRatio)
                size =  ReferenceWidth * 0.5f / (desiredScale * pixelsPerUnit * ReferenceAspectRatio);
            else 
                size = ReferenceHeight * LowerConstant / (desiredScale * pixelsPerUnit * currentAspectRatio);

            if (float.IsNaN(size))
                return Option<float>.None;

            return Mathf.Clamp(size, MinSize, MaxSize);
        }

        public IEnumerator TweenZoom(float zoomMultiplier, float duration, bool useUnscaledTime = false)
            => useUnscaledTime ? AnimateZoomWithUnscaledTime(zoomMultiplier, duration) : AnimateZoomWithScaledTime(zoomMultiplier, duration);

        private IEnumerator AnimateZoomWithScaledTime(float zoomMultiplier, float duration)
        {
            float startTime = Time.time;
            float endTime = startTime + duration;
            
            float startScale = scale;
            float endScale = scale * zoomMultiplier;
            
            while (Time.time < endTime)
            {
                float percentage = (Time.time - startTime) / duration;
                float desiredScale = Mathf.Lerp(startScale, endScale, percentage);
                SetScale(desiredScale);
                yield return null;
            }
            
            SetScale(endScale);
        }
        
        private IEnumerator AnimateZoomWithUnscaledTime(float zoomMultiplier, float duration)
        {
            float startTime = Time.unscaledTime;
            float endTime = startTime + duration;
            
            float startScale = scale;
            float endScale = scale * zoomMultiplier;
            
            while (Time.unscaledTime < endTime)
            {
                float percentage = (Time.unscaledTime - startTime) / duration;
                float desiredScale = Mathf.Lerp(startScale, endScale, percentage);
                SetScale(desiredScale);
                yield return null;
            }
            
            SetScale(endScale);
        }
        
    }
}