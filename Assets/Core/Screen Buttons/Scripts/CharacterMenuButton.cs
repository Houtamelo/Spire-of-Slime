﻿using Core.Character_Panel.Scripts;
using Core.Combat.Scripts.Managers;
using Core.Game_Manager.Scripts;
using Core.Shaders;
using DG.Tweening;
using Save_Management;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utils.Extensions;
using Save = Save_Management.Save;

namespace Core.Screen_Buttons.Scripts
{
    public class CharacterMenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField, Required] 
        private Button button;
        
        [SerializeField, Required]
        private GameObject levelUpIndicator;
        
        [SerializeField, Required]
        private Transform levelUpCueSpawnPoint;

        [SerializeField, Required]
        private GameObject pointsAvailableMessage;
        
        [SerializeField, Required]
        private AudioSource levelUpSound;

        [SerializeField, Required]
        private LighterAnimator levelUpFlasher;

        private Tween _flasherTween;

        private void Start()
        {
            button.onClick.AddListener(() =>
            {
                levelUpIndicator.SetActive(false);
                if (CharacterMenuManager.AssertInstance(out CharacterMenuManager characterMenu))
                    characterMenu.Open();
            });
            
            Save.FloatChanged += OnFloatChanged;
            levelUpFlasher.ResetMaterial();
        }
        private void OnDestroy()
        {
            Save.FloatChanged -= OnFloatChanged;
        }

        public void OnPointerEnter(PointerEventData _)
        {
            _flasherTween.KillIfActive();
            levelUpFlasher.ResetMaterial();
            Save save = Save.Current;
            if (save != null &&
                (save.EthelStats.AvailablePerkPoints > 0 ||
                 save.EthelStats.AvailablePrimaryPoints > 0 ||
                 save.EthelStats.AvailableSecondaryPoints > 0 ||
                 save.NemaStats.AvailablePerkPoints > 0 ||
                 save.NemaStats.AvailablePrimaryPoints > 0 ||
                 save.NemaStats.AvailableSecondaryPoints > 0))
            {
                pointsAvailableMessage.SetActive(true);
            }
        }

        public void OnPointerExit(PointerEventData _) => pointsAvailableMessage.SetActive(false);

        private void OnFloatChanged(CleanString variableName, float oldValue, float newValue)
        {
            if (variableName != VariablesName.Ethel_Experience && variableName != VariablesName.Nema_Experience)
                return;

            int oldLevel = Mathf.FloorToInt(oldValue / ExperienceCalculator.ExperienceNeededForLevelUp);
            int newLevel = Mathf.FloorToInt(newValue / ExperienceCalculator.ExperienceNeededForLevelUp);
            if (oldLevel >= newLevel)
                return;
            
            _flasherTween.KillIfActive();
            levelUpIndicator.SetActive(true);
            _flasherTween = levelUpFlasher.Animate(amplitude: 0.5f, fullLoopCount: 30, Color.white);
            
            if (CombatManager.Instance.IsSome) // Combat shows an experience screen at the end so let it handle this
                return;
            
            levelUpSound.Play();
            WorldCueOptions cueOptions = new(text: "Level Up!", size: 24f, worldPosition: levelUpCueSpawnPoint.position, color: Color.green, stayDuration: 1f,
                                            fadeDuration: 0.5f, speed: Vector3.up * 0.5f, alignment: HorizontalAlignmentOptions.Left, stopOthers: false);

            if (WorldTextCueManager.AssertInstance(out WorldTextCueManager textCueManager))
                textCueManager.Show(cueOptions);
        }
    }
}