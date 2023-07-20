using Core.Combat.Scripts.Interfaces;
using Core.Combat.Scripts.Managers;
using Core.Game_Manager.Scripts;
using Core.Save_Management.SaveObjects;
using Core.Utils.Patterns;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Core.Character_Panel.Scripts.Positioning
{
    public class CharacterDragable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler
    {
        [SerializeField, Required]
        private Image background;

        [SerializeField, Required]
        private Image icon;
        
        [SerializeField, Required, SceneObjectsOnly, ShowIn(PrefabKind.InstanceInScene)]
        private Camera targetCamera;

        [SerializeField, Required, SceneObjectsOnly, ShowIn(PrefabKind.InstanceInScene)]
        private PositioningPanel positioningPanel;
      
        [SerializeField, Required, SceneObjectsOnly, ShowIn(PrefabKind.InstanceInScene)]
        private AudioSource mouseOverSound, holdingDragableSound;
        
        public Option<CleanString> Character { get; private set; }
        
        private bool _isDragging;
        private Vector2 _dragOffset;

        private bool Interactable => CombatManager.Instance.TrySome(out CombatManager combatManager) == false || combatManager.Running == false;

        public void SetCharacter([CanBeNull] IReadonlyCharacterStats stats)
        {
            if (stats == null)
            {
                Debug.LogWarning("Stats is null, deactivating.", context: this);
                Character = Option<CleanString>.None;
                gameObject.SetActive(false);
                return;
            }
            
            Character = Option<CleanString>.Some(stats.Key);
            ICharacterScript script = stats.GetScript();

            Option<Color> portraitBackgroundColor = script.GetPortraitBackgroundColor;
            background.color = portraitBackgroundColor.IsSome ? portraitBackgroundColor.Value : Color.black;
            
            Option<Sprite> portrait = script.GetPortrait;
            if (portrait.IsSome)
            {
                icon.sprite = portrait.Value;
                icon.color = Color.white;
            }
            else
            {
                icon.sprite = null;
                icon.color = Color.clear;
            }
            
            gameObject.SetActive(true);
        }
        
        public void OnBeginDrag([NotNull] PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;
            
            if (CombatManager.Instance.IsSome)
            {
                if (WorldTextCueManager.AssertInstance(out WorldTextCueManager cueManager))
                    cueManager.Show(new WorldCueOptions("I can't do that in combat.", 35f, transform.position, Color.red, 1f, 0.5f, Vector3.zero, HorizontalAlignmentOptions.Center,  stopOthers: true));

                return;
            }
            
            if (Character.IsNone || Interactable == false)
                return;

            transform.SetAsLastSibling();
            holdingDragableSound.Play();
            _isDragging = true;
            _dragOffset = eventData.position - (Vector2)targetCamera.WorldToScreenPoint(transform.position);
        }

        public void OnDrag([NotNull] PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || _isDragging == false || Interactable == false)
                return;
            
            Vector2 desiredPosition = eventData.position - _dragOffset;
            Vector3 worldPosition = targetCamera.ScreenToWorldPoint(desiredPosition);
            worldPosition.z = 0;
            transform.position = worldPosition;
        }

        public void OnEndDrag([NotNull] PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || _isDragging == false || Interactable == false)
                return;
            
            _isDragging = false;
            holdingDragableSound.Stop();
            positioningPanel.OnDragEnd(dragged: this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_isDragging == false)
                mouseOverSound.Play();
        }
    }
}