using Core.Character_Panel.Scripts.Skills;
using Core.Combat.Scripts.Managers;
using Core.Combat.Scripts.Skills.Interfaces;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utils.Extensions;
using Utils.Patterns;
using static UnityEngine.EventSystems.PointerEventData;

namespace Core.Combat.Scripts.UI
{
    public sealed class SkillButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private const float FadeDuration = 0.5f;
        private const float InactiveAlpha = 0.15f;
        private static readonly Color NonInteractableIconsColor = new(0.26667f, 0.26667f, 0.26667f, 1f);
        private static readonly Color NonInteractableFrameColor = new(0.39216f, 0.39216f, 0.39216f, 1f);

        [SerializeField, Required] 
        private Image background;
        
        [SerializeField, Required]
        private Image iconBase, iconBaseFx;
        
        [SerializeField, Required]
        private Image iconHighlighted, iconHighlightedFx;

        [SerializeField, Required]
        private Image frame;

        [SerializeField, Required]
        private GameObject selectedIndicator;

        [SerializeField, Required, ShowIn(PrefabKind.InstanceInScene)]
        private AudioSource pointerEnterSound;

        private int _baseChildIndex, _baseFxChildIndex;
        private int _highlightedChildIndex, _highlightedFxChildIndex;
        private bool _hasBase, _hasHighlighted;
        private bool _hasBaseFx, _hasHighlightedFx;

        public Option<ISkill> Skill { get; private set; } = Option<ISkill>.None;
        private Sequence _animationSequence;

        private bool _isMouseOver;
        private bool _interactable;
        private bool _showingTooltip;
        private void Start()
        {
            if (CombatManager.Instance.TrySome(out CombatManager combatManager))
            {
                combatManager.InputHandler.SelectedSkill.Changed += CheckIfSelected;
                CheckIfSelected(skillScript: combatManager.InputHandler.SelectedSkill.Value);
            }
            else
            {
                Debug.LogWarning("Combat manager is not present in the scene.");
            }
            
            _baseChildIndex = iconBase.transform.GetSiblingIndex();
            _baseFxChildIndex = iconBaseFx.transform.GetSiblingIndex();
            _highlightedChildIndex = iconHighlighted.transform.GetSiblingIndex();
            _highlightedFxChildIndex = iconHighlightedFx.transform.GetSiblingIndex();
        }

        private void OnDestroy()
        {
            if (CombatManager.Instance.TrySome(out CombatManager combatManager))
                combatManager.InputHandler.SelectedSkill.Changed -= CheckIfSelected;
        }

        public void SetSkill(ISkill skill)
        {
            if (skill == null)
            {
                Debug.LogWarning("Skill is null", context: this);
                ResetMe();
                return;
            }
            
            Skill = Option<ISkill>.Some(skill);
            AssignSprite(skill.IconBackground,        background,        out _); // all skills have background
            AssignSprite(skill.IconBaseSprite,        iconBase,          out _hasBase);
            AssignSprite(skill.IconBaseFx,            iconBaseFx,        out _hasBaseFx);
            AssignSprite(skill.IconHighlightedSprite, iconHighlighted,   out _hasHighlighted);
            AssignSprite(skill.IconHighlightedFx,     iconHighlightedFx, out _hasHighlightedFx);
            gameObject.SetActive(true);
            SetInteractable(false);
        }

        private void CheckIfSelected(ISkill skillScript)
        {
            if (_interactable == false || Skill.IsNone)
                return;
            
            if (skillScript == Skill.Value)
            {
                Highlight();
                selectedIndicator.SetActive(true);
            }
            else
            {
                Base();
                selectedIndicator.SetActive(false);
            }
        }

        private void Base()
        {
            if (_interactable == false)
                return;
            
            if (_animationSequence is { active: true })
                _animationSequence.Kill();
            
            iconBase.transform.SetSiblingIndex(_highlightedChildIndex);
            iconBaseFx.transform.SetSiblingIndex(_highlightedFxChildIndex);
            iconHighlighted.transform.SetSiblingIndex(_baseChildIndex);
            iconHighlightedFx.transform.SetSiblingIndex(_baseFxChildIndex);
            background.color = Color.white;
            
            if (_hasBase == false && _hasBaseFx == false && _hasHighlighted == false && _hasHighlightedFx == false)
                return;

            _animationSequence = DOTween.Sequence();
            if (_hasBase)
            {
                Color color = Color.white;
                color.a = iconBase.color.a;
                iconBase.color = color;
                _animationSequence.Join(iconBase.DOFade(1f, FadeDuration));
            }
            if (_hasBaseFx)
            {
                Color color = Color.white;
                color.a = iconBaseFx.color.a;
                iconBaseFx.color = color;
                _animationSequence.Join(iconBaseFx.DOFade(1f, FadeDuration));
            }
            if (_hasHighlighted)
            {
                Color color = Color.white;
                color.a = iconHighlighted.color.a;
                iconHighlighted.color = color;
                _animationSequence.Join(iconHighlighted.DOFade(InactiveAlpha, FadeDuration));
            }
            if (_hasHighlightedFx)
            {
                Color color = Color.white;
                color.a = iconHighlightedFx.color.a;
                iconHighlightedFx.color = color;
                _animationSequence.Join(iconHighlightedFx.DOFade(InactiveAlpha, FadeDuration));
            }
        }

        private void Highlight()
        {
            if (_interactable == false)
                return;

            _animationSequence.KillIfActive();
            
            iconHighlighted.transform.SetSiblingIndex(_highlightedChildIndex);
            iconHighlightedFx.transform.SetSiblingIndex(_highlightedFxChildIndex);
            iconBase.transform.SetSiblingIndex(_baseChildIndex);
            iconBaseFx.transform.SetSiblingIndex(_baseFxChildIndex);
            background.color = Color.white;
            
            if (_hasBase == false && _hasBaseFx == false && _hasHighlighted == false && _hasHighlightedFx == false)
                return;

            _animationSequence = DOTween.Sequence();
            if (_hasBase)
            {
                Color color = Color.white;
                color.a = iconBase.color.a;
                iconBase.color = color;
                _animationSequence.Join(iconBase.DOFade(InactiveAlpha, FadeDuration));
            }
            if (_hasBaseFx)
            {
                Color color = Color.white;
                color.a = iconBaseFx.color.a;
                iconBaseFx.color = color;
                _animationSequence.Join(iconBaseFx.DOFade(InactiveAlpha, FadeDuration));
            }
            if (_hasHighlighted)
            {
                Color color = Color.white;
                color.a = iconHighlighted.color.a;
                iconHighlighted.color = color;
                _animationSequence.Join(iconHighlighted.DOFade(1f, FadeDuration));
            }
            if (_hasHighlightedFx)
            {
                Color color = Color.white;
                color.a = iconHighlightedFx.color.a;
                iconHighlightedFx.color = color;
                _animationSequence.Join(iconHighlightedFx.DOFade(1f, FadeDuration));
            }
        }

        public void SetInteractable(bool value)
        {
            _interactable = value;
            if (value == true)
            {
                frame.color = Color.white;
                if (_isMouseOver)
                    Highlight();
                else
                    Base();
            }
            else
            {
                _animationSequence.KillIfActive();
                
                iconBase.transform.SetSiblingIndex(_highlightedChildIndex);
                iconBaseFx.transform.SetSiblingIndex(_highlightedFxChildIndex);
                iconHighlighted.transform.SetSiblingIndex(_baseChildIndex);
                iconHighlightedFx.transform.SetSiblingIndex(_baseFxChildIndex);

                frame.color = NonInteractableFrameColor;
                background.color = NonInteractableIconsColor;
                iconBase.color = NonInteractableIconsColor;
                iconBaseFx.color = NonInteractableIconsColor;

                Color color = NonInteractableIconsColor;
                color.a = InactiveAlpha;
                iconHighlighted.color = color;
                iconHighlightedFx.color = color;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_interactable && eventData.button == InputButton.Left && CombatManager.AssertInstance(out CombatManager combatManager))
                combatManager.InputHandler.DoSelectSkill(Skill.Value);
        }

        public void OnPointerEnter(PointerEventData _)
        {
            _isMouseOver = true;
            if (SkillTooltip.AssertInstance(out SkillTooltip skillTooltip) == false)
                return;
            
            skillTooltip.Show(Skill.Value);

            if (_interactable == false)
                return;
            
            Highlight();
            pointerEnterSound.Play();
        }

        public void OnPointerExit(PointerEventData _)
        {
            if (SkillTooltip.AssertInstance(out SkillTooltip tooltip))
                tooltip.Hide();

            _isMouseOver = false;
            if (CombatManager.AssertInstance(out CombatManager combatManager) && combatManager.InputHandler.IsButtonSelected(this) == false)
                Base();
        }

        public void ResetMe()
        {
            if (_isMouseOver)
            {
                OnPointerExit(null);
            }
            
            Skill = Option<ISkill>.None;
            _hasBase = false;
            _hasHighlighted = false;
            _hasBaseFx = false;
            _hasHighlightedFx = false;
            _isMouseOver = false;
            _interactable = false;
            background.sprite = null;
            background.color = Color.clear;
            iconBase.sprite = null;
            iconBase.color = Color.clear;
            iconBaseFx.sprite = null;
            iconBaseFx.color = Color.clear;
            iconHighlighted.sprite = null;
            iconHighlighted.color = Color.clear;
            iconHighlightedFx.sprite = null;
            iconHighlightedFx.color = Color.clear;
            gameObject.SetActive(false);
        }

        private static void AssignSprite(Sprite sprite, Image image, out bool hasSprite)
        {
            hasSprite = sprite != null;
            if (hasSprite)
            {
                image.sprite = sprite;
                image.color = Color.white;
                image.gameObject.SetActive(true);
            }
            else
            {
                image.sprite = null;
                image.color = Color.clear;
                image.gameObject.SetActive(false);
            }
        }
    }
}