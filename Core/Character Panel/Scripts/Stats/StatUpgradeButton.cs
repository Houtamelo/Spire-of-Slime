using System;
using Save_Management;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utils.Math;
using Utils.Patterns;

namespace Core.Character_Panel.Scripts.Stats
{
    public sealed class StatUpgradeButton : MonoBehaviour, IPointerEnterHandler
    {
        [SerializeField] 
        private TMP_Text abbreviationTmp, valueTmp;
        
        [SerializeField] 
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

        public void Initialize(PrimaryUpgrade primaryUpgrade, IReadonlyCharacterStats characterStats)
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

            abbreviationTmp.text = primaryUpgrade switch
            {
                PrimaryUpgrade.Accuracy   => "Accuracy",
                PrimaryUpgrade.Dodge      => "Dodge",
                PrimaryUpgrade.Critical   => "Critical",
                PrimaryUpgrade.Resilience => "Resilience",
                _                         => throw new ArgumentOutOfRangeException(nameof(primaryUpgrade), primaryUpgrade, null)
            };

            uint currentTier = characterStats.PrimaryUpgrades[primaryUpgrade];

            float current = characterStats.GetValue(primaryUpgrade);
            float bonus = UpgradeHelper.GetUpgradeIncrement(currentTier, primaryUpgrade);
            valueTmp.text = $"{current.ToPercentageString()} => {(current + bonus).ToPercentageString()}";
            
            gameObject.SetActive(true);
        }

        public void Initialize(SecondaryUpgrade secondaryUpgrade, IReadonlyCharacterStats characterStats)
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
            
            abbreviationTmp.text = secondaryUpgrade switch
            {
                SecondaryUpgrade.Composure         => "Composure",
                SecondaryUpgrade.StunRecoverySpeed => "Stun Recovery",
                SecondaryUpgrade.MoveResistance    => "Move Res",
                SecondaryUpgrade.DebuffResistance  => "Debuff Res",
                SecondaryUpgrade.PoisonResistance  => "Poison Res",
                SecondaryUpgrade.PoisonApplyChance => "Poison Apply Chance",
                SecondaryUpgrade.DebuffApplyChance => "Debuff Apply Chance",
                SecondaryUpgrade.MoveApplyChance   => "Move Apply Chance",
                _                                  => throw new ArgumentOutOfRangeException(nameof(secondaryUpgrade), secondaryUpgrade, null)
            };
            
            uint currentTier = characterStats.SecondaryUpgrades[secondaryUpgrade];
            
            float current = characterStats.GetValue(secondaryUpgrade);
            float bonus = UpgradeHelper.GetUpgradeIncrement(currentTier, secondaryUpgrade);

            valueTmp.text = $"{current.ToPercentageString()} => {(bonus + current).ToPercentageString()}";
            
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