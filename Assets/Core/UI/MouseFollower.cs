using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UI
{
    public class MouseFollower : MonoBehaviour
    {
        [SerializeField, Required]
        private Camera targetCamera;

        [SerializeField]
        private Vector3 worldOffset;

        [SerializeField]
        private Vector2 screenOffset;
        
        private void Update()
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue() + screenOffset;
            Vector3 worldPosition = targetCamera.ScreenToWorldPoint(mousePosition);
            transform.position = worldPosition + worldOffset;
        }

        private void OnEnable()
        {
            Update();
        }
    }
}