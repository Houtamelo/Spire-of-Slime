using System;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

namespace Core.Visual_Novel.Scripts
{
    public class OptionButton : MonoBehaviour
    {
        [SerializeField, Required] 
        private TMP_Text label;
        
        [SerializeField, Required]
        private Button button;

        private int _index;
        private Action<int> _onClick;
        private DialogueDisplay _dialogueDisplay;
        
        public void Initialize(DialogueOption dialogueOption, int index, Action<int> onClick, DialogueDisplay dialogueDisplay)
        {
            _dialogueDisplay = dialogueDisplay;
            _index = index;
            _onClick = onClick;
            label.text = dialogueOption.Line.Text.Text;
            button.interactable = dialogueOption.IsAvailable;
            gameObject.SetActive(true);
        }

        public void ResetMe()
        {
            _onClick = null;
            _index = 0;
            gameObject.SetActive(false);
        }

        private void Awake()
        {
            button.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            Action<int> cachedOnClick = _onClick; // need to cache these because OptionButtonClicked() calls ResetMe()
            int cachedIndex = _index;
            
            if (_dialogueDisplay != null)
                _dialogueDisplay.OptionButtonClicked();

            if (cachedOnClick != null)
                cachedOnClick.Invoke(cachedIndex);
            else
                Debug.LogWarning("Option button clicked but _onClick callback was not set.", this);
        }

        private void Reset()
        {
            label = GetComponentInChildren<TMP_Text>();
            button = GetComponentInChildren<Button>();
        }
    }
}