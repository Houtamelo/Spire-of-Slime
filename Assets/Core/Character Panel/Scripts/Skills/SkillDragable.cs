using Core.Combat.Scripts.Managers;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Game_Manager.Scripts;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utils.Patterns;

namespace Core.Character_Panel.Scripts.Skills
{
    public class SkillDragable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField, Required]
        private Image background;
        
        [SerializeField, Required]
        private Image iconHighlighted, iconHighlightedFx;
        
        private Camera _camera;
        private SkillsPanel _skillsPanel;
        private AudioSource _mouseOverSound, _holdingDragableSound;
        
        public Option<ISkill> Skill { get; private set; }
        private bool _hasHighlightedFx;
        private bool _isDragging;
        private Vector2 _dragOffset;

        private bool Interactable => CombatManager.Instance.TrySome(out CombatManager combatManager) == false || combatManager.Running == false;

        public void AssignReferences(SkillsPanel skillsPanel, Camera cam)
        {
            _skillsPanel = skillsPanel;
            _camera = cam;
        }

        public void AssignAudioSources(AudioSource mouseOverSound, AudioSource holdingDragableSound)
        {
            _mouseOverSound = mouseOverSound;
            _holdingDragableSound = holdingDragableSound;
        }

        public void SetSkill(ISkill skill)
        {
            if (skill == null)
            {
                Debug.LogWarning("Skill is null, deactivating.", context: this);
                Skill = Option<ISkill>.None;
                gameObject.SetActive(false);
                return;
            }
            
            Skill = Option<ISkill>.Some(skill);
            background.sprite = skill.IconBackground;
            iconHighlighted.sprite = skill.IconHighlightedSprite;
            Sprite highlightedFx = skill.IconHighlightedFx;
            _hasHighlightedFx = highlightedFx != null;
            if (_hasHighlightedFx)
            {
                iconHighlightedFx.sprite = highlightedFx;
                iconHighlightedFx.gameObject.SetActive(true);
            }
            else
            {
                iconHighlightedFx.sprite = null;
                iconHighlightedFx.gameObject.SetActive(false);
            }
            gameObject.SetActive(true);
        }
        
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;
            
            if (CombatManager.Instance.IsSome)
            {
                if (WorldTextCueManager.AssertInstance(out WorldTextCueManager cueManager))
                    cueManager.Show(new WorldCueOptions("I can't do that in combat.", 35f, transform.position, Color.red, 1f, 0.5f, Vector3.zero, HorizontalAlignmentOptions.Center,  stopOthers: true));

                return;
            }
            
            if (Skill.IsNone || Interactable == false)
                return;

            _holdingDragableSound.Play();
            _isDragging = true;
            _skillsPanel.OnSkillDragStart(this);
            _dragOffset = eventData.position - (Vector2)_camera.WorldToScreenPoint(transform.position);
            if (SkillTooltip.Instance.TrySome(out SkillTooltip tooltip))
                tooltip.Hide();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || _isDragging == false || Interactable == false)
                return;
            
            Vector2 desiredPosition = eventData.position - _dragOffset;
            Vector3 worldPosition = _camera.ScreenToWorldPoint(desiredPosition);
            worldPosition.z = 0;
            transform.position = worldPosition;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || _isDragging == false || Interactable == false)
                return;
            
            _isDragging = false;
            _holdingDragableSound.Stop();
            _skillsPanel.OnSkillDragEnd(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_isDragging == false && Skill.TrySome(out ISkill skill) && SkillTooltip.Instance.TrySome(out SkillTooltip tooltip))
            {
                tooltip.RawTooltip(skill);
                _mouseOverSound.Play();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (SkillTooltip.Instance.TrySome(out SkillTooltip tooltip))
                tooltip.Hide();
        }
    }
}