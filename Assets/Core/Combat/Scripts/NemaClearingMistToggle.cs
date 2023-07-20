using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Core.Combat.Scripts
{
    public class NemaClearingMistToggle : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private GameObject clearMistOption;
        
        public void OnPointerClick([NotNull] PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Right)
                return;
            
            clearMistOption.SetActive(clearMistOption.activeSelf);
        }
    }
}