using TMPro;
using UnityEngine;

namespace Core.Combat.Scripts.UI
{
    public class TooltipSidePanel : MonoBehaviour
    {
        [field: SerializeField] 
        private TMP_Text Title { get; set; }
        
        [field: SerializeField] 
        private TMP_Text Description { get; set; }

        public void Show(string title, string description)
        {
            Title.text = title;
            Description.text = description;
            gameObject.SetActive(true);
        }
        
        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}