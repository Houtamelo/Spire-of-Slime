using Core.Localization.Scripts;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Core.Game_Manager.Scripts.Tooltips
{
    public class TooltipDisplayer : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField]
        private LocalizedText key;

        private void Awake()
        {
            if (key.Key.IsNullOrEmpty())
            {
                Debug.LogWarning("TooltipDisplayer has no text key set!", context: this);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (GeneralTooltip.AssertInstance(out GeneralTooltip generalTooltip))
                generalTooltip.Show(key.Translate().GetText());
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (GeneralTooltip.AssertInstance(out GeneralTooltip generalTooltip))
                generalTooltip.Hide();
        }
    }
}