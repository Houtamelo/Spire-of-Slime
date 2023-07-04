using Core.Combat.Scripts.Interfaces;
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Localization.Scripts;
using Save_Management;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utils.Patterns;

namespace Core.Screen_Buttons.Scripts
{
    public class LustSlider : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private static readonly LocalizedText Trans_Lust = new("screen-buttons_tooltip_lust-slider");
        
        [SerializeField, Required]
        private Image lowFill, highFill;

        [SerializeField, Required]
        private Image portraitIcon, portraitBackground;

        [SerializeField, Required]
        private GameObject portraitObject;

        [SerializeField, Required]
        private TMP_Text tooltipTmp;

        [SerializeField, Required]
        private Vector3 textCueWorldOffset;
        
        private Option<AudioSource> _pointerEnterSound;
        public Option<IReadonlyCharacterStats> AssignedCharacter { get; private set; }
            
        public void SetCharacter(IReadonlyCharacterStats character)
        {
            gameObject.SetActive(true);
            SetLust(character.Lust);
            if (AssignedCharacter.IsSome && AssignedCharacter.Value == character)
                return;

            AssignedCharacter = Option<IReadonlyCharacterStats>.Some(character);
            ICharacterScript script = character.GetScript();
            Option<Sprite> portraitOption = script.GetPortrait;
            if (portraitOption.IsNone)
            {
                portraitObject.SetActive(false);
                return;
            }
            
            portraitObject.SetActive(true);
            portraitIcon.sprite = portraitOption.Value;
            Option<Color> color = script.GetPortraitBackgroundColor;
            portraitBackground.color = color.IsSome ? color.Value : Color.white;
        }
        
        private void SetLust(uint lust)
        {
            if (lust <= 100)
            {
                lowFill.fillAmount = lust / 100f;
                highFill.fillAmount = 0;
                return;
            }
            
            lowFill.fillAmount = 1;
            highFill.fillAmount = (lust - 100) / 100f;
        }

        public Vector3 GetTextCueStartWorldPosition() => transform.position + textCueWorldOffset;

        public void ResetMe()
        {
            AssignedCharacter = Option<IReadonlyCharacterStats>.None;
            gameObject.SetActive(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (AssignedCharacter.IsNone)
                return;
            
            tooltipTmp.text = $"{Trans_Lust.Translate()} {AssignedCharacter.Value.Lust.ToString("0")} / {ILustModule.MaxLust.ToString("0")}";
            tooltipTmp.gameObject.SetActive(true);
            if (_pointerEnterSound.AssertSome(out AudioSource sound))
                sound.Play();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            tooltipTmp.gameObject.SetActive(false);
        }

        public void AssignPointerEnterSound(AudioSource audioSource)
        {
            _pointerEnterSound = audioSource != null ? Option<AudioSource>.Some(audioSource) : Option<AudioSource>.None;
        }
    }
}