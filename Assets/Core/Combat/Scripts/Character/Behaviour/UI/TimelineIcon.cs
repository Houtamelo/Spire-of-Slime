using Core.Combat.Scripts.Managers;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utils.Extensions;
using Utils.Patterns;

namespace Core.Combat.Scripts.Behaviour.UI
{
    public class TimelineIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private const float TimeAmplitude = 6f;

        [SerializeField, Required]
        private Image icon;

        [SerializeField, Required]
        private TMP_Text tmp;

        [SerializeField, Required]
        private RectTransform selfRect;

        private Tween _tween;
        
        private Option<RectTransform> _parentRect;
        private CharacterDisplay _owner;

        public void AssignParent(Transform iconsParent)
        {
            RectTransform rectTransform = (RectTransform)iconsParent;
            _parentRect = rectTransform != null ? rectTransform : Option<RectTransform>.None;
        }
        
        public void SetSprite(Sprite sprite)
        {
            icon.sprite = sprite;
        }

        public void SetLabel(string text, Color color)
        {
            tmp.text = text;
            tmp.color = color;
        }

        public void SetTime(float time, CombatManager combatManager)
        {
            _tween.CompleteIfActive();
            float timeUntilNextStep = CombatManager.TimePerStep - combatManager.AccumulatedStepTime;
            float speed = combatManager.SpeedHandler.Value;
            Vector2 targetAnchoredPosition = new(CalculatePosition(time), 0f);
            if (combatManager.PauseHandler.Value || timeUntilNextStep <= 0f || speed <= 0f)
            {
                selfRect.anchoredPosition = targetAnchoredPosition;
                return;
            }

            timeUntilNextStep /= speed;
            _tween = selfRect.DOAnchorPos(targetAnchoredPosition, timeUntilNextStep);
        }
        private void OnDisable()
        {
            _tween.KillIfActive();
        }

        private float CalculatePosition(float time)
        {
            if (_parentRect.IsNone)
            {
                Debug.LogWarning("Parent rect is not assigned.");
                return 0;
            }

            float percentage = Mathf.Min(time / TimeAmplitude, TimeAmplitude);
            percentage = Mathf.Clamp(percentage, 0f, 1f);
            float desiredX = _parentRect.Value.rect.width * percentage;
            return desiredX;
        }
        private void Reset()
        {
            selfRect = (RectTransform)transform;
            icon = GetComponentInChildren<Image>();
            tmp = GetComponentInChildren<TMP_Text>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            tmp.enabled = true;
            if (_owner != null && _owner.StateMachine.AssertSome(out CharacterStateMachine stateMachine))
                _owner.CombatManager.InputHandler.CharacterPointerEnter(stateMachine);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            tmp.enabled = false;
            if (_owner != null && _owner.StateMachine.TrySome(out CharacterStateMachine stateMachine))
                _owner.CombatManager.InputHandler.CharacterPointerExit(stateMachine);
        }
    }
}