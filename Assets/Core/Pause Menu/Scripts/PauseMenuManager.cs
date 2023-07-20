using System;
using System.Collections.Generic;
using System.Linq;
using Core.Game_Manager.Scripts;
using Core.Localization.Scripts;
using Core.Save_Management;
using Core.Utils.Extensions;
using Core.Utils.Handlers;
using Core.Utils.Patterns;
using Core.Visual_Novel.Scripts;
using KGySoft.CoreLibraries;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Core.Pause_Menu.Scripts
{
    public delegate void LanguageDelegate(Language language);
    public sealed class PauseMenuManager : Singleton<PauseMenuManager>
    {
        private static readonly LocalizedText[] ScreenModeTranslations = {
            new("screen-mode_ExclusiveFullScreen"),
            new("screen-mode_FullScreenWindow"),
            new("screen-mode_MaximizedWindow"),
            new("screen-mode_Windowed")
        };
        
        private static readonly LocalizedText[] SkillOverlayModeTranslations = {
            new("skill-overlay-mode_Auto"),
            new("skill-overlay-mode_WaitForInput")
        };
        
        public static readonly ValueHandler<bool> TypeWriterSoundHandler = new();
        public static readonly ValueHandler<float> TextDelayHandler = new();
        public static readonly ValueHandler<float> CombatAnimationSpeedHandler = new();
        public static readonly ValueHandler<int> CombatTickRateHandler = new();
        public static readonly ValueHandler<SkillOverlayMode> SkillOverlayModeHandler = new();
        public static readonly ValueHandler<float> SkillOverlayAutoDurationHandler = new();

        [OdinSerialize, Required]
        private readonly AudioMixer _audioMixer;

        [OdinSerialize, SceneObjectsOnly, Required]
        private readonly Slider _mainVolumeSlider, _musicVolumeSlider, _sfxVolumeSlider, _voiceVolumeSlider;

        [OdinSerialize, SceneObjectsOnly, Required]
        private readonly TMP_Dropdown _resolutionDropdown, _screenModeDropdown, _languageDropdown;

        [OdinSerialize, SceneObjectsOnly, Required]
        private readonly Toggle _vsyncToggle, _typeWriterSoundToggle;

        [SerializeField, Required]
        private Slider textDelaySlider;
        
        [SerializeField, Required]
        private TMP_Text textDelayNumberTmp;
        
        [SerializeField, Required]
        private Slider targetFrameRateSlider;

        [SerializeField, Required]
        private TMP_Text frameRateNumberTmp;

        [SerializeField, Required]
        private Slider combatAnimationSpeedSlider;
        
        [SerializeField, Required]
        private TMP_Text combatAnimationSpeedNumberTmp;

        [SerializeField, Required]
        private Slider combatTickRateSlider;
        
        [SerializeField, Required]
        private TMP_Text combatTickRateNumberTmp;
        
        [SerializeField, Required]
        private TMP_Dropdown skillOverlayModeDropdown;
        
        [SerializeField, Required]
        private Slider skillOverlayAutoDurationSlider;
        
        [SerializeField, Required]
        private TMP_Text skillOverlayAutoDurationNumberTmp;

        [OdinSerialize, SceneObjectsOnly, Required]
        private readonly CanvasGroup _canvasGroup;

        [OdinSerialize, SceneObjectsOnly, Required]
        private readonly Button _returnButton, _mainMenuButton, _saveAndQuitButton;
        
        private Option<float> _timeScaleBeforePause = Option<float>.Some(1f);
        private bool _isOpen = true;

        public static Language CurrentLanguage { get; private set; } = Language.English;
        public static event LanguageDelegate LanguageChanged;

        protected override void Awake()
        {
            base.Awake();
            Close();
        }
        
        private void Start()
        {
            _mainVolumeSlider.onValueChanged.AddListener(SetMainVolumePercentage);
            _musicVolumeSlider.onValueChanged.AddListener(SetMusicVolumePercentage);
            _sfxVolumeSlider.onValueChanged.AddListener(SetSfxVolumePercentage);
            _voiceVolumeSlider.onValueChanged.AddListener(SetVoiceVolumePercentage);
            
            _vsyncToggle.onValueChanged.AddListener(SetVsync);
            targetFrameRateSlider.onValueChanged.AddListener(SetTargetFrameRateViaSlider);

            _returnButton.onClick.AddListener(Close);
            _mainMenuButton.onClick.AddListener(LoadMainMenuViaPauseMenu);
            _saveAndQuitButton.onClick.AddListener(SaveAndQuit);

            CreateScreenModeOptions();
            _screenModeDropdown.onValueChanged.AddListener(SetFullScreenModeByDropdown);
            LanguageChanged += CreateScreenModeOptions;

            CreateResolutionOptions(extra: Screen.currentResolution);
            _resolutionDropdown.onValueChanged.AddListener(SetResolutionByDropdown);

            if (ResolutionManager.AssertInstance(out ResolutionManager resolutionManager))
            {
                resolutionManager.ScreenModeChanged += UpdateScreenModeDropdown;
                resolutionManager.ResolutionChanged += UpdateResolutionDropdown;
                resolutionManager.ResolutionChanged += UpdateTargetFrameRateSlider;
            }

            _languageDropdown.ClearOptions();
            _languageDropdown.AddOptions(Enum<Language>.GetNames().ToList());
            _languageDropdown.onValueChanged.AddListener(SetLanguageByDropdown);
            LanguageChanged += UpdateLanguageDropdownValue;

            textDelaySlider.onValueChanged.AddListener(SetTextDelay);
            TextDelayHandler.Changed += OnTextDelayChanged;

            _typeWriterSoundToggle.onValueChanged.AddListener(SetTypeWriterSound);
            TypeWriterSoundHandler.Changed += OnTypeWriterSoundChanged;

            combatAnimationSpeedSlider.onValueChanged.AddListener(SetCombatAnimationSpeedViaSlider);
            CombatAnimationSpeedHandler.Changed += OnCombatAnimationSpeedChanged;

            combatTickRateSlider.onValueChanged.AddListener(SetCombatTickRateViaSlider);
            CombatTickRateHandler.Changed += OnCombatTickRateChanged;
            
            CreateSkillOverlayModeOptions();
            skillOverlayModeDropdown.onValueChanged.AddListener(SetSkillOverlayModeByDropdown);
            SkillOverlayModeHandler.Changed += OnSkillOverlayModeChanged;
            LanguageChanged += CreateSkillOverlayModeOptions;

            skillOverlayAutoDurationSlider.onValueChanged.AddListener(SetSkillOverlayAutoDurationViaSlider);
            SkillOverlayAutoDurationHandler.Changed += OnSkillOverlayAutoDurationChanged;

            LoadSettingsFromPlayerPrefs();
            
            if (InputManager.AssertInstance(out InputManager inputManager))
                inputManager.PerformedActionsCallbacks[InputEnum.TogglePauseMenu].Add(OnPauseAction);
        }

        protected override void OnDestroy()
        {
            if (InputManager.Instance.TrySome(out InputManager inputManager))
                inputManager.PerformedActionsCallbacks[InputEnum.TogglePauseMenu].Remove(OnPauseAction);

            if (ResolutionManager.Instance.TrySome(out ResolutionManager resolutionManager))
            {
                resolutionManager.ScreenModeChanged -= UpdateScreenModeDropdown;
                resolutionManager.ResolutionChanged -= UpdateResolutionDropdown;
                resolutionManager.ResolutionChanged -= UpdateTargetFrameRateSlider;
            }
            
            LanguageChanged -= UpdateLanguageDropdownValue;
            LanguageChanged -= CreateScreenModeOptions;
            LanguageChanged -= CreateSkillOverlayModeOptions;
            TextDelayHandler.Changed -= OnTextDelayChanged;
            TypeWriterSoundHandler.Changed -= OnTypeWriterSoundChanged;
            CombatAnimationSpeedHandler.Changed -= OnCombatAnimationSpeedChanged;
            CombatTickRateHandler.Changed -= OnCombatTickRateChanged;
            SkillOverlayModeHandler.Changed -= OnSkillOverlayModeChanged;
            SkillOverlayAutoDurationHandler.Changed -= OnSkillOverlayAutoDurationChanged;

            base.OnDestroy();
        }

        private void OnPauseAction()
        {
            GameObject currentCurrentSelectedGameObject = EventSystem.current.currentSelectedGameObject;
            if (currentCurrentSelectedGameObject != null && currentCurrentSelectedGameObject.TryGetComponent<TMP_InputField>(out _))
                return;
            
            if (_canvasGroup.interactable)
                Close();
            else
                Open();
        }

        private void LoadSettingsFromPlayerPrefs()
        {
            {
                (string key, float percentage) = PlayerPrefsReferences.MainVolume;
                percentage = PlayerPrefs.GetFloat(key: key, defaultValue: percentage);
                SetMainVolumePercentage(percentage: percentage);
            }
            {
                (string key, float percentage) = PlayerPrefsReferences.MusicVolume;
                percentage = PlayerPrefs.GetFloat(key: key, defaultValue: percentage);
                SetMusicVolumePercentage(percentage: percentage);
            }
            {
                (string key, float percentage) = PlayerPrefsReferences.SfxVolume;
                percentage = PlayerPrefs.GetFloat(key: key, defaultValue: percentage);
                SetSfxVolumePercentage(percentage: percentage);
            }
            {
                (string key, float percentage) = PlayerPrefsReferences.VoiceVolume;
                percentage = PlayerPrefs.GetFloat(key: key, defaultValue: percentage);
                SetVoiceVolumePercentage(percentage: percentage);
            }
            {
                (string key, FullScreenMode mode) = PlayerPrefsReferences.ScreenMode;
                int memorizedMode = PlayerPrefs.GetInt(key, defaultValue: (int)mode);
                SetFullScreenMode((FullScreenMode) memorizedMode);
            }
            {
                (string key, string defaultResolution) = PlayerPrefsReferences.Resolution;
                string memorizedResolution = PlayerPrefs.GetString(key: key, defaultValue: defaultResolution);
                Result<Resolution> parsedResolution = memorizedResolution.ParseResolution();
                
                if (parsedResolution.IsOk)
                {
                    SetResolution(parsedResolution.Value);
                }
                else
                {
                    Debug.LogWarning(parsedResolution.Reason);
                    
                    Result<Resolution> defaultOption = defaultResolution.ParseResolution();
                    if (defaultOption.IsOk)
                        SetResolution(defaultOption.Value);
                    else
                        Debug.LogWarning(defaultOption.Reason);
                }
            }
            {
                (string key, bool value) = PlayerPrefsReferences.Vsync;
                bool memorizedVsync = PlayerPrefs.GetInt(key, defaultValue: value ? 1 : 0) == 1;
                SetVsync(active: memorizedVsync);
            }
            {
                (string key, bool value) = PlayerPrefsReferences.TypeWriterSound;
                bool memorizedTypeWriterSound = PlayerPrefs.GetInt(key, defaultValue: value ? 1 : 0) == 1;
                SetTypeWriterSound(active: memorizedTypeWriterSound);
            }
            {
                (string key, float delayInSeconds) = PlayerPrefsReferences.TextDelay;
                float memorizedDelayInSeconds = PlayerPrefs.GetFloat(key, defaultValue: delayInSeconds);
                SetTextDelay(memorizedDelayInSeconds);
            }
            {
                (string key, int value) = PlayerPrefsReferences.TargetFrameRate;
                int memorizedTargetFrameRate = PlayerPrefs.GetInt(key, defaultValue: value);
                SetTargetFrameRate(memorizedTargetFrameRate);
            }
            {
                (string key, float speedMultiplier) = PlayerPrefsReferences.CombatAnimationSpeed;
                float memorizedSpeedMultiplier = PlayerPrefs.GetFloat(key, defaultValue: speedMultiplier);
                memorizedSpeedMultiplier = PlayerPrefsReferences.ClampCombatAnimationSpeed(memorizedSpeedMultiplier);
                SetCombatAnimationSpeed(memorizedSpeedMultiplier);
            }
            {
                (string key, int tickRate) = PlayerPrefsReferences.CombatTickRate;
                int memorizedTickRate = PlayerPrefs.GetInt(key, defaultValue: tickRate);
                memorizedTickRate = PlayerPrefsReferences.ClampCombatTickRate(memorizedTickRate);
                SetCombatTickRate(memorizedTickRate);
            }
            {
                (string key, int mode) = PlayerPrefsReferences.SkillOverlayMode;
                int memorizedMode = PlayerPrefs.GetInt(key, defaultValue: mode);
                SetSkillOverlayMode((SkillOverlayMode) memorizedMode);
            }
            {
                (string key, float duration) = PlayerPrefsReferences.SkillOverlayAutoDuration;
                float memorizedDuration = PlayerPrefs.GetFloat(key, defaultValue: duration);
                memorizedDuration = PlayerPrefsReferences.ClampSkillOverlayAutoDuration(memorizedDuration);
                SetSkillOverlayAutoDuration(memorizedDuration);
            }
            {
                (string key, int value) = PlayerPrefsReferences.Language;
                int memorizedLanguage = PlayerPrefs.GetInt(key, defaultValue: value);
                SetLanguage((Language) memorizedLanguage);
            }
        }

        private void CreateSkillOverlayModeOptions()
        {
            skillOverlayModeDropdown.ClearOptions();
            List<string> options = SkillOverlayModeTranslations.Select(t => t.Translate().GetText()).ToList();
            skillOverlayModeDropdown.AddOptions(options);
        }

        private void CreateSkillOverlayModeOptions(Language _) => CreateSkillOverlayModeOptions();

        private void OnSkillOverlayModeChanged(SkillOverlayMode mode)
        {
            skillOverlayModeDropdown.SetValueWithoutNotify((int)mode);
            switch (mode)
            {
                case SkillOverlayMode.Auto:         skillOverlayAutoDurationSlider.gameObject.SetActive(true); break;
                case SkillOverlayMode.WaitForInput: skillOverlayAutoDurationSlider.gameObject.SetActive(false); break;
                default:                            throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
            
            PlayerPrefs.SetInt(PlayerPrefsReferences.SkillOverlayMode.key, (int)mode);
        }

        private void SetSkillOverlayMode(SkillOverlayMode overlayMode) => SkillOverlayModeHandler.SetValue(overlayMode);

        private void SetSkillOverlayModeByDropdown(int index) => SetSkillOverlayMode((SkillOverlayMode) index);

        private void OnSkillOverlayAutoDurationChanged(float duration)
        {
            skillOverlayAutoDurationSlider.SetValueWithoutNotify(duration * 10f);
            skillOverlayAutoDurationNumberTmp.text = $"{duration:0.0}s";
            PlayerPrefs.SetFloat(PlayerPrefsReferences.SkillOverlayAutoDuration.key, duration);
        }

        private void SetSkillOverlayAutoDurationViaSlider(float value) => SetSkillOverlayAutoDuration(value / 10f);

        private void SetSkillOverlayAutoDuration(float value) => SkillOverlayAutoDurationHandler.SetValue(value);

        private void SetLanguage(Language language)
        {
            PlayerPrefs.SetInt(key: PlayerPrefsReferences.Language.key, value: (int) language);
            CurrentLanguage = language;
            LanguageChanged?.Invoke(language);
        }

        private void SetLanguageByDropdown(int index) => SetLanguage((Language) index);
        private void UpdateLanguageDropdownValue(Language language) => _languageDropdown.SetValueWithoutNotify((int) language);

        private void SetVsync(bool active)
        {
            QualitySettings.vSyncCount = active ? 1 : 0;
            _vsyncToggle.SetIsOnWithoutNotify(value: active);
            PlayerPrefs.SetInt(key: PlayerPrefsReferences.Vsync.key, value: active ? 1 : 0);
        }

        private void SetVolumePercentageWithoutNotify(string key, float percentage)
        {
            float decibels = VolumePercentageToDecibels(volume: percentage);
            _audioMixer.SetFloat(name: key, value: decibels);
            PlayerPrefs.SetFloat(key: key, value: percentage);
        }

        private void SetMainVolumePercentage(float percentage)
        {
            _mainVolumeSlider.SetValueWithoutNotify(input: percentage);
            (string key, float _) = PlayerPrefsReferences.MainVolume;
            SetVolumePercentageWithoutNotify(key: key, percentage: percentage);
            PlayerPrefs.SetFloat(key: key, value: percentage);
        }

        private void SetMusicVolumePercentage(float percentage)
        {
            _musicVolumeSlider.SetValueWithoutNotify(input: percentage);
            (string key, float _) = PlayerPrefsReferences.MusicVolume;
            SetVolumePercentageWithoutNotify(key: key, percentage: percentage);
            PlayerPrefs.SetFloat(key: key, value: percentage);
        }

        private void SetSfxVolumePercentage(float percentage)
        {
            _sfxVolumeSlider.SetValueWithoutNotify(input: percentage);
            (string key, float _) = PlayerPrefsReferences.SfxVolume;
            SetVolumePercentageWithoutNotify(key: key, percentage: percentage);
            PlayerPrefs.SetFloat(key: key, value: percentage);
        }

        private void SetVoiceVolumePercentage(float percentage)
        {
            _voiceVolumeSlider.SetValueWithoutNotify(input: percentage);
            (string key, float _) = PlayerPrefsReferences.VoiceVolume;
            SetVolumePercentageWithoutNotify(key: key, percentage: percentage);
            PlayerPrefs.SetFloat(key: key, value: percentage);
        }

        private void SetFullScreenMode(FullScreenMode mode)
        {
            if (ResolutionManager.AssertInstance(out ResolutionManager resolutionManager) == false)
                return;
            
            resolutionManager.ChangeScreenMode(mode: mode);
            _screenModeDropdown.SetValueWithoutNotify((int) mode);
            PlayerPrefs.SetString(key: PlayerPrefsReferences.ScreenMode.key, value: Enum<FullScreenMode>.ToString(mode));
        }

        private void SetFullScreenModeByDropdown(int index)
        {
            if (ResolutionManager.AssertInstance(out ResolutionManager resolutionManager) == false)
                return;
            
            resolutionManager.ChangeScreenMode(mode: (FullScreenMode) index);
            PlayerPrefs.SetString(key: PlayerPrefsReferences.ScreenMode.key, value: Enum<FullScreenMode>.ToString((FullScreenMode) index));
        }

        private void SetResolution(Resolution resolution)
        {
            if (ResolutionManager.AssertInstance(out ResolutionManager resolutionManager) == false)
                return;
            
            resolutionManager.ChangeResolution(resolution: resolution);
            PlayerPrefs.SetString(key: PlayerPrefsReferences.Resolution.key, value: resolution.CompactFormat());
        }

        private void SetResolutionByDropdown(int index)
        {
            Option<ResolutionManager> resolutionManager = ResolutionManager.Instance;
            if (resolutionManager.IsNone)
            {
                Debug.LogWarning("ResolutionManager is not available.", this);
                return;
            }
            
            Result<Resolution> parsedResolution = _resolutionDropdown.options[index].text.ParseResolution();
            if (parsedResolution.IsOk)
            {
                resolutionManager.Value.ChangeResolution(resolution: parsedResolution.Value);
                PlayerPrefs.SetString(key: PlayerPrefsReferences.Resolution.key, value: parsedResolution.Value.CompactFormat());
            }
            else
            {
                Debug.LogWarning(parsedResolution.Reason, _resolutionDropdown);
            }
        }

        private void SetTypeWriterSound(bool active) => TypeWriterSoundHandler.SetValue(active);

        private void OnTypeWriterSoundChanged(bool active)
        {
            _typeWriterSoundToggle.SetIsOnWithoutNotify(value: active);
            PlayerPrefs.SetInt(PlayerPrefsReferences.TypeWriterSound.key, active ? 1 : 0);
        }

        private void SetTextDelay(float value) => TextDelayHandler.SetValue(value);

        private void OnTextDelayChanged(float delayInSeconds)
        {
            textDelayNumberTmp.text = $"{delayInSeconds:0.00}s";
            textDelaySlider.SetValueWithoutNotify(delayInSeconds);
            PlayerPrefs.SetFloat(PlayerPrefsReferences.TextDelay.key, delayInSeconds);
        }

        private void SetTargetFrameRate(int value)
        {
            frameRateNumberTmp.text = value.ToString("0");
            targetFrameRateSlider.SetValueWithoutNotify(value);
            Application.targetFrameRate = value;
            PlayerPrefs.SetInt(PlayerPrefsReferences.TargetFrameRate.key, value);
        }

        private void SetTargetFrameRateViaSlider(float value) => SetTargetFrameRate(Mathf.RoundToInt(value));

        private void UpdateTargetFrameRateSlider(Resolution resolution)
        {
            targetFrameRateSlider.SetValueWithoutNotify(resolution.refreshRate);
            frameRateNumberTmp.text = resolution.refreshRate.ToString("0");
        }

        private void OnCombatAnimationSpeedChanged(float speedMultiplier)
        {
            combatAnimationSpeedNumberTmp.text = $"{speedMultiplier:0.00}x";
            combatAnimationSpeedSlider.SetValueWithoutNotify(speedMultiplier * 100f);
            PlayerPrefs.SetFloat(PlayerPrefsReferences.CombatAnimationSpeed.key, speedMultiplier);
        }

        private void SetCombatAnimationSpeedViaSlider(float value) => SetCombatAnimationSpeed(value / 100f);

        private void SetCombatAnimationSpeed(float speedMultiplier) => CombatAnimationSpeedHandler.SetValue(PlayerPrefsReferences.ClampCombatAnimationSpeed(speedMultiplier));

        private void SetCombatTickRateViaSlider(float value) => SetCombatTickRate(Mathf.RoundToInt(value));

        private void SetCombatTickRate(int tickRate) => CombatTickRateHandler.SetValue(PlayerPrefsReferences.ClampCombatTickRate(tickRate));
        
        private void OnCombatTickRateChanged(int tickRate)
        {
            combatTickRateNumberTmp.text = tickRate.ToString("0");
            combatTickRateSlider.SetValueWithoutNotify(tickRate);
            PlayerPrefs.SetInt(PlayerPrefsReferences.CombatTickRate.key, tickRate);
        }

        private void UpdateScreenModeDropdown(FullScreenMode mode) => _screenModeDropdown.SetValueWithoutNotify(input: (int) mode);
        private void CreateScreenModeOptions(Language language) => CreateScreenModeOptions();

        private void CreateScreenModeOptions()
        {
            _screenModeDropdown.ClearOptions();
            List<string> screenModeOptions = ScreenModeTranslations.Select(t => t.Translate().GetText()).ToList();
            _screenModeDropdown.AddOptions(screenModeOptions);
        }

        private void UpdateResolutionDropdown(Resolution resolution)
        {
            List<TMP_Dropdown.OptionData> options = _resolutionDropdown.options;
            for (int i = 0; i < options.Count; i++)
            {
                string text = options[index: i].text;
                Result<Resolution> parsedResolution = text.ParseResolution();
                if (parsedResolution.IsOk && ResolutionManager.CompareResolutions(right: resolution, left: parsedResolution.Value))
                {
                    _resolutionDropdown.SetValueWithoutNotify(input: i);
                    return;
                }
            }
            
            CreateResolutionOptions(extra: resolution);
        }

        private void CreateResolutionOptions(Resolution extra)
        {
            List<Resolution> allResolutions = Screen.resolutions.ToList();
            _resolutionDropdown.ClearOptions();
            List<string> newOptions = allResolutions.Select(selector: res => res.CompactFormat()).ToList();
            int index = allResolutions.FindIndex(match: res => ResolutionManager.CompareResolutions(right: res, left: extra));
            if (index == -1)
            {
                newOptions.Add(extra.CompactFormat());
                index = newOptions.Count - 1;
            }
                
            _resolutionDropdown.AddOptions(options: newOptions);
            _resolutionDropdown.SetValueWithoutNotify(input: index);
        }

        private static float VolumePercentageToDecibels(float volume)
        {
            if (volume <= 0)
                return -80;
            
            return Mathf.Log10(f: volume) * 20;
        }

        private void LoadMainMenuViaPauseMenu()
        {
            if (GameManager.AssertInstance(out GameManager gameManager))
                gameManager.PauseMenuToMainMenu();
        }

        private void SaveAndQuit()
        {
            if (SaveFilesManager.AssertInstance(out SaveFilesManager saveFilesManager))
                saveFilesManager.WriteCurrentSessionToDisk(log: true);
            
            if (DialogueController.AssertInstance(out DialogueController dialogueController))
                dialogueController.Stop();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
        }

        private void SetOpen(bool value)
        {
            if (value == _isOpen)
                return;

            _isOpen = value;
            if (_isOpen)
            {
                _timeScaleBeforePause = Option<float>.Some(Time.timeScale);
                Time.timeScale = 0f;
                _canvasGroup.alpha = 1;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
                Scene mainMenuScene = SceneManager.GetSceneByName(SceneRef.MainMenu);
                bool shouldQuitAndMainMenuButtonActivate = !(mainMenuScene.IsValid() && mainMenuScene.isLoaded);
                _saveAndQuitButton.gameObject.SetActive(shouldQuitAndMainMenuButtonActivate);
                _mainMenuButton.gameObject.SetActive(shouldQuitAndMainMenuButtonActivate);
            }
            else
            {
                if (_timeScaleBeforePause.TrySome(out float scale))
                    Time.timeScale = scale;
                
                _timeScaleBeforePause = Option<float>.None;
                _canvasGroup.alpha = 0;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }
        }

        public void Open() => SetOpen(true);
        public void Close() => SetOpen(false);
    }
}