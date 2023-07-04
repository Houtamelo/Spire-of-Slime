using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{
    [DisallowMultipleComponent]
    public class UnselectableByMouse : MonoBehaviour, IPointerClickHandler, IPointerUpHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            if (gameObject.activeInHierarchy)
                StartCoroutine(UnselectRoutine());
        }
        
        public void OnPointerUp(PointerEventData eventData)
        {
            if (gameObject.activeInHierarchy)
                StartCoroutine(UnselectRoutine());
        }

        private IEnumerator UnselectRoutine()
        {
            const int maxFrames = 10;

            for (int i = 0; i < maxFrames; i++)
            {
                EventSystem eventSystem = EventSystem.current;
                if (eventSystem.currentSelectedGameObject == gameObject && !eventSystem.alreadySelecting)
                {
                    eventSystem.SetSelectedGameObject(null);
                    StopAllCoroutines();
                    break;
                }

                yield return null;
            }
        }
    }
}