using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utils.Patterns;

namespace Core.World_Map.Scripts
{
    public sealed class DottedLine : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private Image image;
        [SerializeField] private Color defaultColor, highlightedColor;
        [field: SerializeField] public BothWays Way { get; private set; }
        public LocationButton StartLocationButton { get; private set;}
        public LocationButton EndLocationButton { get; private set; }

        public void Initialize(LocationButton start, LocationButton end)
        {
            gameObject.SetActive(true);
            StartLocationButton = start;
            EndLocationButton = end;
        }

        public void OnPointerEnter(PointerEventData _ = null)
        {
            HighLight();
            Option<WorldMapManager> worldMap = WorldMapManager.Instance;
            if (worldMap.IsSome)
                worldMap.Value.DottedLinePointerEnter(dottedLine: this);
            else
                Debug.LogError("WorldMapManager.Instance is null");
        }

        public void OnPointerExit(PointerEventData _ = null)
        {
            LowLight();
            Option<WorldMapManager> worldMap = WorldMapManager.Instance;
            if (worldMap.IsSome)
                worldMap.Value.DottedLinePointerExit(dottedLine: this);
            else
                Debug.LogError("WorldMapManager.Instance is null");
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            Option<WorldMapManager> worldMap = WorldMapManager.Instance;
            if (worldMap.IsSome)
                worldMap.Value.DottedLineClicked(dottedLine: this);
            else
                Debug.LogError("WorldMapManager.Instance is null");
        }
        
        public void HighLight()
        {
            image.color = highlightedColor;
        }

        public void LowLight()
        {
            image.color = defaultColor;
        }

        private void Reset()
        {
            image = GetComponent<Image>();
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (image == null)
            {
                image = GetComponent<Image>();
                if (image == null)
                {
                    Debug.Log(message: "DottedLine: Image not found", context: this);
                }
                else
                {
                    UnityEditor.EditorUtility.SetDirty(target: this);
                }
            }
        }
#endif
    }
}