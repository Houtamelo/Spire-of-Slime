using System.Collections.Generic;
using Animation;
using DG.Tweening;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils.Patterns;

// ReSharper disable HeuristicUnreachableCode
#pragma warning disable CS0162

namespace Core.World_Map.Scripts
{
    public sealed class WorldTooltip : Singleton<WorldTooltip>
    {
        [OdinSerialize, Required, SceneObjectsOnly] private readonly TMP_Text _titleTMP, _descriptionTMP;
        [OdinSerialize] private readonly IReadOnlyCollection<Graphic> _graphicsToLerp;
        [OdinSerialize] private readonly Vector2 _offset;
        [OdinSerialize, Required, SceneObjectsOnly] private readonly CameraAnimator _cameraAnimator;
        
        private RectTransform _selfRect;
        private Sequence _currentSequence;
        private Transform _currentButton;

        protected override void Awake()
        {
            base.Awake();
            _selfRect = (RectTransform) transform;
        }

        public void DisplayTooltip(LocationEnum location, Transform locationButton)
        {
            return; // disabled until improved
            
            if (_currentSequence is { active: true })
                _currentSequence.Kill();
            
            _currentButton = locationButton;
            _currentSequence = DOTween.Sequence();
            _currentSequence.Append(t: DOVirtual.Float(from: _titleTMP.color.a, to: 0, duration: 0.5f, onVirtualUpdate: LerpAlpha).SetSpeedBased());
            _currentSequence.AppendCallback(callback: () =>
            {
                _titleTMP.text = location.FormattedName();
                _descriptionTMP.text = location.Description();
                Vector3 transformPosition = locationButton.transform.position;
                transformPosition.z = 0;
                _selfRect.anchoredPosition = (Vector2) _cameraAnimator.selfCamera.WorldToScreenPoint(position: transformPosition) + _offset;
            });
            _currentSequence.Append(t: DOVirtual.Float(from: _titleTMP.color.a, to: 1, duration: 0.5f, onVirtualUpdate: LerpAlpha).SetSpeedBased());
        }

        public void StopTooltiping(Transform locationButton)
        {
            return; // disabled until improved
            if (locationButton != _currentButton)
                return;

            _currentButton = null;
            
            if (_currentSequence is { active: true })
                _currentSequence.Kill();
            
            _currentSequence = DOTween.Sequence();
            _currentSequence.Append(t: DOVirtual.Float(from: _titleTMP.color.a, to: 0, duration: 0.5f, onVirtualUpdate: LerpAlpha).SetSpeedBased());
        }

        private void LerpAlpha(float value)
        {
            foreach (Graphic graphic in _graphicsToLerp)
            {
                Color color = graphic.color;
                color.a = value;
                graphic.color = color;
            }
        }
    }
}