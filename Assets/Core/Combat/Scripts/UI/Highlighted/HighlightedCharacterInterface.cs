using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Combat.Scripts.Managers;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Interfaces;
using ListPool;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils.Extensions;
using Utils.Math;
using Utils.Patterns;

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
        private TMP_Text stunRecoverySpeedTmp;
        
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

        private void UpdateInterface(CharacterStateMachine target)
        {
            ISkill skill = inputHandler.SelectedSkill.Value;
            CharacterStateMachine caster = inputHandler.SelectedCharacter.Value;
            
            if (target == null)
            {
                caster?.ForceUpdateTimelineCue();
                canvasGroup.alpha = 0;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
                targetingInfoBox.Hide();
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
                portraitImage.sprite = target.Script.LustPromptPortrait;
            }

            nameTmp.text = target.Script.CharacterName;
            raceTmp.text = target.Script.Race.UpperCaseName();

            UpdateStaminaAndResilience(target);
            UpdateDodge(target.StatsModule);

            IResistancesModule resistances = target.ResistancesModule;
            UpdateStunRecoverySpeed(resistances);
            UpdateDebuffResistance(resistances);
            UpdatePoisonResistance(resistances);
            UpdateMoveResistance(resistances);
            
            UpdateLustStats(target);
            
            IReadOnlyList<ISkill> skills = target.Script.Skills;
            int i = 0;
            for (; i < skills.Count && i < skillDisplayers.Length; i++)
                skillDisplayers[i].text = skills[index: i].DisplayName;
            for (; i < skillDisplayers.Length; i++) 
                skillDisplayers[i].text = string.Empty;

            if (caster == null || (CombatManager.DEBUGMODE == false && (caster.Script.IsControlledByPlayer == false || caster.PositionHandler.IsLeftSide == false)) || skill == null ||
                skill.FullCastingAndTargetingOk(caster, target) == false)
            {
                targetingInfoBox.Hide();
                return;
            }
            
            SkillStruct skillStruct = SkillStruct.CreateInstance(skill, caster, target);
            ref ValueListPool<TargetProperties> allProperties = ref skillStruct.TargetProperties;
            if (allProperties.Count == 0)
            {
                targetingInfoBox.Hide();
            }
            else
            {
                ReadOnlyProperties targetProperties = allProperties[0].ToReadOnly();
                Option<float> rawHitChance = SkillUtils.GetHitChance(ref skillStruct, targetProperties);
                Option<float> rawCriticalChance = SkillUtils.GetCriticalChance(ref skillStruct, targetProperties);
                Option<(uint lowerDamage, uint upperDamage)> rawDamage = SkillUtils.GetDamage(ref skillStruct, targetProperties, crit: false);

                skillStruct.ApplyCustomStats();
                caster.SkillModule.ModifySkill(ref skillStruct);

                Option<float> hitChance = SkillUtils.GetHitChance(ref skillStruct, targetProperties);
                Option<float> criticalChance = SkillUtils.GetCriticalChance(ref skillStruct, targetProperties);
                Option<(uint lowerDamage, uint upperDamage)> damage = SkillUtils.GetDamage(ref skillStruct, targetProperties, crit: false);

                ComparisonResult hitChanceComparison;
                ComparisonResult criticalChanceComparison;
                ComparisonResult damageComparison;

                if (rawHitChance == hitChance)
                    hitChanceComparison = ComparisonResult.Equals;
                else if (hitChance.IsSome && (rawHitChance.IsNone || rawHitChance.Value < hitChance.Value))
                    hitChanceComparison = ComparisonResult.Bigger;
                else
                    hitChanceComparison = ComparisonResult.Smaller;

                if (rawCriticalChance == criticalChance)
                    criticalChanceComparison = ComparisonResult.Equals;
                else if (criticalChance.IsSome && (rawCriticalChance.IsNone || rawCriticalChance.Value < criticalChance.Value))
                    criticalChanceComparison = ComparisonResult.Bigger;
                else
                    criticalChanceComparison = ComparisonResult.Smaller;

                if (rawDamage == damage)
                    damageComparison = ComparisonResult.Equals;
                else if (damage.IsSome && (rawDamage.IsNone || rawDamage.Value.lowerDamage < damage.Value.lowerDamage || rawDamage.Value.upperDamage < damage.Value.upperDamage))
                    damageComparison = ComparisonResult.Bigger;
                else
                    damageComparison = ComparisonResult.Smaller;

                targetingInfoBox.UpdateInterface(ref skillStruct, hitChance, hitChanceComparison, criticalChance, criticalChanceComparison, damage, damageComparison);
            }

            if (caster.StateEvaluator.PureEvaluate() is not CharacterState.Idle || caster.Display.AssertSome(out CharacterDisplay display) == false)
            {
                caster.ForceUpdateTimelineCue();
            }
            else
            {
                float estimatedRealCharge = skillStruct.Skill.BaseCharge / caster.StatsModule.GetSpeed();
                display.AllowTimelineIcon(true);
                Color color = skill.AllowAllies ? ColorReferences.Buff : ColorReferences.Debuff;
                display.SetTimelineCuePosition(estimatedRealCharge, $"{skill.DisplayName}=>{target.Script.CharacterName}", color);
            }

            skillStruct.Dispose();
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
    }
}