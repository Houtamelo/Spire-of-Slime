using UnityEngine;
using UnityEngine.EventSystems;

namespace Core.Pause_Menu.Scripts
{
    public class KeycapAnimationEnabler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private InputEnum inputKey;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (KeycapAnimator.Instance.TrySome(out KeycapAnimator keycapAnimator))
                keycapAnimator.Show(inputKey);
            else
                Debug.LogWarning("KeycapAnimator is not initialized", this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (KeycapAnimator.Instance.TrySome(out KeycapAnimator keycapAnimator))
                keycapAnimator.Hide();
            else
                Debug.LogWarning("KeycapAnimator is not initialized", this);
        }
    }
}