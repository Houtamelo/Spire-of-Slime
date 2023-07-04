using Core.Pause_Menu.Scripts;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Core.Localization.Scripts
{
    public class LocalizedTMP : MonoBehaviour
    {
        [SerializeField, Required]
        private TMP_Text tmp;
        
        [SerializeField, Required]
        private LocalizedText text;
        
        private LanguageDelegate _onLanguageChanged;

        private void Awake() => _onLanguageChanged = _ => tmp.text = text.Translate().GetText();

        private void OnEnable()
        {
            PauseMenuManager.LanguageChanged += _onLanguageChanged;
            _onLanguageChanged.Invoke(PauseMenuManager.CurrentLanguage);
        }

        private void OnDisable() => PauseMenuManager.LanguageChanged -= _onLanguageChanged;

        private void Reset()
        {
            tmp = GetComponent<TMP_Text>();
        }

#if UNITY_EDITOR        
        private void OnValidate()
        {
            if (tmp == null)
            {
                tmp = GetComponent<TMP_Text>();
                if (tmp == null)
                {
                    Debug.LogWarning($"TMP_Text is null on {gameObject.name}", context: this);
                    return;
                }
                
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
        
#endif
    }
}