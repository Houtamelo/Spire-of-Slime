using UnityEngine;
using UnityEngine.UI;

namespace Core.Visual_Novel.Scripts
{
    public class SkipToggle : MonoBehaviour
    {
        private Toggle _toggle;

        private void Awake()
        {
            _toggle = GetComponent<Toggle>();
        }
        private void Start()
        {
            _toggle.onValueChanged.AddListener(OnValueChanged);
            
            DialogueDisplay.SkipHandler.Changed += SkipToggled;
            SkipToggled(DialogueDisplay.SkipHandler.Value);
        }
        private void OnDestroy()
        {
            DialogueDisplay.SkipHandler.Changed -= SkipToggled;
        }

        private void OnValueChanged(bool value)
        {
            DialogueDisplay.SkipHandler.SetValue(value);
        }

        private void SkipToggled(bool value)
        {
            _toggle.SetIsOnWithoutNotify(value);
        }
    }
}