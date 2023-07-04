using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using Utils.Patterns;

namespace Core.Pause_Menu.Scripts
{
    public class KeycapAnimator : Singleton<KeycapAnimator>
    {
        [SerializeField, Required, SceneObjectsOnly]
        private TMP_Text upText, downText;

        [SerializeField, Required, SceneObjectsOnly]
        private RectTransform selfRect;

        [SerializeField]
        private Vector2 offset = new(10, -20);

        [SerializeField, Required, SceneObjectsOnly]
        private Camera uiCamera;

        private void Start()
        {
            Hide();
        }

        private void Update()
        {
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
            Vector2 desiredScreenPosition = mouseScreenPosition + offset;
            Vector3 desiredWorldPosition = uiCamera.ScreenToWorldPoint(desiredScreenPosition);
            desiredWorldPosition.z = 0;
            selfRect.position = desiredWorldPosition;
        }

        public void Show(InputEnum inputKey)
        {
            Option<InputManager> inputManager = InputManager.Instance;
            if (inputManager.IsNone)
            {
                Debug.LogWarning("InputManager is not initialized");
                return;
            }
            
            ReadOnlyArray<InputBinding> bindings = inputManager.Value.GetInputBindings(inputKey);
            if (bindings.Count == 0)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"Input {inputKey} is not bound");
#endif
                return;
            }

            string keyText = bindings[0].ToDisplayString(); 
            upText.text = keyText;
            downText.text = keyText;
            
            UpdatePosition();
            gameObject.SetActive(true);
        }
        
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void Reset()
        {
            selfRect = (RectTransform)transform;
        }
    }
}