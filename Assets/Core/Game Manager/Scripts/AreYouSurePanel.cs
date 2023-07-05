using Core.Utils.Patterns;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Utils.Patterns;

namespace Core.Game_Manager.Scripts
{
    public sealed class AreYouSurePanel : Singleton<AreYouSurePanel>
    {
        [SerializeField, Required, SceneObjectsOnly] 
        private Button yesButton, noButton;

        [SerializeField, Required, SceneObjectsOnly]
        private TMP_Text messageTmp;

        [SerializeField, Required, SceneObjectsOnly]
        private Camera uiCamera;
        
        private UnityAction _deactivate;

        protected override void Awake()
        {
            base.Awake();
            _deactivate = () => gameObject.SetActive(false);
            noButton.onClick.AddListener(_deactivate);
        }
        private void Start()
        {
            gameObject.SetActive(false);
        }
        private void Update()
        {
            if (Mouse.current.rightButton.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
                gameObject.SetActive(false);
        }

        public void Show(UnityAction onYes, string message)
        {
            yesButton.onClick.RemoveAllListeners();
            yesButton.onClick.AddListener(onYes);
            yesButton.onClick.AddListener(_deactivate);
            messageTmp.text = message;

            Vector2 desiredScreenPosition = Mouse.current.position.ReadValue();
            Vector3 desiredWorldPosition = uiCamera.ScreenToWorldPoint(desiredScreenPosition);
            desiredWorldPosition.z = 0;
            transform.position = desiredWorldPosition;
            
            gameObject.SetActive(true);
        }
    }
}