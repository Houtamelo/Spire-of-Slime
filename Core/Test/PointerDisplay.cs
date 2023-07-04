using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Core.Test
{
    public class PointerDisplay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField]
        private Toggle target;

        public void OnPointerEnter(PointerEventData eventData)
        {
            target.isOn = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            target.isOn = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log("Clicked");
        }
    }
}