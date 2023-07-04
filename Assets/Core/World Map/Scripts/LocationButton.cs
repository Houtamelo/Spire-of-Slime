using Core.Pause_Menu.Scripts;
using KGySoft.CoreLibraries;
using Save_Management;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utils.Patterns;

namespace Core.World_Map.Scripts
{
    public class LocationButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField, Required]
        private Button button;
        
        [SerializeField]
        private LocationEnum location;
        public LocationEnum Location => location;

        [SerializeField]
        private Color defaultColor, highlightedColor;

        [SerializeField, Required]
        private TMP_Text locationNameText;

        [SerializeField, Required]
        private Image image;

        private CleanString _locationName;
        private LanguageDelegate _onLanguageChanged;

        private void Awake()
        {
            _locationName = new CleanString($"world-location_{Enum<LocationEnum>.ToString(Location)}");
            _onLanguageChanged = _ => locationNameText.text = _locationName.ToString();
        }

        private void Start()
        {
            button.onClick.AddListener(LocationClicked);
        }

        private void OnEnable()
        {
            _onLanguageChanged.Invoke(PauseMenuManager.CurrentLanguage);
            PauseMenuManager.LanguageChanged += _onLanguageChanged;
        }

        private void OnDisable() => PauseMenuManager.LanguageChanged -= _onLanguageChanged;

        private void LocationClicked()
        {
            if (WorldMapManager.AssertInstance(out WorldMapManager worldMapManager))
                worldMapManager.LocationButtonClicked(buttonLocation: Location);
        }

        public void OnPointerEnter(PointerEventData _ = null)
        {
            HighLight();
            Option<WorldMapManager> worldMap = WorldMapManager.Instance;
            if (worldMap.IsSome)
                worldMap.Value.LocationButtonPointerEnter(locationButton: this);
            else
                Debug.LogError("WorldMapManager is null");
        }

        public void OnPointerExit(PointerEventData _ = null)
        {
            LowLight();
            Option<WorldMapManager> worldMap = WorldMapManager.Instance;
            if (worldMap.IsSome)
                worldMap.Value.LocationButtonPointerExit(locationButton: this);
            else
                Debug.LogError("WorldMapManager is null");
        }

        public void HighLight()
        {
            image.color = highlightedColor;
            locationNameText.color = highlightedColor;
            
            Option<WorldTooltip> tooltip = WorldTooltip.Instance;
            if (tooltip.IsSome)
                tooltip.Value.DisplayTooltip(location: Location, locationButton: transform);
            else
                Debug.LogWarning("WorldTooltip is null");
        }

        public void LowLight()
        {
            image.color = defaultColor;
            locationNameText.color = defaultColor;

            Option<WorldTooltip> tooltip = WorldTooltip.Instance;
            if (tooltip.IsSome)
                tooltip.Value.StopTooltiping(locationButton: transform);
            else
                Debug.LogWarning("WorldTooltip is null");
        }

        public void SetActive(bool value)
        {
            gameObject.SetActive(value);
        }
    }
}