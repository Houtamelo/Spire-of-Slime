using System;
using System.Text;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using Core.Utils.Math;
using Core.Utils.Patterns;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Core.Character_Panel.Scripts.Stats
{
    public sealed class StatUpgradeButton : MonoBehaviour, IPointerEnterHandler
    {
        private static readonly StringBuilder Builder = new();
        
        [SerializeField, Required] 
        private TMP_Text abbreviationTmp, valueTmp;
        
        [SerializeField, Required]
        private Button button;
        
        private AudioSource _pointerEnterSource, _pointerClickSource, _invalidClickSource;

        private bool _audioSourcesAssigned;
        private bool _initialized;
        private bool _isPrimary;
        private PrimaryUpgrade _primaryUpgrade;
        private SecondaryUpgrade _secondaryUpgrade;

        private void Start()
        {
            button.onClick.AddListener(OnClick);
        }

        public void AssignAudioSources(AudioSource onPointerEnter, AudioSource onPointerClick, AudioSource onInvalidClick)
        {
            _pointerEnterSource = onPointerEnter;
            _pointerClickSource = onPointerClick;
            _invalidClickSource = onInvalidClick;
            _audioSourcesAssigned = true;
        }

        public void Initialize(PrimaryUpgrade primaryUpgrade, [NotNull] IReadonlyCharacterStats characterStats)
        {
            if (characterStats.AvailablePrimaryPoints <= 0)
            {
                ResetMe();
                return;
            }
            
            button.interactable = true;
            _initialized = true;
            _isPrimary = true;
            _primaryUpgrade = primaryUpgrade;

            abbreviationTmp.text = primaryUpgrade.UpperCaseName().Translate().GetText();

            int currentTier = characterStats.PrimaryUpgrades[primaryUpgrade];

            int current = characterStats.GetValue(primaryUpgrade);
            int bonus = UpgradeHelper.GetUpgradeIncrement(currentTier, primaryUpgrade);
            valueTmp.text = Builder.Override(current.ToString(), " => ", (current + bonus).ToString()).ToString();
            
            gameObject.SetActive(true);
        }

        public void Initialize(SecondaryUpgrade secondaryUpgrade, [NotNull] IReadonlyCharacterStats characterStats)
        {
            if (characterStats.AvailableSecondaryPoints <= 0)
            {
                ResetMe();
                return;
            }
            
            button.interactable = true;
            _initialized = true;
            _isPrimary = false;
            _secondaryUpgrade = secondaryUpgrade;

            abbreviationTmp.text = secondaryUpgrade.UpperCaseName().Translate().GetText();
            
            int currentTier = characterStats.SecondaryUpgrades[secondaryUpgrade];
            
            int current = characterStats.GetValue(secondaryUpgrade);
            int bonus = UpgradeHelper.GetUpgradeIncrement(currentTier, secondaryUpgrade);

            valueTmp.text = Builder.Override(current.ToString(), " => ", (bonus + current).ToString()).ToString();
            
            gameObject.SetActive(true);
        }

        public void ResetMe()
        {
            _initialized = false;
            gameObject.SetActive(false);
        }

        private void OnClick()
        {
            if (!_initialized)
            {
                Debug.LogWarning("Button is not initialized!", this);
                return;
            }

            Option<StatsPanel> instance = StatsPanel.Instance;
            if (instance.IsNone)
            {
                Debug.LogWarning("Stats panel is not initialized!", this);
                return;
            }

            bool success = _isPrimary ? instance.Value.UpgradePrimary(_primaryUpgrade) : instance.Value.UpgradeSecondary(_secondaryUpgrade);
            if (success)
                _pointerClickSource.Play();
            else
                _invalidClickSource.Play();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_audioSourcesAssigned)
                _pointerEnterSource.Play();
            else
                Debug.LogWarning("Audio sources are not assigned!", this);
        }
    }
}