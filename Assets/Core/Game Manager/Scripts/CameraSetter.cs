using UnityEngine;

namespace Core.Game_Manager.Scripts
{
    public sealed class CameraSetter : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private Camera sceneCamera;

        private void Awake()
        {
            canvas.worldCamera = sceneCamera;
        }

        private void Reset()
        {
            canvas = GetComponent<Canvas>();
            sceneCamera = FindObjectOfType<Camera>();
            if (sceneCamera == null)
                Debug.Log(message: $"Could not find camera of {name}", context: this);
        }

        #if UNITY_EDITOR
        private void OnValidate()
        {
            if (sceneCamera == null)
            {
                sceneCamera = FindObjectOfType<Camera>();
                if (sceneCamera == null)
                    Debug.Log(message: $"Could not find camera of {name}", context: this);
                else
                    UnityEditor.EditorUtility.SetDirty(this);
            }
        }
        #endif
    }
}