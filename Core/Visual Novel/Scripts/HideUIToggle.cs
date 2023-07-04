using UnityEngine;
using UnityEngine.UI;
using Utils.Patterns;

namespace Core.Visual_Novel.Scripts
{
    public class HideUIToggle : MonoBehaviour
    {
        private Toggle _toggle;

        private void Awake()
        {
            _toggle = GetComponent<Toggle>();
        }
        
        private void Start()
        {
            _toggle.onValueChanged.AddListener(OnValueChanged);

            Option<DialogueUIManager> dialogueManager = DialogueUIManager.Instance;
            if (dialogueManager.IsNone)
            {
                Debug.LogError("DialogueUIManager not found");
                return;
            }
            
            dialogueManager.Value.HideUIHandler.Changed += HideUIToggled;
            HideUIToggled(dialogueManager.Value.HideUIHandler.Value);
        }

        private void OnDestroy()
        {
            Option<DialogueUIManager> dialogueManager = DialogueUIManager.Instance;
            if (dialogueManager.IsSome)
                dialogueManager.Value.HideUIHandler.Changed -= HideUIToggled;
        }

        private void OnValueChanged(bool value)
        {
            Option<DialogueUIManager> dialogueManager = DialogueUIManager.Instance;
            if (dialogueManager.IsSome)
                dialogueManager.Value.HideUIHandler.SetValue(value);
            else
                Debug.LogError("DialogueUIManager not found");
        }

        private void HideUIToggled(bool value) => _toggle.SetIsOnWithoutNotify(value);
    }
}