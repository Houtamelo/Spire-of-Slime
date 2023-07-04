using System;
using System.Collections.Generic;
using System.Linq;
using Core.Character_Panel.Scripts.Skills;
using Save_Management;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils.Extensions;
using Utils.Patterns;
using Save = Save_Management.Save;

namespace Core.Character_Panel.Scripts.Positioning
{
    public class PositioningPanel : MonoBehaviour
    {
        [SerializeField, Required, SceneObjectsOnly]
        private AudioSource confirmedAssignmentSound, invalidAssignmentSound;

        [SerializeField, Required]
        private RectTransform[] characterSlots = new RectTransform[4];

        [SerializeField, Required, SceneObjectsOnly]
        private CanvasGroup canvasGroup;

        [SerializeField, Required]
        private SerializableBounds[] slotsBounds = new SerializableBounds[4];
        
        [SerializeField, Required]
        private CharacterDragable[] orderedDragables = new CharacterDragable[4];

#if UNITY_EDITOR
        [Button]
        private void CalculateBounds()
        {
            for (int index = 0; index < characterSlots.Length; index++)
            {
                RectTransform rectTransform = characterSlots[index];
                Vector3[] bounds = new Vector3[4];
                rectTransform.GetWorldCorners(bounds);
                slotsBounds[index] = new SerializableBounds(bounds);
            }
            
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        private void Start()
        {
            Save.StringChanged += OnStringChanged;
            SetOpen(false);
        }

        private void OnDestroy()
        {
            Save.StringChanged -= OnStringChanged;
        }

        public void OnDragEnd(CharacterDragable dragged)
        {
            Option<CleanString> characterOption = dragged.Character;
            if (characterOption.IsNone)
            {
                Debug.LogWarning("Dragged character is none");
                return;
            }

            Save save = Save.Current;
            if (save == null)
            {
                Debug.LogWarning("Save is null");
                return;
            }

            Transform draggedTransform = dragged.transform;
            int maximumSlot = 0;
            Option<int> targetSlot = Option<int>.None;
            for (int i = orderedDragables.Length - 1 ; i >= 0; i--)
            {
                if (orderedDragables[i].gameObject.activeSelf)
                {
                    targetSlot = i;
                    maximumSlot = i;
                    break;
                }
            }
            
            for (int index = 0; index < maximumSlot; index++)
            {
                if (IsWithinBounds(slotsBounds[index], draggedTransform.position))
                {
                    targetSlot = index;
                    break;
                }
            }

            int oldIndex = Array.IndexOf(orderedDragables, dragged);
            int targetSlotIndex = targetSlot.Value;
            if (oldIndex != targetSlotIndex)
            {
                List<(CleanString key, bool bindToSave)> order = save.GetCombatOrderAsKeys().ToList();
                confirmedAssignmentSound.Play();
                order.ReInsert(currentIndex: oldIndex, targetIndex: targetSlotIndex);
                save.SetCombatOrder(order.Select(element => element.key));
            }
            else
            {
                invalidAssignmentSound.Play();
            }
            
            CheckOrder();
        }

        private void OnStringChanged(CleanString variableName, CleanString oldValue, CleanString newValue)
        {
            Save save = Save.Current;
            if (save == null)
            {
                Debug.LogWarning("Save is null", context: this);
                return;
            }

            if (variableName == VariablesName.Combat_Order)
                CheckOrder();
        }

        [Button]
        private void CheckOrder()
        {
            Save save = Save.Current;
            if (save == null)
                return;

            IReadOnlyList<(IReadonlyCharacterStats stats, bool bindToSave)> order = save.GetCombatOrderAsStats();
            int index = 0;
            for (; index < order.Count; index++)
            {
                CharacterDragable dragable = orderedDragables[index];
                dragable.SetCharacter(order[index].stats);
                dragable.transform.position = characterSlots[index].position;
            }
            
            for (; index < orderedDragables.Length; index++)
            {
                CharacterDragable dragable = orderedDragables[index];
                dragable.gameObject.SetActive(false);
            }
        }

        private static bool IsWithinBounds(SerializableBounds bounds, Vector3 position)
        {
            return position.x >= bounds[0].x && position.x <= bounds[2].x && position.y >= bounds[0].y && position.y <= bounds[2].y;
        }
        
        public void SetOpen(bool open)
        {
            canvasGroup.alpha = open ? 1 : 0;
            canvasGroup.interactable = open;
            canvasGroup.blocksRaycasts = open;
            if (open)
                CheckOrder();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            for (int index = 0; index < slotsBounds.Length; index++)
            {
                SerializableBounds bounds = slotsBounds[index];
                Gizmos.DrawLine(bounds[0], bounds[1]);
                Gizmos.DrawLine(bounds[1], bounds[2]);
                Gizmos.DrawLine(bounds[2], bounds[3]);
                Gizmos.DrawLine(bounds[3], bounds[0]);
                
                Vector3 center = bounds[0] + (bounds[2] - bounds[0]) / 2;
                UnityEditor.Handles.Label(center, index.ToString());
            }
        }
#endif
    }
}