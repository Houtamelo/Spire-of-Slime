using System.Text;
using Core.Localization.Scripts;
using Core.Save_Management;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using Core.Utils.Patterns;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utils.Patterns;
using Save = Core.Save_Management.SaveObjects.Save;

namespace Core.Screen_Buttons.Scripts
{
    public class ClearMistToggle : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private static readonly StringBuilder Builder = new();
        
        private static readonly LocalizedText Trans_NemaClearingMist = new("screen-buttons_tooltip_nema-clearing-mist");
        private static readonly LocalizedText Trans_NemaExhausted = new("screen-buttons_tooltip_nema-exhausted");
        private static readonly LocalizedText Trans_NemaNotClearingMist = new("screen-buttons_tooltip_nema-not-clearing-mist");

        private static readonly LocalizedText Trans_MistNone = new("nemaexhaustion_none_tooltip");
        private static readonly LocalizedText Trans_MistLow = new("nemaexhaustion_low_tooltip");
        private static readonly LocalizedText Trans_MistMedium = new("nemaexhaustion_medium_tooltip");
        private static readonly LocalizedText Trans_MistHigh = new("nemaexhaustion_high_tooltip");

        [SerializeField, Required]
        private Sprite onSprite, offSprite;

        [SerializeField, Required, SceneObjectsOnly]
        private Image icon;

        [SerializeField, Required, SceneObjectsOnly]
        private AudioSource pointerEnterSound, pointerClickSound, invalidClickSound;

        [SerializeField, Required, SceneObjectsOnly]
        private TMP_Text tooltip;

        [SerializeField, Required, SceneObjectsOnly]
        private GameObject tooltipObject;

        private void Start()
        {
            Save.NemaExhaustionChanged += OnNemaExhaustionChanged;
            Save.BoolChanged += OnBoolChanged;
            Save.Handler.Changed += OnSaveChanged;
            Save save = Save.Current;
            if (save != null)
                CheckStatus(save.IsNemaClearingMist, save.NemaExhaustion);
        }
        private void OnDestroy()
        {
            Save.NemaExhaustionChanged -= OnNemaExhaustionChanged;
            Save.BoolChanged -= OnBoolChanged;
            Save.Handler.Changed -= OnSaveChanged;
        }

        private void OnSaveChanged(Save save)
        {
            if (save != null)
                CheckStatus(save.IsNemaClearingMist, save.NemaExhaustion);
        }

        private void OnBoolChanged(CleanString variableName, bool oldValue, bool newValue)
        {
            if (variableName != VariablesName.Nema_ClearingMist)
                return;
            
            Save save = Save.Current;
            if (save == null)
            {
                Debug.LogWarning("Save is null, bool changed event should not be triggered.");
                return;
            }
            
            CheckStatus(newValue, save.NemaExhaustion);
        }

        private void OnNemaExhaustionChanged(NemaStatus status)
        {
            CheckStatus(status.SetToClearMist.current, status.Exhaustion.current);
        }

        private void CheckStatus(bool isClearingMist, ClampedPercentage exhaustion)
        {
            (LocalizedText mistTooltip, LocalizedText clearingTooltip, Sprite sprite) = (isClearingMist, (float)exhaustion) switch
            {
                (_, >= Save.HighExhaustion)      => (Trans_MistHigh, Trans_NemaExhausted, offSprite),
                (false, _)                       => (Trans_MistHigh, Trans_NemaNotClearingMist, offSprite), 
                (true, >= Save.MediumExhaustion) => (Trans_MistMedium, Trans_NemaClearingMist, onSprite),
                (true, >= Save.LowExhaustion)    => (Trans_MistLow, Trans_NemaClearingMist, onSprite),
                (true, _)                        => (Trans_MistNone, Trans_NemaClearingMist, onSprite)
            };

            icon.sprite = sprite;
            tooltip.text = Builder.Override(clearingTooltip.Translate().GetText(), "\n", mistTooltip.Translate().GetText()).ToString();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            tooltipObject.SetActive(true);
            pointerEnterSound.Play();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            tooltipObject.SetActive(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (Save.AssertInstance(out Save save) == false)
                return;
            
            if (save.NemaExhaustion >= 1f)
            {
                invalidClickSound.Play();
                return;
            }
            
            pointerClickSound.Play();
            save.SetNemaClearingMist(!save.IsNemaClearingMist);
        }
    }
}