using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Game_Manager.Scripts
{
    public class CameraPrioritySetter : MonoBehaviour
    {
        [SerializeField]
        private bool byScene = true;
        
        [SerializeField, ShowIf(nameof(byScene))]
        private SceneEnum scene;
        
        [SerializeField, HideIf(nameof(byScene))]
        private int priority;
        
        [SerializeField]
        private bool addDelta;
        
        [SerializeField, ShowIf(nameof(addDelta))]
        private int delta;

        private void Awake() => SetPriority();
        private void OnValidate() => SetPriority();
        private void SetPriority()
        {
            Camera cam = GetComponent<Camera>();
            if (cam == null)
            {
                Debug.LogWarning($"Missing camera on priority setter: {name}", this);
                return;
            }

            cam.depth = (byScene ? scene.GetCameraPriority() : priority) + (addDelta ? delta : 0);
        }
    }
}