using System;
using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Combat.Scripts.Managers;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Interfaces;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utils.Extensions;
using Utils.Math;

namespace Core.Combat.Scripts.UI.Selected
{
    public class SelectedCharacterInterface : MonoBehaviour
    {
        private static readonly StringBuilder Builder = new();
        
        [SerializeField, Required, SceneObjectsOnly]
        private CombatManager combatManager;

        [SerializeField, Required, SceneObjectsOnly]
        private CombatInputManager inputHandler;

        [SerializeField, Required, SceneObjectsOnly]
        private CanvasGroup canvasGroup;

        [SerializeField, Required]
        private SkillButton[] skillButtons = new SkillButton[4];

        public SkillButton GetSkillButton(int index)
        {
            if (index < 0 || index >= skillButtons.Length)
                throw new ArgumentException($"Index must be between 0 and {skillButtons.Length}.", nameof(index));

            return skillButtons[index];
        }

        [SerializeField, Required, SceneObjectsOnly]
        private Animator portraitAnimator;

        [SerializeField, Required, SceneObjectsOnly]
        private Image portraitImage;

        [SerializeField, Required, SceneObjectsOnly]
        private TMP_Text nameTmp, raceTmp;

        [SerializeField, Required, SceneObjectsOnly]
        private Slider staminaGauge, lustLowGauge, lustHighGauge, temptationGauge;

        [SerializeField, Required, SceneObjectsOnly]
        private TMP_Text staminaMouseOverTmp, lustMouseOverTmp, temptationMouseOverTmp;

        [SerializeField, Required, SceneObjectsOnly]
        private Toggle orgasmBoxPrefab;

        [SerializeField, Required, SceneObjectsOnly]
        private Transform orgasmBoxesParent;

        [SerializeField, Required, SceneObjectsOnly]
        private GameObject resilienceObject, composureObject;

        [SerializeField, Required, SceneObjectsOnly]
        private TMP_Text resilienceTmp, composureTmp;

        [SerializeField, Required, SceneObjectsOnly]
        private TMP_Text damageTmp, speedTmp;

        [SerializeField, Required, SceneObjectsOnly]
        private TMP_Text accuracyTmp, criticalTmp, dodgeTmp;

        [SerializeField, Required, SceneObjectsOnly]
        private TMP_Text stunRecoverySpeedTmp;

        [SerializeField, Required, SceneObjectsOnly]
        private TMP_Text debuffResistanceTmp, moveResistanceTmp, poisonResistanceTmp;

        [SerializeField, Required, SceneObjectsOnly]
        private TMP_Text debuffApplyChanceTmp, moveApplyChanceTmp,  poisonApplyChanceTmp;

        private readonly List<Toggle> _spawnedOrgasmBoxes = new(16);

        private void Start()
        {
            AssignMouseOverTrigger(interactable: staminaGauge.gameObject, toToggle: staminaMouseOverTmp.gameObject);
            AssignMouseOverTrigger(interactable: lustLowGauge.gameObject, toToggle: lustMouseOverTmp.gameObject);
            AssignMouseOverTrigger(interactable: temptationGauge.gameObject, toToggle: temptationMouseOverTmp.gameObject);
            
            inputHandler.SelectedCharacter.Changed += UpdateInterface;
            combatManager.RelevantPropertiesChanged += UpdateInterface;
            inputHandler.PlayerCharacterIdle += UpdateInterface;
            UpdateInterface();
            
            static void AssignMouseOverTrigger(GameObject interactable, GameObject toToggle)
            {
                EventTrigger trigger = interactable.gameObject.AddComponent<EventTrigger>();
                EventTrigger.Entry mouseOverEntry = new() { eventID = EventTriggerType.PointerEnter };
                mouseOverEntry.callback.AddListener(_ => toToggle.gameObject.SetActive(true));
                trigger.triggers.Add(mouseOverEntry);
            
                EventTrigger.Entry mouseExitEntry = new() { eventID = EventTriggerType.PointerExit };
                mouseExitEntry.callback.AddListener(_ => toToggle.gameObject.SetActive(false));
                trigger.triggers.Add(mouseExitEntry);
            }
        }

        private void OnDestroy()
        {
            if (combatManager == null)
                return;
            
            inputHandler.SelectedCharacter.Changed -= UpdateInterface;
            combatManager.RelevantPropertiesChanged -= UpdateInterface;
            inputHandler.PlayerCharacterIdle -= UpdateInterface;
        }

        private void UpdateInterface() => UpdateInterface(caster: inputHandler.SelectedCharacter.Value);

        private void UpdateInterface(CharacterStateMachine caster)
        {
            if (caster == null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            
                for (int index = 0; index < skillButtons.Length; index++)
                {
                    SkillButton skillButton = skillButtons[index];
                    skillButton.ResetMe();
                }

                return;
            }

            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;

            if (caster.Script.GetPortraitAnimation.TrySome(out RuntimeAnimatorController portraitAnimation))
            {
                portraitAnimator.runtimeAnimatorController = portraitAnimation;
            }
            else
            {
                portraitAnimator.runtimeAnimatorController = null;
                portraitImage.sprite = caster.Script.LustPromptPortrait;
            }
            
            nameTmp.text = caster.Script.CharacterName;
            raceTmp.text = caster.Script.Race.UpperCaseName();
            
            UpdateStaminaAndResilience(caster);
            
            IStatsModule statsModule = caster.StatsModule;
            UpdateDamage(statsModule);
            UpdateAccuracy(statsModule);
            UpdateCritical(statsModule);
            UpdateDodge(statsModule);
            UpdateSpeed(statsModule);

            IResistancesModule resistances = caster.ResistancesModule;
            UpdateStunRecoverySpeed(resistances);
            UpdateDebuffResistance(resistances);
            UpdatePoisonResistance(resistances);
            UpdateMoveResistance(resistances);
            
            IStatusApplierModule statusApplier = caster.StatusApplierModule;
            UpdateDebuffApplyChance(statusApplier);
            UpdatePoisonApplyChance(statusApplier);
            UpdateMoveApplyChance(statusApplier);

            UpdateLustStats(caster);

            if ((CombatManager.DEBUGMODE == false || Application.isEditor == false) && caster.Script.IsControlledByPlayer == false)
            {
                for (int index = 0; index < skillButtons.Length; index++)
                    skillButtons[index].ResetMe();

                return;
            }

            IReadOnlyList<ISkill> skills = caster.Script.Skills;
            int skillIndex = 0;
            for (; skillIndex < skills.Count && skillIndex < skillButtons.Length; skillIndex++)
                skillButtons[skillIndex].SetSkill(skills[skillIndex]);

            for (; skillIndex < skillButtons.Length; skillIndex++)
                skillButtons[skillIndex].ResetMe();
            
            bool canCharacterAct = caster.StateEvaluator.PureEvaluate() is CharacterState.Idle;
            for (int i = 0; i < skillButtons.Length; i++)
            {
                SkillButton skillButton = skillButtons[i];
                if (skillButton.Skill.IsSome)
                    skillButton.SetInteractable(canCharacterAct && skillButton.Skill.Value.FullCastingOk(caster));
            }
        }

        private void UpdateStaminaAndResilience(CharacterStateMachine caster)
        {
            if (caster.StaminaModule.TrySome(out IStaminaModule staminaModule) == false)
            {
                staminaGauge.gameObject.SetActive(false);
                staminaMouseOverTmp.gameObject.SetActive(false);
                resilienceObject.SetActive(false);
                return;
            }
            
            staminaGauge.gameObject.SetActive(true);
            resilienceObject.SetActive(true);

            uint currentMax = staminaModule.ActualMax;
            uint baseMax = staminaModule.BaseMax;
            uint current = staminaModule.GetCurrent();
            staminaGauge.value = (float) current / currentMax;
            Builder.Override(staminaModule.GetCurrent().ToString("0"), " / ", currentMax.ToString("0"));
            
            if (currentMax > baseMax)
                Builder.Surround(ColorReferences.BuffedRichText);
            else if (currentMax < baseMax)
                Builder.Surround(ColorReferences.DebuffedRichText);

            staminaMouseOverTmp.text = Builder.ToString();

            float currentResilience = staminaModule.GetResilience();
            float baseResilience = staminaModule.BaseResilience;
            Builder.Override(currentResilience.ToPercentlessString(digits: 2, decimalDigits: 0));

            if (currentResilience > baseResilience)
                Builder.Surround(ColorReferences.BuffedRichText);
            else if (currentResilience < baseResilience)
                Builder.Surround(ColorReferences.DebuffedRichText);

            resilienceTmp.text = Builder.ToString();
        }

        private void UpdateLustStats(CharacterStateMachine caster)
        {
            if (caster.LustModule.TrySome(out ILustModule lustModule) == false)
            {
                portraitImage.sprite = caster.Script.LustPromptPortrait;
                composureTmp.text = string.Empty;
                composureObject.SetActive(false);
                lustMouseOverTmp.gameObject.SetActive(false);
                lustLowGauge.gameObject.SetActive(false);
                lustHighGauge.gameObject.SetActive(false);
                temptationGauge.gameObject.SetActive(false);
                temptationMouseOverTmp.gameObject.SetActive(false);
                orgasmBoxesParent.gameObject.SetActive(false);
                return;
            }

            composureObject.SetActive(true);
            lustLowGauge.gameObject.SetActive(true);
            lustHighGauge.gameObject.SetActive(true);
            temptationGauge.gameObject.SetActive(true);
            orgasmBoxesParent.gameObject.SetActive(true);

            float baseComposure = lustModule.BaseComposure;
            float currentComposure = lustModule.GetComposure();
            Builder.Override(currentComposure.ToPercentlessString(digits: 2, decimalDigits: 0));

            if (currentComposure > baseComposure)
                Builder.Surround(ColorReferences.BuffedRichText);
            else if (currentComposure < baseComposure)
                Builder.Surround(ColorReferences.DebuffedRichText);

            composureTmp.text = Builder.ToString();

            uint currentLust = lustModule.GetLust();
            const uint maxLust = ILustModule.MaxLust;
            lustMouseOverTmp.text = Builder.Override(currentLust.ToString("0"), " / ", maxLust.ToString("0")).ToString();

            if (currentLust < 100)
            {
                lustLowGauge.value = currentLust / (maxLust / 2f);
                lustHighGauge.value = 0f;
            }
            else
            {
                lustLowGauge.value = 1f;
                lustHighGauge.value = (currentLust - 100f) / (maxLust / 2f);
            }
            
            temptationGauge.value = lustModule.GetTemptation() / 100f;
            temptationMouseOverTmp.text = lustModule.GetTemptation().ToString();

            uint orgasmLimit = lustModule.OrgasmLimit;
            uint orgasmCount = lustModule.GetOrgasmCount();

            for (int i = _spawnedOrgasmBoxes.Count; i < orgasmLimit; i++)
            {
                Toggle box = orgasmBoxPrefab.InstantiateWithFixedLocalScale(orgasmBoxesParent);
                _spawnedOrgasmBoxes.Add(box);
            }

            for (int i = (int)orgasmLimit; i < _spawnedOrgasmBoxes.Count; i++)
                _spawnedOrgasmBoxes[i].gameObject.SetActive(false);

            for (int i = (int)(orgasmLimit - orgasmCount); i < orgasmLimit; i++)
            {
                Toggle box = _spawnedOrgasmBoxes[i];
                box.gameObject.SetActive(true);
                box.isOn = false;
            }

            for (int i = 0; i < orgasmCount; i++)
            {
                Toggle box = _spawnedOrgasmBoxes[i];
                box.gameObject.SetActive(true);
                box.isOn = true;
            }
        }

        private void UpdateDamage(IStatsModule stats)
        {
            (uint lower, uint upper) baseDamage = (stats.BaseDamageLower, stats.BaseDamageUpper);
            (uint lower, uint upper) currentDamage = stats.GetDamageWithMultiplierRounded();
            Builder.Override(currentDamage.ToDamageFormat());

            if (currentDamage.lower > baseDamage.lower || currentDamage.upper > baseDamage.upper)
                Builder.Surround(ColorReferences.BuffedRichText);
            else if (currentDamage.lower < baseDamage.lower || currentDamage.upper < baseDamage.upper)
                Builder.Surround(ColorReferences.DebuffedRichText);

            damageTmp.text = Builder.ToString();
        }

        private void UpdateAccuracy(IStatsModule stats)
        {
            float baseAccuracy = stats.BaseAccuracy;
            float currentAccuracy = stats.GetAccuracy();
            Builder.Override(currentAccuracy.ToPercentlessString(digits: 2, decimalDigits: 0));

            if (currentAccuracy > baseAccuracy)
                Builder.Surround(ColorReferences.BuffedRichText);
            else if (currentAccuracy < baseAccuracy)
                Builder.Surround(ColorReferences.DebuffedRichText);

            accuracyTmp.text = Builder.ToString();
        }

        private void UpdateCritical(IStatsModule stats)
        {
            float baseCritical = stats.BaseCriticalChance;
            float currentCritical = stats.GetCriticalChance();
            Builder.Override(currentCritical.ToPercentlessString(digits: 2, decimalDigits: 0));
            
            if (currentCritical > baseCritical)
                Builder.Surround(ColorReferences.BuffedRichText);
            else if (currentCritical < baseCritical)
                Builder.Surround(ColorReferences.DebuffedRichText);

            criticalTmp.text = Builder.ToString();
        }

        private void UpdateDodge(IStatsModule stats)
        {
            float baseDodge = stats.BaseDodge;
            float currentDodge = stats.GetDodge();
            Builder.Override(currentDodge.ToPercentlessString(digits: 2, decimalDigits: 0));

            if (currentDodge > baseDodge)
                Builder.Surround(ColorReferences.BuffedRichText);
            else if (currentDodge < baseDodge)
                Builder.Surround(ColorReferences.DebuffedRichText);

            dodgeTmp.text = Builder.ToString();
        }

        private void UpdateSpeed(IStatsModule stats)
        {
            float baseSpeed = stats.BaseSpeed;
            float currentSpeed = stats.GetSpeed();
            Builder.Override(currentSpeed.ToPercentlessString(digits: 2, decimalDigits: 0));

            if (currentSpeed > baseSpeed)
                Builder.Surround(ColorReferences.BuffedRichText);
            else if (currentSpeed < baseSpeed)
                Builder.Surround(ColorReferences.DebuffedRichText);

            speedTmp.text = Builder.ToString();
        }

        private void UpdateStunRecoverySpeed(IResistancesModule resistances)
        {
            float baseStunRecoverySpeed = resistances.BaseStunRecoverySpeed;
            float currentStunRecoverySpeed = resistances.GetStunRecoverySpeed();
            Builder.Override(currentStunRecoverySpeed.ToPercentlessString(digits: 2, decimalDigits: 0));

            if (currentStunRecoverySpeed > baseStunRecoverySpeed)
                Builder.Surround(ColorReferences.BuffedRichText);
            else if (currentStunRecoverySpeed < baseStunRecoverySpeed)
                Builder.Surround(ColorReferences.DebuffedRichText);

            stunRecoverySpeedTmp.text = Builder.ToString();
        }

        private void UpdateDebuffResistance(IResistancesModule resistances)
        {
            float baseDebuffResistance = resistances.BaseDebuffResistance;
            float currentDebuffResistance = resistances.GetDebuffResistance();
            Builder.Override(currentDebuffResistance.ToPercentlessString(digits: 2, decimalDigits: 0));

            if (currentDebuffResistance > baseDebuffResistance)
                Builder.Surround(ColorReferences.BuffedRichText);
            else if (currentDebuffResistance < baseDebuffResistance)
                Builder.Surround(ColorReferences.DebuffedRichText);

            debuffResistanceTmp.text = Builder.ToString();
        }

        private void UpdateMoveResistance(IResistancesModule resistances)
        {
            float baseMoveResistance = resistances.BaseMoveResistance;
            float currentMoveResistance = resistances.GetMoveResistance();
            Builder.Override(currentMoveResistance.ToPercentlessString(digits: 2, decimalDigits: 0));

            if (currentMoveResistance > baseMoveResistance)
                Builder.Surround(ColorReferences.BuffedRichText);
            else if (currentMoveResistance < baseMoveResistance)
                Builder.Surround(ColorReferences.DebuffedRichText);

            moveResistanceTmp.text = Builder.ToString();
        }

        private void UpdatePoisonResistance(IResistancesModule resistances)
        {
            float basePoisonResistance = resistances.BasePoisonResistance;
            float currentPoisonResistance = resistances.GetPoisonResistance();
            Builder.Override(currentPoisonResistance.ToPercentlessString(digits: 2, decimalDigits: 0));
            
            if (currentPoisonResistance > basePoisonResistance)
                Builder.Surround(ColorReferences.BuffedRichText);
            else if (currentPoisonResistance < basePoisonResistance)
                Builder.Surround(ColorReferences.DebuffedRichText);
            
            poisonResistanceTmp.text = Builder.ToString();
        }

        private void UpdateDebuffApplyChance(IStatusApplierModule statusApplier)
        {
            float baseApplyChance = statusApplier.BaseDebuffApplyChance;
            float currentApplyChance = statusApplier.GetDebuffApplyChance();
            Builder.Override(currentApplyChance.ToPercentlessString(digits: 2, decimalDigits: 0));
            
            if (currentApplyChance > baseApplyChance)
                Builder.Surround(ColorReferences.BuffedRichText);
            else if (currentApplyChance < baseApplyChance)
                Builder.Surround(ColorReferences.DebuffedRichText);
         
            debuffApplyChanceTmp.text = Builder.ToString();
        }
        
        private void UpdateMoveApplyChance(IStatusApplierModule statusApplier)
        {
            float baseApplyChance = statusApplier.BaseMoveApplyChance;
            float currentApplyChance = statusApplier.GetMoveApplyChance();
            Builder.Override(currentApplyChance.ToPercentlessString(digits: 2, decimalDigits: 0));
            
            if (currentApplyChance > baseApplyChance)
                Builder.Surround(ColorReferences.BuffedRichText);
            else if (currentApplyChance < baseApplyChance)
                Builder.Surround(ColorReferences.DebuffedRichText);
            
            moveApplyChanceTmp.text = Builder.ToString();
        }
        
        private void UpdatePoisonApplyChance(IStatusApplierModule statusApplier)
        {
            float baseApplyChance = statusApplier.BasePoisonApplyChance;
            float currentApplyChance = statusApplier.GetPoisonApplyChance();
            Builder.Override(currentApplyChance.ToPercentlessString(digits: 2, decimalDigits: 0));

            if (currentApplyChance > baseApplyChance)
                Builder.Surround(ColorReferences.BuffedRichText);
            else if (currentApplyChance < baseApplyChance)
                Builder.Surround(ColorReferences.DebuffedRichText);

            poisonApplyChanceTmp.text = Builder.ToString();
        }
    }
}