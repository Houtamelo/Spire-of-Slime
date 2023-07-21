using System;
using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Behaviour.Modules;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Managers;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Combat.Scripts.Timeline;
using Core.Utils.Collections;
using Core.Utils.Extensions;
using Core.Utils.Math;
using Core.Utils.Patterns;
using JetBrains.Annotations;
using ListPool;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Combat.Scripts.UI.Highlighted
{
    public sealed class HighlightedCharacterInterface : MonoBehaviour
    {
        private static readonly StringBuilder Builder = new();
        
        [SerializeField, Required, SceneObjectsOnly]
        private CombatManager combatManager;

        [SerializeField, Required, SceneObjectsOnly]
        private CombatInputManager inputHandler;

        [SerializeField, Required, SceneObjectsOnly]
        private TimelineManager timelineManager;

        [SerializeField, Required, SceneObjectsOnly]
        private CanvasGroup canvasGroup;

        [SerializeField, Required] 
        private TMP_Text[] skillDisplayers;

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
        private TMP_Text stunMitigationTmp;
        
        [SerializeField, Required, SceneObjectsOnly]
        private TMP_Text dodgeTmp;
        
        [SerializeField, Required, SceneObjectsOnly]
        private TMP_Text debuffResistanceTmp, moveResistanceTmp, poisonResistanceTmp;

        [SerializeField, Required, SceneObjectsOnly]
        private TargetingInfoBox targetingInfoBox;
        
        private readonly List<Toggle> _spawnedOrgasmBoxes = new(16);

        private void Start()
        {
            inputHandler.HighlightedCharacter.Changed += UpdateInterface;
            combatManager.RelevantPropertiesChanged += UpdateInterface;
            UpdateInterface();
        }
        private void OnDestroy()
        {
            if (combatManager == null)
                return;
            
            inputHandler.HighlightedCharacter.Changed -= UpdateInterface;
            combatManager.RelevantPropertiesChanged -= UpdateInterface;
        }

        private void UpdateInterface() => UpdateInterface(target: inputHandler.HighlightedCharacter.Value);

        private void UpdateInterface([CanBeNull] CharacterStateMachine target)
        {
            ISkill skill = inputHandler.SelectedSkill.Value;
            CharacterStateMachine caster = inputHandler.SelectedCharacter.Value;
            
            if (target == null)
            {
                canvasGroup.alpha = 0;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
                targetingInfoBox.Hide();
                timelineManager.HideTemporaryTargetingIcon();
                return;
            }
            
            canvasGroup.alpha = 1;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;

            if (target.Script.GetPortraitAnimation.TrySome(out RuntimeAnimatorController portraitAnimation))
            {
                portraitAnimator.runtimeAnimatorController = portraitAnimation;
            }
            else
            {
                portraitAnimator.runtimeAnimatorController = null;
                portraitImage.sprite = target.Script.GetPortrait.SomeOrDefault();
            }

            nameTmp.text = target.Script.CharacterName.Translate().GetText();
            raceTmp.text = target.Script.Race.UpperCaseName().Translate().GetText();

            UpdateStaminaAndResilience(target);
            UpdateDodge(target.StatsModule);

            IResistancesModule resistances = target.ResistancesModule;
            UpdateDebuffResistance(resistances);
            UpdatePoisonResistance(resistances);
            UpdateMoveResistance(resistances);
            
            UpdateStunMitigation(target.StunModule);
            
            UpdateLustStats(target);
            
            ReadOnlySpan<ISkill> skills = target.Script.Skills;
            int i = 0;
            for (; i < skills.Length && i < skillDisplayers.Length; i++)
                skillDisplayers[i].text = skills[i].DisplayName.Translate().GetText();
            for (; i < skillDisplayers.Length; i++) 
                skillDisplayers[i].text = string.Empty;

            if (caster == null
             || (CombatManager.DEBUGMODE == false && (caster.Script.IsControlledByPlayer == false || caster.PositionHandler.IsLeftSide == false))
             || skill == null
             || skill.FullCastingAndTargetingOk(caster, target) == false)
            {
                targetingInfoBox.Hide();
                return;
            }

            UpdateTargetingInfoBox(target, skill, caster);

            if (caster.StateEvaluator.PureEvaluate() is not CharacterState.Idle)
                return;

            ChargeStruct chargeStruct = new(skill, caster, target);
            caster.SkillModule.ModifyCharge(ref chargeStruct);

            TSpan chargeTime = caster.ChargeModule.EstimateCharge(chargeStruct.Charge);

            if (chargeTime.Ticks > 0)
                timelineManager.ShowTemporaryTargetingIcon(caster, chargeTime, skill);

            chargeStruct.Dispose();
        }

        private void UpdateTargetingInfoBox(CharacterStateMachine target, ISkill skill, CharacterStateMachine caster)
        {
            SkillStruct skillStruct = SkillStruct.CreateInstance(skill, caster, target);
            ref CustomValuePooledList<TargetProperties> allProperties = ref skillStruct.TargetProperties;

            if (allProperties.Count == 0)
            {
                targetingInfoBox.Hide();
                return;
            }

            ReadOnlyProperties targetProperties = allProperties[0].ToReadOnly();
            Option<int> rawHitChance = SkillCalculator.FinalHitChance(ref skillStruct, targetProperties);
            Option<int> rawCriticalChance = SkillCalculator.FinalCriticalChance(ref skillStruct, targetProperties);
            Option<(int lowerDamage, int upperDamage)> rawDamage = SkillCalculator.FinalDamage(ref skillStruct, targetProperties, crit: false);

            skillStruct.ApplyCustomStats();
            caster.SkillModule.ModifySkill(ref skillStruct);

            Option<int> hitChance = SkillCalculator.FinalHitChance(ref skillStruct, targetProperties);
            Option<int> criticalChance = SkillCalculator.FinalCriticalChance(ref skillStruct, targetProperties);
            Option<(int lowerDamage, int upperDamage)> damage = SkillCalculator.FinalDamage(ref skillStruct, targetProperties, crit: false);

            ComparisonResult hitChanceComparison;

            if (rawHitChance == hitChance)
                hitChanceComparison = ComparisonResult.Equals;
            else if (hitChance.IsSome && (rawHitChance.IsNone || rawHitChance.Value < hitChance.Value))
                hitChanceComparison = ComparisonResult.Bigger;
            else
                hitChanceComparison = ComparisonResult.Smaller;

            ComparisonResult criticalChanceComparison;

            if (rawCriticalChance == criticalChance)
                criticalChanceComparison = ComparisonResult.Equals;
            else if (criticalChance.IsSome && (rawCriticalChance.IsNone || rawCriticalChance.Value < criticalChance.Value))
                criticalChanceComparison = ComparisonResult.Bigger;
            else
                criticalChanceComparison = ComparisonResult.Smaller;

            ComparisonResult damageComparison;

            if (rawDamage == damage)
                damageComparison = ComparisonResult.Equals;
            else if (damage.IsSome && (rawDamage.IsNone || rawDamage.Value.lowerDamage < damage.Value.lowerDamage || rawDamage.Value.upperDamage < damage.Value.upperDamage))
                damageComparison = ComparisonResult.Bigger;
            else
                damageComparison = ComparisonResult.Smaller;

            targetingInfoBox.UpdateInterface(ref skillStruct, hitChance, hitChanceComparison, criticalChance, criticalChanceComparison, damage, damageComparison);
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
    }
}