using Core.Save_Management;
using Core.Save_Management.SaveObjects;
using Core.Utils.Math;
using Core.Utils.Patterns;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utils.Patterns;

namespace Core.Character_Panel.Scripts
{
    public class ExperienceBar : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField, Required, SceneObjectsOnly]
        private Slider slider;
        
        [SerializeField, Required, SceneObjectsOnly]
        private CanvasGroup canvasGroup;

        [SerializeField, Required, SceneObjectsOnly]
        private TMP_Text currentPercentageText;

        private void Start()
        {
            if (!CharacterMenuManager.Instance.TrySome(out CharacterMenuManager characterMenuManager))
            {
                Debug.LogWarning("CharacterMenuManager not found.", context: this);
                return;
            }
            
            characterMenuManager.SelectedCharacter.Changed += OnSelectedCharacterChanged;
        }

        private void OnDestroy()
        {
            if (CharacterMenuManager.Instance.TrySome(out CharacterMenuManager characterMenuManager))
                characterMenuManager.SelectedCharacter.Changed -= OnSelectedCharacterChanged;
        }

        private void OnEnable()
        {
            if (!CharacterMenuManager.Instance.TrySome(out CharacterMenuManager characterMenuManager))
            {
                Debug.LogWarning("CharacterMenuManager not found.", context: this);
                return;
            }

            OnSelectedCharacterChanged(characterMenuManager.SelectedCharacter.AsOption());
        }

        private void OnSelectedCharacterChanged(Option<IReadonlyCharacterStats> character)
        {
            if (character.IsSome)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                slider.value = ExperienceCalculator.GetExperiencePercentage(character.Value);
            }
            else
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            currentPercentageText.text = $"To next level: {(1f - slider.value).ToPercentageString()}";
            currentPercentageText.gameObject.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            currentPercentageText.gameObject.SetActive(false);
        }
    }
}