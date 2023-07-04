using UnityEngine;
using UnityEngine.UI;

namespace Core.Visual_Novel.Scripts
{
    public class AutoPlayToggle : MonoBehaviour
    {
        private Toggle _toggle;
        private void Awake()
        {
            _toggle = GetComponent<Toggle>();
        }

        private void Start()
        {
            DialogueDisplay.AutoPlayHandler.Changed += AutoPlayToggled;
            AutoPlayToggled(DialogueDisplay.AutoPlayHandler.Value);
            _toggle.onValueChanged.AddListener(OnValueChanged);
        }

        private void OnDestroy()
        {
            DialogueDisplay.AutoPlayHandler.Changed -= AutoPlayToggled;
        }

        private void OnValueChanged(bool value)
        {
            DialogueDisplay.AutoPlayHandler.SetValue(value);
        }

        private void AutoPlayToggled(bool value)
        {
            _toggle.SetIsOnWithoutNotify(value);
        }
    }
}