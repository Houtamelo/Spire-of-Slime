using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Main_Menu.Scripts
{
    public class PlannedFeatures : SerializedMonoBehaviour
    {
        [OdinSerialize, Required]
        private (Toggle, GameObject)[] _toggles;

        [SerializeField, Required]
        private Button mainButton, closeButton;

        [SerializeField, Required]
        private GameObject firstPanel, shortTerm, longTerm;

        private void Awake()
        {
            foreach ((Toggle toggle, GameObject bindedObject) in _toggles)
                toggle.onValueChanged.AddListener(isOn => bindedObject.SetActive(isOn));

            mainButton.onClick.AddListener(() =>
            {
                firstPanel.SetActive(false);
                shortTerm.SetActive(true);
                longTerm.SetActive(true);
            });

            closeButton.onClick.AddListener(() => gameObject.SetActive(false));
        }
    }
}