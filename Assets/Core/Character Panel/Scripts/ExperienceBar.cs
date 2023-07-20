using Core.Localization.Scripts;
using Core.Save_Management;
using Core.Save_Management.SaveObjects;
using Core.Utils.Math;
using Core.Utils.Patterns;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Core.Character_Panel.Scripts
{
    public class ExperienceBar : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private static readonly LocalizedText ToNextLevelTrans = new("characterpanel_experiencebar_tonextlevel");
        
        [SerializeField, Required, SceneObjectsOnly]
        private Slider slider;
        
        [SerializeField, Required, SceneObjectsOnly]
        private CanvasGroup canvasGroup;

        [SerializeField, Required, SceneObjectsOnly]
        private TMP_Text currentPercentageText;

        private void Start()
        {
            if (CharacterMenuManager.AssertInstance(out CharacterMenuManager characterMenuManager))
                characterMenuManager.SelectedCharacter.Changed += OnSelectedCharacterChanged;
        }

        private void OnDestroy()
        {
            if (CharacterMenuManager.Instance.TrySome(out CharacterMenuManager characterMenuManager))
                characterMenuManager.SelectedCharacter.Changed -= OnSelectedCharacterChanged;
        }

        private void OnEnable()
        {
            if (CharacterMenuManager.AssertInstance(out CharacterMenuManager characterMenuManager))
                OnSelectedCharacterChanged(characterMenuManager.SelectedCharacter.AsOption());
        }

        private void OnSelectedCharacterChanged(Option<IReadonlyCharacterStats> character)
        {
            if (character.IsSome)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                slider.value = (float)ExperienceCalculator.GetExperiencePercentage(character.Value);
            }
            else
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            currentPercentageText.text = ToNextLevelTrans.Translate().GetText((1f - slider.value).ToPercentageString());
            currentPercentageText.gameObject.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            currentPercentageText.gameObject.SetActive(false);
        }
    }
}