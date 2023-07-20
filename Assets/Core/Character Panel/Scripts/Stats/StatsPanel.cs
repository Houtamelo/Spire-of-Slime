using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.Combat.Scripts.Managers;
using Core.Game_Manager.Scripts;
using Core.Main_Characters.Ethel.Combat;
using Core.Main_Characters.Nema.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using Core.Utils.Math;
using Core.Utils.Patterns;
using KGySoft.CoreLibraries;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using TMPro;
using UnityEngine;
using Save = Core.Save_Management.SaveObjects.Save;

namespace Core.Character_Panel.Scripts.Stats
{
    public sealed class StatsPanel : Singleton<StatsPanel>
    {
        private static readonly StringBuilder Builder = new();
        private static readonly Dictionary<CleanString, GeneralStat> EthelVariables = Enum<GeneralStat>.GetValues().ToDictionary(stat => VariablesName.StatName(Ethel.GlobalKey, stat), stat => stat);
        private static readonly Dictionary<CleanString, GeneralStat> NemaVariables = Enum<GeneralStat>.GetValues().ToDictionary(stat => VariablesName.StatName(Nema.GlobalKey,   stat), stat => stat);

        [OdinSerialize, Required]
        private readonly Dictionary<GeneralStat, TMP_Text> _generalStatTmps;
        
        [SerializeField, SceneObjectsOnly, Required]
        private TMP_Text baseDamageTmp;
        
        [OdinSerialize, AssetsOnly, Required]
        private readonly StatUpgradeButton _statUpgradeButtonPrefab;
        
        [OdinSerialize, SceneObjectsOnly, Required]
        private readonly Transform _primaryButtonsParent, _secondaryButtonsParent;

        [SerializeField, Required, SceneObjectsOnly]
        private CanvasGroup canvasGroup;

        [SerializeField, Required, SceneObjectsOnly]
        private CharacterMenuManager characterMenu;

        [SerializeField, Required, SceneObjectsOnly]
        private AudioSource statButtonPointerEnterSound, statButtonPointerClickSound, statButtonInvalidClickSound;

        private readonly List<StatUpgradeButton> _primaryButtons = new(), _secondaryButtons = new();

        private void Start()
        {
            characterMenu.SelectedCharacter.Changed += OnSelectedCharacterChanged;
            characterMenu.SelectedCharacter.Changed += CheckPrimaryUpgradeButtons;
            characterMenu.SelectedCharacter.Changed += CheckSecondaryUpgradeButtons;
            Save.IntChanged += OnIntChanged;
            SetOpen(false);
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            Save.IntChanged -= OnIntChanged;
            if (characterMenu == null)
                return;
            
            characterMenu.SelectedCharacter.Changed -= OnSelectedCharacterChanged;
            characterMenu.SelectedCharacter.Changed -= CheckPrimaryUpgradeButtons;
            characterMenu.SelectedCharacter.Changed -= CheckSecondaryUpgradeButtons;
        }

        private void OnIntChanged(CleanString variableName, int oldValue, int newValue)
        {
            Option<IReadonlyCharacterStats> character = characterMenu.SelectedCharacter.AsOption();
            if (character.TrySome(out IReadonlyCharacterStats stats) == false)
                return;
            
            if (false == 
                ((EthelVariables.TryGetValue(variableName, out GeneralStat stat) && stats.Key == Ethel.GlobalKey) || (NemaVariables.TryGetValue(variableName, out stat) && stats.Key == Nema.GlobalKey)))
                return;

            if (stat is GeneralStat.DamageLower or GeneralStat.DamageUpper)
            {
                (int lower, int upper) damage = character.Value.GetDamage();
                baseDamageTmp.text = damage.ToDamageRangeFormat();
            }
            else if (_generalStatTmps.TryGetValue(stat, out TMP_Text tmp))
            {
                if (stat is GeneralStat.OrgasmLimit or GeneralStat.OrgasmCount)
                    tmp.text = Builder.Override(stats.OrgasmCount.ToString(), " / ", stats.OrgasmLimit.ToString()).ToString();
                else
                    tmp.text = stat.AltFormat(newValue);
            }
        }
        private void OnEnable()
        {
            OnSelectedCharacterChanged(characterMenu.SelectedCharacter.AsOption());
            CheckPrimaryUpgradeButtons(characterMenu.SelectedCharacter.AsOption());
            CheckSecondaryUpgradeButtons(characterMenu.SelectedCharacter.AsOption());
        }

        private void OnSelectedCharacterChanged(Option<IReadonlyCharacterStats> character)
        {
            if (character.IsNone)
            {
                foreach (TMP_Text tmp in _generalStatTmps.Values)
                    tmp.text = string.Empty;

                baseDamageTmp.text = string.Empty;
                return;
            }

            IReadonlyCharacterStats stats = character.Value;

            foreach ((GeneralStat stat, TMP_Text tmp) in _generalStatTmps)
            {
                int value = stats.GetValue(stat);
                if (stat is GeneralStat.OrgasmLimit or GeneralStat.OrgasmCount)
                    tmp.text = Builder.Override(stats.OrgasmCount.ToString(), " / ", stats.OrgasmLimit.ToString()).ToString();
                else
                    tmp.text = stat.AltFormat(value);
            }

            (int lower, int upper) damage = stats.GetDamage();
            baseDamageTmp.text = damage.ToDamageRangeFormat();
        }

        private void CheckPrimaryUpgradeButtons(Option<IReadonlyCharacterStats> character)
        {
            if (character.IsNone || character.Value.AvailablePrimaryPoints <= 0)
            {
                foreach (StatUpgradeButton button in _primaryButtons) 
                    button.ResetMe();
                
                return;
            }

            IReadonlyCharacterStats stats = character.Value;
            int tierToCheck = stats.GetUsedPrimaryPoints() + 1;
            ReadOnlySpan<PrimaryUpgrade> options = stats.GetPrimaryUpgradeOptions(tierToCheck);

            for (int i = _primaryButtons.Count; i < options.Length; i++) 
                CreatePrimaryButton();

            int index = 0;
            for (; index < options.Length; index++)
            {
                PrimaryUpgrade option = options[index];
                StatUpgradeButton button = _primaryButtons[index];
                
                button.Initialize(option, stats);
            }
            
            for (; index < _primaryButtons.Count; index++)
                _primaryButtons[index].ResetMe();
        }
        
        private void CheckSecondaryUpgradeButtons(Option<IReadonlyCharacterStats> character)
        {
            if (character.IsNone || character.Value.AvailableSecondaryPoints <= 0)
            {
                foreach (StatUpgradeButton button in _secondaryButtons) 
                    button.ResetMe();
                
                return;
            }
            
            IReadonlyCharacterStats stats = character.Value;
            int tierToCheck = stats.GetUsedSecondaryPoints() + 1;
            ReadOnlySpan<SecondaryUpgrade> options = stats.GetSecondaryUpgradeOptions(tierToCheck);
            
            for (int i = _secondaryButtons.Count; i < options.Length; i++) 
                CreateSecondaryButton();
            
            int index = 0;
            for (; index < options.Length; index++)
            {
                SecondaryUpgrade option = options[index];
                StatUpgradeButton button = _secondaryButtons[index];
                
                button.Initialize(option, stats);
            }
            
            for (; index < _secondaryButtons.Count; index++)
                _secondaryButtons[index].ResetMe();
        }

        private void CreatePrimaryButton()
        {
            StatUpgradeButton button = Instantiate(_statUpgradeButtonPrefab, _primaryButtonsParent);
            button.AssignAudioSources(onPointerEnter: statButtonPointerEnterSound, onPointerClick: statButtonPointerClickSound, statButtonInvalidClickSound);
            _primaryButtons.Add(button);
        }
        
        private void CreateSecondaryButton()
        {
            StatUpgradeButton button = Instantiate(_statUpgradeButtonPrefab, _secondaryButtonsParent);
            button.AssignAudioSources(onPointerEnter: statButtonPointerEnterSound, onPointerClick: statButtonPointerClickSound, statButtonInvalidClickSound);
            _secondaryButtons.Add(button);
        }

        public bool UpgradePrimary(PrimaryUpgrade primaryUpgrade)
        {
            if (Save.AssertInstance(out Save save) == false)
                return false;

            if (CombatManager.Instance.IsSome)
            {
                if (WorldTextCueManager.AssertInstance(out WorldTextCueManager cueManager))
                    cueManager.Show(new WorldCueOptions("I can't do that in combat.", 35f, transform.position, Color.red, 1f, 0.5f, Vector3.zero, HorizontalAlignmentOptions.Center, true));

                return false;
            }

            Option<IReadonlyCharacterStats> character = characterMenu.SelectedCharacter.AsOption();
            if (character.IsNone)
            {
                Debug.LogWarning($"Trying to increase upgrade {primaryUpgrade} with no character selected");
                return false;
            }
            
            save.IncreasePrimaryUpgrade(character.Value.Key, primaryUpgrade);
            CheckPrimaryUpgradeButtons(characterMenu.SelectedCharacter.AsOption());
            return true;
        }

        public bool UpgradeSecondary(SecondaryUpgrade secondaryUpgrade)
        {
            if (Save.AssertInstance(out Save save) == false)
                return false;
            
            if (CombatManager.Instance.IsSome)
            {
                if (WorldTextCueManager.AssertInstance(out WorldTextCueManager cueManager))
                    cueManager.Show(new WorldCueOptions("You can't do that in combat.", size: 35f, transform.position, Color.red, stayDuration: 1f, fadeDuration: 0.5f, speed: Vector3.zero, HorizontalAlignmentOptions.Center, stopOthers: true));

                return false;
            }
            
            Option<IReadonlyCharacterStats> character = characterMenu.SelectedCharacter.AsOption();
            if (character.IsNone)
            {
                Debug.LogWarning($"Trying to increase upgrade {secondaryUpgrade} with no character selected");
                return false;
            }
            
            save.IncreaseSecondaryUpgrade(character.Value.Key, secondaryUpgrade);
            CheckSecondaryUpgradeButtons(characterMenu.SelectedCharacter.AsOption());
            return true;
        }

        public void SetOpen(bool open)
        {
            canvasGroup.alpha = open ? 1 : 0;
            canvasGroup.interactable = open;
            canvasGroup.blocksRaycasts = open;
            
            if (open)
                CheckEverything();
        }

        private void CheckEverything()
        {
            Option<IReadonlyCharacterStats> character = characterMenu.SelectedCharacter.AsOption();
            CheckPrimaryUpgradeButtons(character);
            CheckSecondaryUpgradeButtons(character);
            OnSelectedCharacterChanged(character);
        }
    }
}