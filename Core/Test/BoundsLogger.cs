using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Test
{
    [ExecuteInEditMode]
    public class BoundsLogger : MonoBehaviour
    {
        [SerializeField]
        private SpriteRenderer spriteRenderer;
        
        [Button]
        private void LogBounds()
        {
            Bounds bounds = spriteRenderer.bounds;
            Debug.Log($"Bounds: {bounds.ToString()}");
            Vector3 top = bounds.max;
            top.x -= bounds.extents.x;
            top.z = 0;
            Debug.Log($"Top: {top.ToString()}");
            
            Vector3 bottom = bounds.min;
            bottom.x += bounds.extents.x;
            bottom.z = 0;
            Debug.Log($"Bottom: {bottom.ToString()}");
            
            Vector3 bottomToTop = top - bottom;
            Debug.Log($"Bottom to top: {bottomToTop.ToString()}");
        }

#if UNITY_EDITOR
        private void Update()
        {
            // foreach (Object obj in UnityEditor.Selection.objects)
            // {
            //     Debug.Log($"Selected: {obj.name}, type:{obj.GetType()}");
            // }

            // foreach (Object obj in UnityEditor.DragAndDrop.objectReferences)
            // {
            //     Debug.Log($"Selected: {obj.name}, type:{obj.GetType()}");
            // }
        }
#endif
    }
}