using System.Collections.Generic;
using Core.Main_Characters.Nema.Combat;
using Data.Main_Characters.Ethel;
using KGySoft.CoreLibraries;
using ListPool;
using Save_Management;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using Utils.Patterns;
using Save = Save_Management.Save;

namespace Core.Character_Panel.Scripts.Perks
{
    public class PerksPanel : MonoBehaviour
    {
        private static readonly CleanString EthelPerkPoints = VariablesName.StatName(Ethel.GlobalKey, GeneralStat.PerkPoints);
        private static readonly CleanString NemaPerkPoints = VariablesName.StatName(Nema.GlobalKey, GeneralStat.PerkPoints);
        
        [SerializeField, Required, SceneObjectsOnly]
        private CanvasGroup ethelPanel;
        
        [SerializeField, Required, SceneObjectsOnly]
        private CanvasGroup nemaPanel;

        [SerializeField, Required, SceneObjectsOnly]
        private CharacterMenuManager characterMenu;

        [SerializeField, Required, SceneObjectsOnly]
        private CanvasGroup canvasGroup;

        [SerializeField, Required, SceneObjectsOnly]
        private TMP_Text availablePointsTmp;

        private readonly HashSet<PerkButton> _ethelPerkButtons = new();
        private readonly HashSet<SkillUnlockerButton> _ethelSkillUnlockerButtons = new();
        private readonly HashSet<PerkButton> _nemaPerkButtons = new();
        private readonly HashSet<SkillUnlockerButton> _nemaSkillUnlockerButtons = new();

        private void Awake()
        {
            _ethelPerkButtons.AddRange(ethelPanel.GetComponentsInChildren<PerkButton>(includeInactive: true));
            _ethelSkillUnlockerButtons.AddRange(ethelPanel.GetComponentsInChildren<SkillUnlockerButton>(includeInactive: true));
            _nemaPerkButtons.AddRange(nemaPanel.GetComponentsInChildren<PerkButton>(includeInactive: true));
            _nemaSkillUnlockerButtons.AddRange(nemaPanel.GetComponentsInChildren<SkillUnlockerButton>(includeInactive: true));
        }

        private void Start()
        {
            Save.BoolChanged += OnBoolChanged;
            Save.FloatChanged += OnFloatChanged;
            characterMenu.SelectedCharacter.Changed += OnCharacterSelected;
            OnCharacterSelected(characterMenu.SelectedCharacter.AsOption());
            SetOpen(false);
        }

        private void OnDestroy()
        {
            Save.BoolChanged -= OnBoolChanged;
            Save.FloatChanged -= OnFloatChanged;
            if (characterMenu != null)
                characterMenu.SelectedCharacter.Changed -= OnCharacterSelected;
        }

        private void OnBoolChanged(CleanString variableName, bool oldValue, bool newValue)
        {
            CleanString enabledPerkPrefix = VariablesName.EnabledPerkPrefix();
            if (variableName.StartsWith("perk_") || variableName.StartsWith("skill_") || variableName.StartsWith(ref enabledPerkPrefix))
                CheckPerks();
        }

        private void OnFloatChanged(CleanString variableName, float oldValue, float newValue)
        {
            Option<IReadonlyCharacterStats> character = characterMenu.SelectedCharacter.AsOption();
            if (character.TrySome(out IReadonlyCharacterStats stats) == false)
                return;
                    
            if (variableName == EthelPerkPoints && stats.Key == Ethel.GlobalKey || variableName == NemaPerkPoints && stats.Key == Nema.GlobalKey)
                availablePointsTmp.text = $"Available Points: {newValue.ToString("0")}";
        }

        private void OnCharacterSelected(Option<IReadonlyCharacterStats> character)
        {
            if (character.IsNone)
            {
                SetCanvasGroup(ethelPanel, false);
                SetCanvasGroup(nemaPanel, false);
                return;
            }
         
            CheckPerks();

            if (character.Value.Key == Ethel.GlobalKey)
            {
                SetCanvasGroup(ethelPanel, true);
                SetCanvasGroup(nemaPanel,  false);
            }
            else if (character.Value.Key == Nema.GlobalKey)
            {
                SetCanvasGroup(ethelPanel, false);
                SetCanvasGroup(nemaPanel,  true);
            }
            else
            {
                SetCanvasGroup(ethelPanel, false);
                SetCanvasGroup(nemaPanel,  false);
                return;
            }

            availablePointsTmp.text = $"Available Points: {character.Value.AvailablePerkPoints.ToString("0")}";
        }

        private void CheckPerks()
        {
            Save save = Save.Current;
            if (save == null)
            {
                Debug.LogWarning("Trying to update perks but no save is active", context: this);
                return;
            }

            Option<IReadonlyCharacterStats> character = characterMenu.SelectedCharacter.AsOption();
            if (character.IsNone)
            {
                Debug.LogWarning("Trying to update perks but no character is selected", context: this);
                return;
            }

            IReadonlyCharacterStats stats = character.Value;
            ValueListPool<CleanString> unlockedPerksAndSkills = stats.GetUnlockedPerksAndSkills(save);
            ValueListPool<CleanString> activePerks = stats.GetEnabledPerks(save);
            if (stats.Key == Ethel.GlobalKey)
            {
                foreach (PerkButton perkButton in _ethelPerkButtons)
                    perkButton.UpdateSelf(ref unlockedPerksAndSkills, ref activePerks);

                foreach (SkillUnlockerButton skillUnlockerButton in _ethelSkillUnlockerButtons)
                    skillUnlockerButton.UpdateSelf(ref unlockedPerksAndSkills);
            }
            else if (stats.Key == Nema.GlobalKey)
            {
                foreach (PerkButton perkButton in _nemaPerkButtons)
                    perkButton.UpdateSelf(ref unlockedPerksAndSkills, ref activePerks);

                foreach (SkillUnlockerButton skillUnlockerButton in _nemaSkillUnlockerButtons)
                    skillUnlockerButton.UpdateSelf(ref unlockedPerksAndSkills);
            }
            
            unlockedPerksAndSkills.Dispose();
        }

        public void SetOpen(bool open)
        {
            SetCanvasGroup(canvasGroup, open);
            if (open)
                CheckPerks();
        }

        private static void SetCanvasGroup(CanvasGroup canvasGroup, bool active)
        {
            canvasGroup.alpha = active ? 1 : 0;
            canvasGroup.interactable = active;
            canvasGroup.blocksRaycasts = active;
        }
    }
}