using System;
using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Behaviour.Modules;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Managers;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Utils.Extensions;
using Core.Utils.Math;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
        private TMP_Text stunMitigationTmp;

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
            
            static void AssignMouseOverTrigger([NotNull] GameObject interactable, GameObject toToggle)
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

        private void UpdateInterface([CanBeNull] CharacterStateMachine caster)
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
                portraitImage.sprite = caster.Script.GetPortrait.SomeOrDefault();
            }
            
            nameTmp.text = caster.Script.CharacterName.Translate().GetText();
            raceTmp.text = caster.Script.Race.UpperCaseName().Translate().GetText();
            
            UpdateStaminaAndResilience(caster);
            
            IStatsModule statsModule = caster.StatsModule;
            UpdateDamage(statsModule);
            UpdateAccuracy(statsModule);
            UpdateCritical(statsModule);
            UpdateDodge(statsModule);
            UpdateSpeed(statsModule);

            IResistancesModule resistances = caster.ResistancesModule;
            UpdateDebuffResistance(resistances);
            UpdatePoisonResistance(resistances);
            UpdateMoveResistance(resistances);
            
            UpdateStunMitigation(caster.StunModule);

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

            ReadOnlySpan<ISkill> skills = caster.Script.Skills;
            int skillIndex = 0;
            for (; skillIndex < skills.Length && skillIndex < skillButtons.Length; skillIndex++)
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

        private void UpdateStaminaAndResilience([NotNull] CharacterStateMachine caster)
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

            int currentMax = staminaModule.ActualMax;
            int baseMax = staminaModule.BaseMax;
            int current = staminaModule.GetCurrent();
            staminaGauge.value = (float) current / currentMax;
            Builder.Override(staminaModule.GetCurrent().ToString("0"), " / ", currentMax.ToString("0"));
            
            if (currentMax > baseMax)
                Builder.Surround(ColorReferences.BuffedRichText);
            else if (currentMax < baseMax)
                Builder.Surround(ColorReferences.DebuffedRichText);

            staminaMouseOverTmp.text = Builder.ToString();

            int currentResilience = staminaModule.GetResilience();
            int baseResilience = staminaModule.BaseResilience;
            Builder.Override(currentResilience.ToPercentageStringBase100());

            if (currentResilience > baseResilience)
                Builder.Surround(ColorReferences.BuffedRichText);
            else if (currentResilience < baseResilience)
                Builder.Surround(ColorReferences.DebuffedRichText);

            resilienceTmp.text = Builder.ToString();
        }

        private void UpdateLustStats([NotNull] CharacterStateMachine caster)
        {
            if (caster.LustModule.TrySome(out ILustModule lustModule) == false)
            {
                portraitImage.sprite = caster.Script.GetPortrait.SomeOrDefault();
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

            int baseComposure = lustModule.BaseComposure;
            int currentComposure = lustModule.GetComposure();
            Builder.Override(currentComposure.WithSymbol());

            if (currentComposure > baseComposure)
                Builder.Surround(ColorReferences.BuffedRichText);
            else if (currentComposure < baseComposure)
                Builder.Surround(ColorReferences.DebuffedRichText);

            composureTmp.text = Builder.ToString();

            int currentLust = lustModule.GetLust();
            const int maxLust = ILustModule.MaxLust;
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

            int orgasmLimit = lustModule.GetOrgasmLimit();
            int orgasmCount = lustModule.GetOrgasmCount();

            for (int i = _spawnedOrgasmBoxes.Count; i < orgasmLimit; i++)
            {
                Toggle box = orgasmBoxPrefab.InstantiateWithFixedLocalScale(orgasmBoxesParent);
                _spawnedOrgasmBoxes.Add(box);
            }

            for (int i = orgasmLimit; i < _spawnedOrgasmBoxes.Count; i++)
                _spawnedOrgasmBoxes[i].gameObject.SetActive(false);

            for (int i = orgasmLimit - orgasmCount; i < orgasmLimit; i++)
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

        private void UpdateDamage([NotNull] IStatsModule stats)
        {
            int basePower = stats.BasePower;
            (int lower, int upper) baseDamage = stats.GetBaseDamageRaw();
            baseDamage.lower = (baseDamage.lower * basePower) / 100;
            baseDamage.upper = (baseDamage.upper * basePower) / 100;
            
            int currentPower = stats.GetPower();
            (int lower, int upper) currentDamage = stats.GetBaseDamageRaw();
            currentDamage.lower = (currentDamage.lower * currentPower) / 100;
            currentDamage.upper = (currentDamage.upper * currentPower) / 100;
            
            Builder.Override(currentDamage.ToDamageRangeFormat());

            if (currentDamage.lower > baseDamage.lower || currentDamage.upper > baseDamage.upper)
                Builder.Surround(ColorReferences.BuffedRichText);
            else if (currentDamage.lower < baseDamage.lower || currentDamage.upper < baseDamage.upper)
                Builder.Surround(ColorReferences.DebuffedRichText);

            damageTmp.text = Builder.ToString();
        }

        private void UpdateAccuracy([NotNull] IStatsModule stats)
        {
            int baseAccuracy = stats.BaseAccuracy;
            int currentAccuracy = stats.GetAccuracy();
            Builder.Override(currentAccuracy.WithSymbol());

            if (currentAccuracy > baseAccuracy)
                Builder.Surround(ColorReferences.BuffedRichText);
            else if (currentAccuracy < baseAccuracy)
                Builder.Surround(ColorReferences.DebuffedRichText);

            accuracyTmp.text = Builder.ToString();
        }

        private void UpdateCritical([NotNull] IStatsModule stats)
        {
            int baseCritical = stats.BaseCriticalChance;
            int currentCritical = stats.GetCriticalChance();
            Builder.Override(currentCritical.WithSymbol());
            
            if (currentCritical > baseCritical)
                Builder.Surround(ColorReferences.BuffedRichText);
            else if (currentCritical < baseCritical)
                Builder.Surround(ColorReferences.DebuffedRichText);

            criticalTmp.text = Builder.ToString();
        }

        private void UpdateDodge([NotNull] IStatsModule stats)
        {
            int baseDodge = stats.BaseDodge;
            int currentDodge = stats.GetDodge();
            Builder.Override(currentDodge.WithSymbol());

            if (currentDodge > baseDodge)
                Builder.Surround(ColorReferences.BuffedRichText);
            else if (currentDodge < baseDodge)
                Builder.Surround(ColorReferences.DebuffedRichText);

            dodgeTmp.text = Builder.ToString();
        }

        private void UpdateSpeed([NotNull] IStatsModule stats)
        {
            int baseSpeed = stats.BaseSpeed;
            int currentSpeed = stats.GetSpeed();
            Builder.Override(currentSpeed.ToPercentageStringBase100());

            if (currentSpeed > baseSpeed)
                Builder.Surround(ColorReferences.BuffedRichText);
            else if (currentSpeed < baseSpeed)
                Builder.Surround(ColorReferences.DebuffedRichText);

            speedTmp.text = Builder.ToString();
        }

        private void UpdateStunMitigation([NotNull] IStunModule stunModule)
        {
            int baseStunMitigation = stunModule.BaseStunMitigation;
            int currentStunMitigation = stunModule.GetStunMitigation();
            
            Builder.Override(currentStunMitigation.ToString());

            if (currentStunMitigation > baseStunMitigation)
                Builder.Surround(ColorReferences.BuffedRichText);
            else if (currentStunMitigation < baseStunMitigation)
                Builder.Surround(ColorReferences.DebuffedRichText);

            stunMitigationTmp.text = Builder.ToString();
        }

        private void UpdateDebuffResistance([NotNull] IResistancesModule resistances)
        {
            int baseDebuffResistance = resistances.BaseDebuffResistance;
            int currentDebuffResistance = resistances.GetDebuffResistance();
            Builder.Override(currentDebuffResistance.WithSymbol());

            if (currentDebuffResistance > baseDebuffResistance)
                Builder.Surround(ColorReferences.BuffedRichText);
            else if (currentDebuffResistance < baseDebuffResistance)
                Builder.Surround(ColorReferences.DebuffedRichText);

            debuffResistanceTmp.text = Builder.ToString();
        }

        private void UpdateMoveResistance([NotNull] IResistancesModule resistances)
        {
            int baseMoveResistance = resistances.BaseMoveResistance;
            int currentMoveResistance = resistances.GetMoveResistance();
            Builder.Override(currentMoveResistance.WithSymbol());

            if (currentMoveResistance > baseMoveResistance)
                Builder.Surround(ColorReferences.BuffedRichText);
            else if (currentMoveResistance < baseMoveResistance)
                Builder.Surround(ColorReferences.DebuffedRichText);

            moveResistanceTmp.text = Builder.ToString();
        }

        private void UpdatePoisonResistance([NotNull] IResistancesModule resistances)
        {
            int basePoisonResistance = resistances.BasePoisonResistance;
            int currentPoisonResistance = resistances.GetPoisonResistance();
            Builder.Override(currentPoisonResistance.WithSymbol());
            
            if (currentPoisonResistance > basePoisonResistance)
                Builder.Surround(ColorReferences.BuffedRichText);
            else if (currentPoisonResistance < basePoisonResistance)
                Builder.Surround(ColorReferences.DebuffedRichText);
            
            poisonResistanceTmp.text = Builder.ToString();
        }

        private void UpdateDebuffApplyChance([NotNull] IStatusApplierModule statusApplier)
        {
            int baseApplyChance = statusApplier.BaseDebuffApplyChance;
            int currentApplyChance = statusApplier.GetDebuffApplyChance();
            Builder.Override(currentApplyChance.WithSymbol());
            
            if (currentApplyChance > baseApplyChance)
                Builder.Surround(ColorReferences.BuffedRichText);
            else if (currentApplyChance < baseApplyChance)
                Builder.Surround(ColorReferences.DebuffedRichText);
         
            debuffApplyChanceTmp.text = Builder.ToString();
        }
        
        private void UpdateMoveApplyChance([NotNull] IStatusApplierModule statusApplier)
        {
            int baseApplyChance = statusApplier.BaseMoveApplyChance;
            int currentApplyChance = statusApplier.GetMoveApplyChance();
            Builder.Override(currentApplyChance.WithSymbol());
            
            if (currentApplyChance > baseApplyChance)
                Builder.Surround(ColorReferences.BuffedRichText);
            else if (currentApplyChance < baseApplyChance)
                Builder.Surround(ColorReferences.DebuffedRichText);
            
            moveApplyChanceTmp.text = Builder.ToString();
        }
        
        private void UpdatePoisonApplyChance([NotNull] IStatusApplierModule statusApplier)
        {
            int baseApplyChance = statusApplier.BasePoisonApplyChance;
            int currentApplyChance = statusApplier.GetPoisonApplyChance();
            Builder.Override(currentApplyChance.WithSymbol());

            if (currentApplyChance > baseApplyChance)
                Builder.Surround(ColorReferences.BuffedRichText);
            else if (currentApplyChance < baseApplyChance)
                Builder.Surround(ColorReferences.DebuffedRichText);

            poisonApplyChanceTmp.text = Builder.ToString();
        }
    }
}