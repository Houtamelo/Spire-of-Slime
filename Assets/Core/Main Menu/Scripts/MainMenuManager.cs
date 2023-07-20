using System;
using System.Globalization;
using Core.Audio.Scripts;
using Core.Game_Manager.Scripts;
using Core.Pause_Menu.Scripts;
using Core.Save_Management;
using Core.Save_Management.SaveObjects;
using Core.Utils.Patterns;
using DG.Tweening;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Main_Menu.Scripts
{
    public sealed class MainMenuManager : Singleton<MainMenuManager>
    {
        private const string Discord = "https://discord.gg/Cacam7yuqR";
        private const string Twitter = "https://twitter.com/Houtamelo";
        
        private const string IronGauntletEasterPlayerPref = "IronGauntletEasterCount";
        private const float EasterEggTextSpeed = 30f;

        [SerializeField, Required, SceneObjectsOnly]
        private Button twitterButton, discordButton;

        [OdinSerialize, SceneObjectsOnly, Required]
        private readonly Button _exitButton;

        [OdinSerialize, SceneObjectsOnly, Required]
        private readonly Toggle _newGamePanelToggle;

        [OdinSerialize, SceneObjectsOnly, Required]
        private readonly Button _newGameButton;

        [OdinSerialize, SceneObjectsOnly, Required]
        private readonly GameObject _newGamePanel;

        [OdinSerialize, SceneObjectsOnly, Required]
        private readonly GameObject _areYouSurePanel;

        [OdinSerialize, SceneObjectsOnly, Required]
        private readonly Button _areYouSureButton;

        [OdinSerialize, SceneObjectsOnly, Required]
        private readonly Button _areYouSureCancelButton;

        [OdinSerialize, SceneObjectsOnly, Required]
        private readonly TMP_InputField _saveNameInputField;

        [OdinSerialize, SceneObjectsOnly, Required]
        private readonly TMP_Text _easterEggText;

        [OdinSerialize, SceneObjectsOnly, Required]
        private readonly Button _ironGauntletButton;

        [OdinSerialize, SceneObjectsOnly, Required]
        private readonly Button _pauseMenuButton;

        [OdinSerialize, SceneObjectsOnly, Required]
        private readonly Toggle _loadGamePanelToggle;

        [OdinSerialize, SceneObjectsOnly, Required]
        private readonly GameObject _loadPanel;

        [OdinSerialize, SceneObjectsOnly, Required]
        private readonly GameObject _blur;

        [OdinSerialize, SceneObjectsOnly, Required]
        private readonly Toggle _creditsPanelToggle;

        [OdinSerialize, SceneObjectsOnly, Required]
        private readonly GameObject _creditsPanel;

        [OdinSerialize, SceneObjectsOnly, Required]
        private readonly Button _closeCreditsPanelButton;

        [OdinSerialize, SceneObjectsOnly, Required]
        private readonly ToggleGroup _toggleGroup;

        [OdinSerialize, SceneObjectsOnly, Required]
        private readonly AudioSource _invalidNameAudioSource;
        
        private void Start()
        {
            discordButton.onClick.AddListener(() => Application.OpenURL(Discord));
            twitterButton.onClick.AddListener(() => Application.OpenURL(Twitter));
            
            _newGamePanelToggle.onValueChanged.AddListener(_newGamePanel.SetActive);
            _newGamePanelToggle.onValueChanged.AddListener(_blur.SetActive);
            _saveNameInputField.onValueChanged.AddListener(OnSaveNameInputFieldChanged);
            _areYouSureButton.onClick.AddListener(AreYouSureButtonClicked);
            _areYouSureCancelButton.onClick.AddListener(CloseAreYouSurePanel);
            
            _ironGauntletButton.onClick.AddListener(IronGauntletButtonPressed);
            _newGameButton.onClick.AddListener(NewGame);
            
            _loadGamePanelToggle.onValueChanged.AddListener(_loadPanel.SetActive);
            _loadGamePanelToggle.onValueChanged.AddListener(_blur.SetActive);

            _pauseMenuButton.onClick.AddListener(OpenSettings);

            _creditsPanelToggle.onValueChanged.AddListener(_creditsPanel.SetActive);
            _closeCreditsPanelButton.onClick.AddListener(CloseCreditsPanel);

            _exitButton.onClick.AddListener(Quit);
        }

        private void CloseAreYouSurePanel()
        {
            _areYouSurePanel.SetActive(false);
        }

        private void OnSaveNameInputFieldChanged([NotNull] string text)
        {
            Span<char> easterEggReadable = stackalloc char[text.Length];
            int easterEggLength = 0;

            for (int i = 0; i < text.AsSpan().Length; i++)
            {
                char c = text.AsSpan()[i];
                if (char.IsLetter(c))
                {
                    easterEggReadable[i] = char.ToLowerInvariant(c);
                    easterEggLength++;
                }
            }
            
            string formattedString = easterEggReadable[..easterEggLength].ToString();
            if (formattedString == "alexa")
                Application.Quit();

            if (Easters.TryGetNameEasters(formatted: formattedString, easter: out string easter))
            {
                _easterEggText.text = "";
                _easterEggText.DOText(endValue: easter, duration: EasterEggTextSpeed).SetSpeedBased();
            }
            
            _saveNameInputField.SetTextWithoutNotify(GetSaveNameFormatted(text));
        }

        private void CloseCreditsPanel() => _creditsPanelToggle.isOn = false;

        private void OpenSettings()
        {
            if (PauseMenuManager.AssertInstance(out PauseMenuManager pauseMenuManager))
                pauseMenuManager.Open();
        }

        private void NewGame()
        {
            if (SaveFilesManager.AssertInstance(out SaveFilesManager saveFilesManager) == false || GameManager.AssertInstance(out GameManager gameManager) == false)
                return;

            string saveName = GetSaveNameFormatted(_saveNameInputField.text);
            if (string.IsNullOrEmpty(saveName) || saveName == ((TMP_Text)_saveNameInputField.placeholder).text.ToLowerInvariant())
            {
                _easterEggText.text = string.Empty;
                _easterEggText.DOText(endValue: "Please enter a save name", duration: EasterEggTextSpeed).SetSpeedBased();
                _invalidNameAudioSource.Play();
                return;
            }

            foreach (SaveRecord record in saveFilesManager.GetRecords)
            {
                if (GetSaveNameFormatted(record.Name) == saveName)
                {
                    _areYouSurePanel.SetActive(true);
                    _invalidNameAudioSource.Play();
                    return;
                }
            }

            if (GlobalSounds.AssertInstance(out GlobalSounds globalSounds))
                globalSounds.NewGame.Play();
            
            gameManager.NewGameFromMainMenu(saveName);
        }

        private void AreYouSureButtonClicked()
        {
            if (GameManager.AssertInstance(out GameManager gameManager) == false)
                return;

            string saveName = _saveNameInputField.text;
            if (string.IsNullOrEmpty(saveName))
                saveName = $"New Save{DateTime.Now.ToString(CultureInfo.InvariantCulture).Replace(oldValue: "/", newValue: "-").Replace(oldValue: ":",newValue: "-")}";

            _areYouSurePanel.SetActive(false);
            
            if (GlobalSounds.AssertInstance(out GlobalSounds globalSounds))
                globalSounds.NewGame.Play();
            
            gameManager.NewGameFromMainMenu(saveName);
        }

        [UsedImplicitly]
        public void BackgroundButtonClicked()
        {
            _toggleGroup.SetAllTogglesOff();
            _areYouSurePanel.SetActive(false);
        }

        private void IronGauntletButtonPressed()
        {
            int count = PlayerPrefs.GetInt(IronGauntletEasterPlayerPref, defaultValue: 0);
            string easter = Easters.GetIronGauntletEaster(index: count);
            _easterEggText.text = "";
            _easterEggText.DOText(endValue: easter, duration: EasterEggTextSpeed).SetSpeedBased(); 
            count++;
            PlayerPrefs.SetInt(key: IronGauntletEasterPlayerPref, value: count);
        }

        [NotNull]
        private static string GetSaveNameFormatted([NotNull] string text)
        {
            Span<char> saveName = stackalloc char[text.Length];
            int saveNameLength = 0;
            for (int i = 0; i < text.AsSpan().Length; i++)
            {
                char c = text.AsSpan()[i];
                if (char.IsLetterOrDigit(c))
                {
                    saveName[i] = char.ToLowerInvariant(c);
                    saveNameLength++;
                }
            }

            return saveName[..saveNameLength].ToString();
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
        }
    }
}