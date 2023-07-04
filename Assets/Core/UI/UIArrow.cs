using UnityEngine;

namespace UI
{
    [RequireComponent(typeof(RectTransform))]
    public class UIArrow : MonoBehaviour
    {
        [SerializeField, HideInInspector] private RectTransform rectTransform;
        private RectTransform RectTransform
        {
            get
            {
                if (ReferenceEquals(rectTransform, null)) 
                    rectTransform = (RectTransform)transform;

                return rectTransform;
            }
        }

        public void SetPoints(Vector3 originWorldPos, Vector3 destinationWorldPos)
        {
            originWorldPos.z = 0;
            destinationWorldPos.z = 0;
            Transform selfTransform = transform;
            selfTransform.right = destinationWorldPos - originWorldPos;
            selfTransform.position = originWorldPos;
            RectTransform parentRect = (RectTransform) selfTransform.parent;
            float localDistance = Vector3.Distance(parentRect.InverseTransformPoint(originWorldPos), parentRect.InverseTransformPoint(destinationWorldPos));
            float width = Mathf.Max(0, localDistance);
            RectTransform.sizeDelta = new Vector2(width, RectTransform.sizeDelta.y);
        }

        private void Reset()
        {
            rectTransform = (RectTransform)transform;
        }
    }
}