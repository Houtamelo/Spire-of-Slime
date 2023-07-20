using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Core.Combat.Scripts.UI
{
    public class CombatChoiceBox : MonoBehaviour
    {
        [SerializeField] private CombatChoiceButton buttonPrefab;
        [SerializeField] private Transform buttonsParent;
        [SerializeField] private GameObject optionsBox;
        [SerializeField] private Button outsideBoxDetector;
        
        private readonly List<CombatChoiceButton> _buttons = new();
        private UnityAction _closePanelAction;

        private void Awake()
        {
            _closePanelAction = ClosePanel;
            outsideBoxDetector.onClick.AddListener(_closePanelAction);
        }

        private void ClosePanel() => optionsBox.SetActive(false);
        public void ShowOptions(Vector3 worldPosition, string text, UnityAction onClick)
        {
            for (int i = _buttons.Count; i < 1; i++) 
                CreateButton();
            
            CombatChoiceButton combatChoiceButton = _buttons[0];
            combatChoiceButton.Button.onClick.RemoveAllListeners();
            combatChoiceButton.Button.onClick.AddListener(onClick);
            combatChoiceButton.Button.onClick.AddListener(_closePanelAction);
            combatChoiceButton.Tmp.text = text;
            combatChoiceButton.gameObject.SetActive(true);

            for (int i = 1; i < _buttons.Count; i++)
                _buttons[i].gameObject.SetActive(false);
            
            optionsBox.transform.position = worldPosition;
            optionsBox.SetActive(true);
        }
        
        public void ShowOptions(Vector3 worldPosition, [NotNull] params (string text, UnityAction onClick)[] options)
        {
            for (int i = _buttons.Count; i < options.Length; i++) 
                CreateButton();

            for (int i = 0; i < options.Length; i++)
            {
                (string text, UnityAction onClick) = options[i];
                CombatChoiceButton combatChoiceButton = _buttons[i];
                combatChoiceButton.Button.onClick.RemoveAllListeners();
                combatChoiceButton.Button.onClick.AddListener(onClick);
                combatChoiceButton.Button.onClick.AddListener(_closePanelAction);
                combatChoiceButton.Tmp.text = text;
                combatChoiceButton.gameObject.SetActive(true);
            }
            
            for (int i = options.Length; i < _buttons.Count; i++)
                _buttons[i].gameObject.SetActive(false);
            
            optionsBox.transform.position = worldPosition;
            optionsBox.SetActive(true);
        }

        private void CreateButton()
        {
            CombatChoiceButton button = Instantiate(buttonPrefab, buttonsParent);
            _buttons.Add(button);
        }
    }
}