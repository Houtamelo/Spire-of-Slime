using JetBrains.Annotations;
using UnityEngine;

namespace Utils.Editor
{
    public class AnchorSetter : MonoBehaviour
    {
        [UnityEditor.MenuItem(itemName: "CONTEXT/RectTransform/Set Anchors")]
        private static void AnchorsToCorners()
        {
            foreach (Transform transform in UnityEditor.Selection.transforms)
            {
                RectTransform t = transform as RectTransform;
                RectTransform pt = transform.parent as RectTransform;

                if (t == null || pt == null) return;

                Rect rect = pt.rect;
                Vector2 newAnchorsMin = new Vector2(x: t.anchorMin.x + t.offsetMin.x / rect.width,
                    y: t.anchorMin.y + t.offsetMin.y / rect.height);
                Vector2 newAnchorsMax = new Vector2(x: t.anchorMax.x + t.offsetMax.x / rect.width,
                    y: t.anchorMax.y + t.offsetMax.y / rect.height);

                t.anchorMin = newAnchorsMin;
                t.anchorMax = newAnchorsMax;
                t.offsetMin = t.offsetMax = new Vector2(x: 0, y: 0);
                UnityEditor.EditorUtility.SetDirty(target: transform);
            }
        }
        public static void SetIndividualCorner([NotNull] Transform transform)
        {
            if (transform is not RectTransform t || transform.parent is not RectTransform pt)
                return;
            
            Rect rect = pt.rect;
            Vector2 newAnchorsMin = new Vector2(x: t.anchorMin.x + t.offsetMin.x / rect.width,
                y: t.anchorMin.y + t.offsetMin.y / rect.height);
            Vector2 newAnchorsMax = new Vector2(x: t.anchorMax.x + t.offsetMax.x / rect.width,
                y: t.anchorMax.y + t.offsetMax.y / rect.height);

            t.anchorMin = newAnchorsMin;
            t.anchorMax = newAnchorsMax;
            t.offsetMin = t.offsetMax = new Vector2(x: 0, y: 0);
        }
        [UnityEditor.MenuItem(itemName: "CONTEXT/RectTransform/GetWorldPos")]
        private static void WorldPos()
        {
            if (Camera.main == null)
                return;
            
            foreach (Transform trans in UnityEditor.Selection.transforms)
            {
                Vector3 position = trans.position;
                Debug.Log($"X: {position.x:0.000}, Y: {position.y:0.000}, Z: {position.z:0.000}");
            }
        }
        
        [UnityEditor.MenuItem(itemName: "CONTEXT/RectTransform/GetLocalPos")]
        private static void LocalPos()
        {
            if (Camera.main == null)
                return;
            
            foreach (Transform trans in UnityEditor.Selection.transforms)
            {
                Vector3 position = trans.localPosition;
                Debug.Log($"X: {position.x:0.000}, Y: {position.y:0.000}, Z: {position.z:0.000}");
            }
        }
    }
}