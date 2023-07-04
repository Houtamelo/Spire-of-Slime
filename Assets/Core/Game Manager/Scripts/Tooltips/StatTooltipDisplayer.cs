using Core.Localization.Scripts;
using Save_Management;
using Save_Management.Stats;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Core.Game_Manager.Scripts.Tooltips
{
    public class StatTooltipDisplayer : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField]
        private GeneralStat stat;

        public void OnPointerEnter(PointerEventData eventData)
        {
            CleanString key = stat.GetTooltipKey();
            if (key.IsNone())
                return;
            
            LocalizedText text = new(key);
            if (GeneralTooltip.AssertInstance(out GeneralTooltip generalTooltip))
                generalTooltip.Show(text.Translate().GetText());
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (GeneralTooltip.AssertInstance(out GeneralTooltip generalTooltip))
                generalTooltip.Hide();
        }
    }
}