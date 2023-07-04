using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    [DisallowMultipleComponent, ExecuteInEditMode]
    public class CanvasRendererHighlightable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private const float Gray = 0.58823529411764705882352941176471f;
        
        [SerializeField, Required]
        private Graphic targetGraphic;
        
        [SerializeField]
        private Color normalColor = new(Gray, Gray, Gray, 1f);

        [SerializeField]
        private Color highlightedColor = Color.white;

        [SerializeField]
        private bool validate = true;
        
        public void OnPointerEnter(PointerEventData _)
        {
            targetGraphic.canvasRenderer.SetColor(highlightedColor);
        }

        public void OnPointerExit(PointerEventData _)
        {
            targetGraphic.canvasRenderer.SetColor(normalColor);
        }

        private void OnDisable()
        {
            OnPointerExit(null);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (validate == false)
                return;

            if (targetGraphic == null)
            {
                targetGraphic = GetComponent<Graphic>();
                if (targetGraphic == null)
                {
                    Debug.LogWarning($"Target graphic not found on {name}", context: this);
                    return;
                }

                UnityEditor.EditorUtility.SetDirty(this);
            }
            
            OnPointerExit(null);

            if (targetGraphic.raycastTarget == false)
            {
                Debug.LogWarning($"Target graphic raycast target is disabled on {name}", context: this);
            }
        }
        
#endif

        private void Reset()
        {
            targetGraphic = GetComponent<Graphic>();
        }
    }
}