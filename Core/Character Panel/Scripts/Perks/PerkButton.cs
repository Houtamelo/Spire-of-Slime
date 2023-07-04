using Core.Combat.Scripts.Managers;
using Core.Combat.Scripts.Perks;
using Core.Combat.Scripts.Skills;
using Core.Game_Manager.Scripts;
using ListPool;
using Save_Management;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utils.Patterns;
using Save = Save_Management.Save;

namespace Core.Character_Panel.Scripts.Perks
{
    public class PerkButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField, Required]
        private Image icon;

        [SerializeField, Required]
        private Image frame;
        
        [SerializeField, Required]
        private Toggle toggle;

        [SerializeField, Required]
        private Image toggleOnDisplay, toggleOffDisplay;

        [SerializeField, RequiredIn(PrefabKind.InstanceInScene)]
        private PerkScriptable perk;
        public  PerkScriptable Perk => perk;

        [SerializeField]
        private Color baseColor, mouseOverColor;

        [SerializeField, RequiredIn(PrefabKind.InstanceInScene)]
        private AudioSource pointerEnterSource, confirmPerkSource, invalidClickSource;

        [SerializeField, Required]
        private Material desaturateMaterial;
        
        private bool _unlocked;
        private bool _prerequisitesMet;

        private void Start()
        {
            toggle.onValueChanged.AddListener(isOn =>
            {
                if (isOn && _unlocked == false)
                {
                    Debug.LogWarning("Tried to activate a perk that isn't unlocked.", context: this);
                    return;
                }
                
                if (CharacterMenuManager.AssertInstance(out CharacterMenuManager characterMenuManager) == false)
                    return;
                
                if (Save.AssertInstance(out Save save) == false)
                    return;
                
                if (characterMenuManager.SelectedCharacter.AsOption().AssertSome(out IReadonlyCharacterStats stats) == false)
                    return;

                CleanString activateKey = VariablesName.EnabledPerkName(stats.Key, perk.Key);
                save.SetVariable(activateKey, isOn);
            });
        }

        public void UpdateSelf(ref ValueListPool<CleanString> unlockedPerksAndSkills, ref ValueListPool<CleanString> activePerks)
        {
            _unlocked = unlockedPerksAndSkills.Contains(perk.Key);
            toggle.SetIsOnWithoutNotify(activePerks.Contains(perk.Key));
            if (_unlocked)
            {
                icon.material = null;
                frame.material = null;
                toggleOnDisplay.material = null;
                toggleOffDisplay.material = null;
                _prerequisitesMet = true;
                toggle.interactable = true;
            }
            else
            {
                icon.material = desaturateMaterial;
                frame.material = desaturateMaterial;
                toggleOnDisplay.material = desaturateMaterial;
                toggleOffDisplay.material = desaturateMaterial;
                _prerequisitesMet = ArePrerequisitesMet(perk, ref unlockedPerksAndSkills);
                toggle.interactable = false;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;
            
            if (_unlocked)
                return;

            if (_prerequisitesMet == false)
            {
                if (WorldTextCueManager.AssertInstance(out WorldTextCueManager cueManager))
                    cueManager.Show(new WorldCueOptions("Prerequisites aren't met.", 35f, transform.position, Color.red, 1f, 0.5f, Vector3.zero, HorizontalAlignmentOptions.Center, true));
                
                invalidClickSource.Play();
                return;
            }

            pointerEnterSource.Play();
            Save save = Save.Current;
            if (save == null)
            {
                Debug.LogWarning("Perk button clicked but no save is active.");
                return;
            }

            if (CombatManager.Instance.IsSome)
            {
                if (WorldTextCueManager.AssertInstance(out WorldTextCueManager cueManager))
                    cueManager.Show(new WorldCueOptions("You can't do that in combat.", 35f, transform.position, Color.red, 1f, 0.5f, Vector3.zero, HorizontalAlignmentOptions.Center, true));

                invalidClickSource.Play();
                return;
            }
            
            if (CharacterMenuManager.AssertInstance(out CharacterMenuManager characterMenuManager) == false)
                return;

            Option<IReadonlyCharacterStats> character = characterMenuManager.SelectedCharacter.AsOption();
            if (character.IsNone)
            {
                Debug.LogWarning("Perk button clicked but no character is selected.");
                return;
            }

            IReadonlyCharacterStats stats = character.Value;

            if (stats.IsPerkUnlocked(perk.Key, save))
                return;

            if (ArePrerequisitesMet(perk, stats, save) == false)
            {
                Debug.LogWarning($"Perk button is interactable but prerequisites aren't met. Character is {stats.Key}");
                return;
            }

            if (stats.AvailablePerkPoints <= 0)
            {
                invalidClickSource.Play();
                if (WorldTextCueManager.AssertInstance(out WorldTextCueManager textCueManager))
                    textCueManager.Show(new WorldCueOptions("Not enough perk points.", 35f, transform.position, Color.red, 1f, 0.5f, Vector3.zero, HorizontalAlignmentOptions.Center, true));

                return;
            }

            if (AreYouSurePanel.AssertInstance(out AreYouSurePanel areYouSurePanel) == false)
                return;

            areYouSurePanel.Show(() => OnYes(confirmPerkSource, button: this, stats.Key, save), message: $"Are you sure you wish to unlock {perk.DisplayName}?");

            static void OnYes(AudioSource confirmPerkSource, PerkButton button, CleanString characterKey, Save save)
            {
                if (save.GetReadOnlyStats(characterKey).AssertSome(out IReadonlyCharacterStats stats) == false)
                    return;
                
                if (stats.IsPerkUnlocked(button.perk.Key, save))
                {
                    Debug.LogWarning("On Yes panel is interactable but perk is already unlocked.", button);
                    return;
                }
            
                if (ArePrerequisitesMet(button.perk, stats, save) == false)
                {
                    Debug.LogWarning($"On Yes panel is interactable but prerequisites aren't met. Character is {stats.Key}", button);
                    return;
                }

                confirmPerkSource.Play();
                save.UnlockPerk(button.perk.Key);
                button.toggle.isOn = true;
            }
        }

        private static bool ArePrerequisitesMet(PerkScriptable perk, IReadonlyCharacterStats stats, Save save)
        {
            using (ValueListPool<CleanString> unlockedPerksAndSkills = stats.GetUnlockedPerksAndSkills(save))
            {
                foreach (PerkScriptable prerequisite in perk.PerkPrerequisites)
                    if (unlockedPerksAndSkills.Contains(prerequisite.Key) == false)
                        return false;

                foreach (SkillScriptable prerequisite in perk.SkillPrerequisites)
                    if (unlockedPerksAndSkills.Contains(prerequisite.Key) == false)
                        return false;
            }

            return true;
        }

        private static bool ArePrerequisitesMet(PerkScriptable perk, ref ValueListPool<CleanString> unlockedPerksAndSkills)
        {
            foreach (PerkScriptable prerequisite in perk.PerkPrerequisites)
                if (unlockedPerksAndSkills.Contains(prerequisite.Key) == false)
                    return false;

            foreach (SkillScriptable prerequisite in perk.SkillPrerequisites)
                if (unlockedPerksAndSkills.Contains(prerequisite.Key) == false)
                    return false;
            
            return true;
        }

        private void SetColor(Color color)
        {
            frame.color = color;
            icon.color = color;
        }
        
        public void OnPointerEnter(PointerEventData _)
        {
            if (PerkTooltip.AssertInstance(out PerkTooltip perkTooltip))
                perkTooltip.Show(perk);
            
            pointerEnterSource.Play();
            SetColor(mouseOverColor);
        }

        public void OnPointerExit(PointerEventData _)
        {
            if (PerkTooltip.AssertInstance(out PerkTooltip perkTooltip))
                perkTooltip.Hide();
            
            SetColor(baseColor);
        }
        
#if UNITY_EDITOR
        private void CheckName()
        {
            if (perk == null)
                return;
            
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
            if (string.IsNullOrEmpty(assetPath))
            {
                name = perk.Key.Remove("ethel_").Remove("nema_").ToString();
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
        
        public void SetPerk(PerkScriptable newPerk)
        {
            perk = newPerk;
            icon.sprite = perk.Icon;
            SetColor(baseColor);
            icon.material = desaturateMaterial;
            frame.material = desaturateMaterial;
            toggleOnDisplay.material = desaturateMaterial;
            toggleOffDisplay.material = desaturateMaterial;
            CheckName();
        }
        
        public void SetSources(AudioSource pointerEnter, AudioSource confirmPerk, AudioSource invalidClick)
        {
            pointerEnterSource = pointerEnter;
            confirmPerkSource = confirmPerk;
            invalidClickSource = invalidClick;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}