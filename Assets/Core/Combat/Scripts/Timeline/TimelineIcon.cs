using System.Text;
using Core.Combat.Scripts.Behaviour;
using Core.Utils.Extensions;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Core.Combat.Scripts.Timeline
{
    public class TimelineIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private const float MoveDuration = 1f;
        private const float FadeDuration = 0.5f;
        private const float DeactivatedAnchoredY = 50f;
        private static readonly StringBuilder Builder = new();

        [SerializeField, Required]
        private Image characterIcon, typeIcon;

        [SerializeField, Required]
        private TMP_Text tooltip;

        [SerializeField, Required]
        private RectTransform selfRect;
        
        private TimelineManager _manager;
        private CharacterStateMachine _owner;
        private Sequence _sequence;
        private TweenCallback _deactivateCallback;

        private void Start()
        {
            _deactivateCallback = () => gameObject.SetActive(false);
        }

        public void Initialize(TimelineManager timelineIconsManager)
        {
            _manager = timelineIconsManager;
        }

        public void SetData(TimelineData data)
        {
            _owner = data.Owner;
            characterIcon.sprite = data.Owner.Script.TimelineIcon;
            if (TimelineIconDatabase.GetIcon(data.EventType).AssertSome(out Sprite iconSprite))
                typeIcon.sprite = iconSprite;
            
            tooltip.text = Builder.Override(data.Owner.Script.CharacterName.Translate().GetText(), '\n', data.Description).ToString();
        }

        public void SetTimelinePosition(int order)
        {
            _sequence.KillIfActive();
            
            float targetX = GetAnchoredPositionX(order);
            
            if (gameObject.activeSelf == false)
            {
                gameObject.SetActive(true);
                characterIcon.SetAlpha(0f);
                typeIcon.SetAlpha(0f);
                tooltip.enabled = false;
                selfRect.anchoredPosition = new Vector2(targetX, DeactivatedAnchoredY);
                _sequence = DOTween.Sequence().SetEase(Ease.OutQuad);
                _sequence.Append(selfRect.DOAnchorPosY(endValue: 0f, FadeDuration));
                _sequence.Join(characterIcon.DOFade(endValue: 1f, FadeDuration));
                _sequence.Join(typeIcon.DOFade(endValue: 1f, FadeDuration));
                return;
            }

            if (Mathf.Abs(targetX - selfRect.anchoredPosition.x) < 0.00001f 
             && Mathf.Abs(selfRect.anchoredPosition.y) < 0.00001f 
             && Mathf.Abs(characterIcon.color.a - 1f) < 0.00001f
             && Mathf.Abs(typeIcon.color.a - 1f) < 0.00001f)
            {
                selfRect.anchoredPosition = new Vector2(targetX, 0f);
                characterIcon.SetAlpha(1f);
                typeIcon.SetAlpha(1f);
                return;
            }
            
            _sequence = DOTween.Sequence().SetEase(Ease.OutQuad);
            _sequence.Join(selfRect.DOAnchorPos(endValue: new Vector2(targetX, 0f), MoveDuration));
            _sequence.Join(characterIcon.DOFade(endValue: 1f, FadeDuration));
            _sequence.Join(typeIcon.DOFade(endValue: 1f, FadeDuration));
        }

        public void Deactivate()
        {
            if (gameObject.activeSelf == false)
                return;
            
            _sequence.KillIfActive();
            
            _sequence = DOTween.Sequence();
            _sequence.Append(selfRect.DOAnchorPosY(endValue: DeactivatedAnchoredY, FadeDuration));
            _sequence.Join(characterIcon.DOFade(endValue: 0f, FadeDuration));
            _sequence.Join(typeIcon.DOFade(endValue: 0f, FadeDuration));
            _sequence.OnComplete(_deactivateCallback);
        }

        private void OnDisable()
        {
            _sequence.KillIfActive();
        }

        private float GetAnchoredPositionX(int order) => order * (selfRect.rect.width + _manager.IconSpacing);

        private void Reset()
        {
            selfRect = (RectTransform)transform;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            tooltip.gameObject.SetActive(true);
            if (_owner != null && _owner.Display.AssertSome(out DisplayModule display))
                display.CombatManager.InputHandler.CharacterPointerEnter(_owner);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            tooltip.gameObject.SetActive(false);
            if (_owner != null && _owner.Display.AssertSome(out DisplayModule display))
                display.CombatManager.InputHandler.CharacterPointerExit(_owner);
        }
    }
}