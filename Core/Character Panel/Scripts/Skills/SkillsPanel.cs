using System;
using System.Collections.Generic;
using Core.Combat.Scripts.Interfaces;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Interfaces;
using ListPool;
using Main_Database.Combat;
using Save_Management;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils.Extensions;
using Utils.Patterns;
using Save = Save_Management.Save;

namespace Core.Character_Panel.Scripts.Skills
{
    public class SkillsPanel : MonoBehaviour
    {
        [SerializeField, Required]
        private AudioSource mouseOverSound, holdingDragableSound, confirmedAssignmentSound, confirmedUnAssignmentSound;
        
        [SerializeField, Required]
        private SkillDragable skillDragablePrefab;
        
        [SerializeField, Required] 
        private Transform availableSkillsGridLayout, looseSkillsParent;

        [SerializeField, Required]
        private RectTransform[] skillSlots = new RectTransform[4];

        [SerializeField, Required]
        private Camera sceneCamera;

        [SerializeField, Required]
        private CanvasGroup canvasGroup;

        [SerializeField, Required, SceneObjectsOnly]
        private CharacterMenuManager characterMenu;

        [SerializeField, Required]
        private SerializableBounds[] skillSlotsBounds = new SerializableBounds[4];
        
        private readonly SkillDragable[] _setDragables = new SkillDragable[4];
        private readonly List<SkillDragable> _spawnedDragables = new();

        private int _currentSkillSetHash;

#if UNITY_EDITOR
        [Button]
        private void CalculateBounds()
        {
            for (int index = 0; index < skillSlots.Length; index++)
            {
                RectTransform rectTransform = skillSlots[index];
                Vector3[] bounds = new Vector3[4];
                rectTransform.GetWorldCorners(bounds);
                skillSlotsBounds[index] = new SerializableBounds(bounds);
            }
            
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        private void Start()
        {
            Save.StringChanged += OnStringChanged;
            Save.BoolChanged += OnBoolChanged;
            characterMenu.SelectedCharacter.Changed += OnCharacterSelected;
            OnCharacterSelected(characterMenu.SelectedCharacter.AsOption());
            SetOpen(false);
        }

        private void OnDestroy()
        {
            Save.StringChanged -= OnStringChanged;
            Save.BoolChanged -= OnBoolChanged;
            if (characterMenu != null)
                characterMenu.SelectedCharacter.Changed -= OnCharacterSelected;
        }

        private void OnCharacterSelected(Option<IReadonlyCharacterStats> _)
        {
            CheckAssignedSkills();
        }

        public void OnSkillDragEnd(SkillDragable dragged)
        {
            Option<ISkill> skillOption = dragged.Skill;
            if (skillOption.IsNone)
            {
                Debug.LogWarning("Dragged skill is none");
                return;
            }

            Save save = Save.Current;
            if (save == null)
            {
                Debug.LogWarning("Save is null");
                return;
            }

            Option<IReadonlyCharacterStats> selectedCharacter = characterMenu.SelectedCharacter.AsOption();
            if (selectedCharacter.IsNone)
            {
                Debug.LogWarning("Selected character is none");
                return;
            }

            int oldHash = _currentSkillSetHash;
            IReadonlyCharacterStats stats = selectedCharacter.Value;
            Transform draggedTransform = dragged.transform;
            Option<int> targetSlot = Option<int>.None;
            for (int index = 0; index < 4; index++)
            {
                if (IsWithinBounds(skillSlotsBounds[index], draggedTransform.position))
                {
                    targetSlot = index;
                    break;
                }
            }
            
            Option<int> oldIndex = _setDragables.IndexOf(dragged);
            if (targetSlot.IsNone)
            {
                if (oldIndex.IsSome)
                {
                    save.UnassignSkill(stats.Key, skillOption.Value); // this will trigger the string changed event, which will call CheckAssignedSkills
                    CheckAssignedSkillsIfHashDidntChange(oldHash);
                    confirmedUnAssignmentSound.Play();
                    return;
                }
                
                draggedTransform.SetParent(availableSkillsGridLayout);
                draggedTransform.SetAsLastSibling();
                CheckAssignedSkillsIfHashDidntChange(oldHash);
                return;
            }

            int targetSlotIndex = targetSlot.Value;
            switch (oldIndex.IsSome)
            {
                case true when oldIndex.Value == targetSlotIndex: // no change so we just re-center the dragged at it's slot
                    draggedTransform.SetParent(looseSkillsParent);
                    draggedTransform.position = skillSlots[targetSlotIndex].position;
                    break;
                case true:
                    save.SwitchSkillSlot(stats.Key, skillOption.Value, targetSlotIndex);
                    confirmedAssignmentSound.Play();
                    break;
                default:
                    save.OverrideSkill(stats.Key, skillOption.Value, targetSlotIndex);
                    confirmedAssignmentSound.Play();
                    break;
            }
            
            CheckAssignedSkillsIfHashDidntChange(oldHash);
        }

        /// <summary>
        /// If a dragable was messed with but the skillset didn't detect any actual changes (it sorts itself),
        /// the hash will remain the same so we need to force CheckAssignedSkills() in order to fix the dragables, if the hash did change then StringChanged will call CheckAssignedSkills()
        /// </summary>
        private void CheckAssignedSkillsIfHashDidntChange(int oldHash)
        {
            if (oldHash == _currentSkillSetHash)
                CheckAssignedSkills();
        }

        private void OnStringChanged(CleanString variableName, CleanString oldValue, CleanString newValue)
        {
            Save save = Save.Current;
            if (save == null)
            {
                Debug.LogWarning("Save is null", context: this);
                return;
            }

            Option<IReadonlyCharacterStats> character = characterMenu.SelectedCharacter.AsOption();
            if (character.IsNone)
                return;

            int newHash = character.Value.GetSkillSet().GetHashCode();
            if (newHash != _currentSkillSetHash)
                CheckAssignedSkills();
        }

        private void OnBoolChanged(CleanString variableName, bool oldValue, bool newValue)
        {
            if (variableName.StartsWith("perk_") || variableName.StartsWith("skill_"))
                CheckAssignedSkills();
        }

        [Button]
        private void CheckAssignedSkills()
        {
            Save save = Save.Current;
            if (save == null)
                return;

            Option<IReadonlyCharacterStats> selectedCharacter = characterMenu.SelectedCharacter.AsOption();
            if (selectedCharacter.IsNone)
                return;
            
            IReadonlyCharacterStats stats = selectedCharacter.Value;
            Array.Fill(_setDragables, null);
            ICharacterScript characterScript = stats.GetScript();
            IReadOnlyList<ISkill> allSkills = characterScript.GetAllPossibleSkills();
            for (int i = _spawnedDragables.Count; i < allSkills.Count || i < 4; i++)
                CreateDragable(availableSkillsGridLayout);

            IReadOnlySkillSet skillSet = stats.GetSkillSet();
            int dragableIndex = 0;
            for (int skillSetIndex = 0; skillSetIndex < 4; skillSetIndex++)
            {
                Option<CleanString> skillKeyOption = save.GetSkill(stats.Key, skillSetIndex);
                if (skillKeyOption.IsNone) // skill-sets are ordered by non-null values, so there won't be any more after this
                    break;

            #region FindingSkillObject
                CleanString skillKey = skillKeyOption.Value;
                bool foundSkill = false;
                ISkill skill = null;
                foreach (ISkill element in allSkills)
                {
                    if (element.Key == skillKey)
                    {
                        foundSkill = true;
                        skill = element;
                        break;
                    }
                }

                if (foundSkill == false)
                {
                    Debug.LogWarning($"Skill with {skillKey.ToString()} not included in character possible skills, searching on database...");
                    Option<SkillScriptable> skillOption = SkillDatabase.GetSkill(skillKey);
                    if (skillOption.IsSome)
                    {
                        skill = skillOption.Value;
                    }
                    else
                    {
                        Debug.LogWarning($"Skill with {skillKey.ToString()} not found on database.");
                        continue;
                    }
                }
            #endregion
                
                SkillDragable dragable = _spawnedDragables[dragableIndex];
                _setDragables[skillSetIndex] = dragable;
                dragable.SetSkill(skill);
                Transform dragableTransform = dragable.transform;
                dragableTransform.SetParent(looseSkillsParent);
                dragableTransform.position = skillSlots[skillSetIndex].position;
                dragableIndex++;
            }

            using ValueListPool<int> indexesToSkip = new(allSkills.Count);
            for (int index = 0; index < allSkills.Count; index++)
            {
                ISkill skill = allSkills[index];
                bool unlocked = save.GetVariable<bool>(skill.Key);
                if (unlocked == false)
                    continue;
                
                bool alreadySet = false;
                for (int i = 0; i < _setDragables.Length; i++)
                {
                    SkillDragable setDragable = _setDragables[i];
                    if (setDragable == null)
                        continue;

                    if (setDragable.Skill.IsNone)
                    {
                        Debug.LogWarning($"SkillDragable {setDragable.name} has no skill assigned, but it's in the setDragables array, this shouldn't happen", context: setDragable);
                        continue;
                    }
                    
                    if (setDragable.Skill.Value.Key == skill.Key)
                    {
                        alreadySet = true;
                        break;
                    }
                }
                
                if (alreadySet)
                    continue;
                
                bool alreadyBelongsToAvailable = false;
                for (int i = dragableIndex; i < _spawnedDragables.Count; i++)
                {
                    SkillDragable availableDragable = _spawnedDragables[i];
                    if (availableDragable.Skill.IsNone)
                        continue;

                    if (availableDragable.Skill.Value.Key == skill.Key && availableDragable.gameObject.activeSelf)
                    {
                        alreadyBelongsToAvailable = true;
                        availableDragable.transform.SetParent(availableSkillsGridLayout);
                        indexesToSkip.Add(i);
                        break;
                    }
                }
                
                if (alreadyBelongsToAvailable)
                    continue;
                
                while (indexesToSkip.Contains(dragableIndex))
                    dragableIndex++;
                
                SkillDragable dragable = _spawnedDragables[dragableIndex];
                dragable.SetSkill(skill);
                Transform dragableTransform = dragable.transform;
                dragableTransform.SetParent(availableSkillsGridLayout);
                dragableIndex++;
            }
            
            for (; dragableIndex < _spawnedDragables.Count; dragableIndex++)
            {
                if (indexesToSkip.Contains(dragableIndex))
                    continue;
                
                SkillDragable dragable = _spawnedDragables[dragableIndex];
                Transform dragableTransform = dragable.transform;
                dragableTransform.SetParent(looseSkillsParent);
                dragable.gameObject.SetActive(false);
            }
            
            _currentSkillSetHash = skillSet.GetHashCode();
        }

        private SkillDragable CreateDragable(Transform parent)
        {
            SkillDragable dragable = skillDragablePrefab.InstantiateWithFixedLocalScale(parent);
            dragable.AssignReferences(this, sceneCamera);
            dragable.AssignAudioSources(mouseOverSound, holdingDragableSound);
            _spawnedDragables.Add(dragable);
            return dragable;
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
                CheckAssignedSkills();
        }

        public void OnSkillDragStart(SkillDragable skillDragable)
        {
            skillDragable.transform.SetParent(looseSkillsParent);
            skillDragable.transform.SetAsLastSibling();
        }
    }
}