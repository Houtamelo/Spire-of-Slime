using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts.Cues;
using Core.Combat.Scripts.Effects;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Utils.Patterns;
using DG.Tweening;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Core.Combat.Scripts.Behaviour.UI
{
    public class StatusEffectIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private const float FadeDuration = 1f;
        
        [SerializeField, Required]
        private Image icon;
        
        private readonly List<StatusInstance> _statusInstances = new();
        private readonly StringBuilder _tooltipBuilder = new();
        
        private Option<CharacterStateMachine> _owner;
        private Option<DisplayModule> _display;
        private bool _isMouseOver;

        public Option<EffectType> GetEffectType()
        {
            RemoveDeactivatedStatuses();
            return _statusInstances.Count == 0 ? Option.None : _statusInstances[0].EffectType;
        }

        private Option<string> GetDescription()
        {
            RemoveDeactivatedStatuses();
            _tooltipBuilder.Clear();
            foreach (StatusInstance statusInstance in _statusInstances)
            {
                if (statusInstance.GetDescription().TrySome(out string description))
                    _tooltipBuilder.AppendLine(description);
            }

            return _tooltipBuilder.Length == 0 ? Option<string>.None : _tooltipBuilder.ToString();
        }

        private void Update()
        {
            if (_isMouseOver == false || _owner.IsNone)
                return;

            if (_display.TrySome(out DisplayModule display) && GetDescription().TrySome(out string description))
                display.ShowStatusTooltip(description);
        }

        public void AssignCharacter(CharacterStateMachine owner, DisplayModule display)
        {
            _owner = owner;
            _display = display != null ? Option<DisplayModule>.Some(display) : Option<DisplayModule>.None;
        }

        public void AddStatus([NotNull] StatusInstance statusInstance)
        {
            Option<EffectType> currentType = GetEffectType();
            if (currentType.IsSome)
            {
                if (currentType.Value != statusInstance.EffectType)
                    Debug.LogWarning($"Check current type before adding a new status.\nCurrent:{currentType.Value}. New:{statusInstance.EffectType}");

                _statusInstances.Add(statusInstance);
                EvaluateStatuses();
                return;
            }

            _statusInstances.Add(statusInstance);
            Option<Sprite> sprite = StatusEffectsDatabase.GetStatusIcon(statusInstance.EffectType);
            if (sprite.IsSome)
            {
                icon.sprite = sprite.Value;
                gameObject.SetActive(true);
            }
            else
            {
                icon.sprite = null;
                gameObject.SetActive(false);
            }
        }

        public void RemoveStatus(StatusInstance effectInstance)
        {
            if (_statusInstances.Remove(effectInstance))
                EvaluateStatuses();
        }

        private void EvaluateStatuses()
        {
            RemoveDeactivatedStatuses();
            if (_statusInstances.Count == 0)
            {
                icon.sprite = null;
                gameObject.SetActive(false);
            }
        }

        private void RemoveDeactivatedStatuses()
        {
            for (int i = 0; i < _statusInstances.Count; i++)
            {
                if (_statusInstances[i].IsDeactivated)
                {
                    _statusInstances.RemoveAt(i);
                    i--;
                }
            }
        }

        public void ClearStatuses()
        {
            _statusInstances.Clear();
            icon.sprite = null;
            gameObject.SetActive(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isMouseOver = true;
            if (_display.TrySome(out DisplayModule display) && GetDescription().TrySome(out string description))
                display.ShowStatusTooltip(description);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isMouseOver = false;
            if (_owner.IsSome)
                _owner.Value.StatusReceiverModule.HideStatusTooltip();
        }

        private void OnEnable()
        {
            icon.color = new Color(1f, 1f, 1f, 0f);
            icon.DOFade(endValue: 1f, FadeDuration);
        }

        private void OnDisable()
        {
            icon.DOKill();

            if (_isMouseOver && _owner.IsSome)
                _owner.Value.StatusReceiverModule.HideStatusTooltip();
        }
    }
}