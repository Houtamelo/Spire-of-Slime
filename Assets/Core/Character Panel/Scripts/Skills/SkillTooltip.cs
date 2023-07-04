using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Combat.Scripts.UI;
using Core.Pause_Menu.Scripts;
using ListPool;
using NetFabric.Hyperlinq;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Utils.Async;
using Utils.Extensions;
using Utils.Math;
using Utils.Patterns;
using CombatManager = Core.Combat.Scripts.Managers.CombatManager;

namespace Core.Character_Panel.Scripts.Skills
{
    public class SkillTooltip : Singleton<SkillTooltip>
    {
        private static readonly StringBuilder SingleUseBuilder = new();
        
        private static readonly StringBuilder[] StringBuilders = { new(), new(), new(), new() };
        private static readonly StringBuilder EffectsStringBuilder = new();
        private static readonly List<StatusToApply> ReusableStatusList = new();
        private static readonly (CharacterStateMachine target, List<StatusToApply> effects)[] ReusableStatusRecordArray =
        {
            (null, new List<StatusToApply>()),
            (null, new List<StatusToApply>()),
            (null, new List<StatusToApply>()),
            (null, new List<StatusToApply>())
        };
        
        private static readonly Vector3[] ReusableCornersArray = new Vector3[4];

        private readonly List<TooltipSidePanel> _sidePanels = new();

        [SerializeField, Required]
        private Image[] castingPositionCircles = new Image[4];

        [SerializeField, Required]
        private Image[] targetingPositionCircles = new Image[4];

        [SerializeField, Required]
        private Image[] targetingConnectors = new Image[3];
        
        [SerializeField, Required] 
        private Sprite emptyCircle, friendlyCircle, enemyCircle;

        [SerializeField, Required]
        private Sprite friendlyConnector, enemyConnector;

        [SerializeField, Required, SceneObjectsOnly]
        private TMP_Text skillNameText;

        [SerializeField, Required, SceneObjectsOnly]
        private TMP_Text chargeText, recoveryText, accuracyText, dmgMultText, criticalChanceText;

        [SerializeField, Required, SceneObjectsOnly]
        private GameObject accuracyObject, criticalChanceObject, damageMultiplierObject;

        [SerializeField, Required, SceneObjectsOnly]
        private TMP_Text effectsText;

        [SerializeField, Required, SceneObjectsOnly]
        private TMP_Text flavorText;

        [SerializeField, Required, SceneObjectsOnly]
        private RectTransform selfRect;

        [SerializeField, Required, AssetsOnly]
        private TooltipSidePanel sidePanelPrefab;

        [SerializeField, Required, SceneObjectsOnly]
        private Transform sidePanelParent;

        [SerializeField, Required, SceneObjectsOnly]
        private CanvasGroup canvasGroup;

        [SerializeField, Required, SceneObjectsOnly]
        private Camera uiCamera;

        private Action _onTooltipButtonHeld;

        private void Start()
        {
            new CoroutineWrapper(LoadRoutine(), nameof(LoadRoutine), this, true);
            IEnumerator LoadRoutine()
            {
                _onTooltipButtonHeld = Hide;
                while (InputManager.Instance.IsNone)
                    yield return null;

                InputManager.Instance.Value.CanceledActionsCallbacks[InputEnum.HoldTooltip].Add(_onTooltipButtonHeld);
            }
            
            for (int i = 0; i < 4; i++)
            {
                CreateSidePanel();
            }

            canvasGroup.alpha = 1f;
            Hide();
        }

        protected override void OnDestroy()
        {
            if (InputManager.Instance.TrySome(out InputManager inputManager))
                inputManager.CanceledActionsCallbacks[InputEnum.HoldTooltip].Remove(_onTooltipButtonHeld);

            base.OnDestroy();
        }

        // atrocious, but I'm stubborn, skill struct shall not enter the heap!

        [Pure]
        private static void GenerateStructs(ISkill skill, int targetCount, CharacterStateMachine selectedCharacter, 
                                            CharacterStateMachine targetOne, CharacterStateMachine targetTwo, CharacterStateMachine targetThree, CharacterStateMachine targetFour,
                                            out SkillStruct skillOne, out SkillStruct skillTwo, out SkillStruct skillThree, out SkillStruct skillFour, 
                                            out ChargeStruct chargeOne, out ChargeStruct chargeTwo, out ChargeStruct chargeThree, out ChargeStruct chargeFour)
        {
            ISkillModule skillModule = selectedCharacter.SkillModule;
            switch (targetCount)
            {
                case 1:
                {
                    skillOne = SkillStruct.CreateInstance(skill, selectedCharacter, targetOne);
                    skillOne.ApplyCustomStats();
                    skillModule.ModifySkill(ref skillOne);
                    skillTwo = default;
                    skillThree = default;
                    skillFour = default;
                    chargeOne = new ChargeStruct(skill, selectedCharacter, targetOne);
                    skillModule.ModifyCharge(ref chargeOne);
                    chargeTwo = default;
                    chargeThree = default;
                    chargeFour = default;
                    break;
                }
                case 2:
                {
                    skillOne = SkillStruct.CreateInstance(skill, selectedCharacter, targetOne);
                    skillOne.ApplyCustomStats();
                    skillModule.ModifySkill(ref skillOne);
                    skillTwo = SkillStruct.CreateInstance(skill, selectedCharacter, targetTwo);
                    skillTwo.ApplyCustomStats();
                    skillModule.ModifySkill(ref skillTwo);
                    skillThree = default;
                    skillFour = default;
                    chargeOne = new ChargeStruct(skill, selectedCharacter, targetOne);
                    skillModule.ModifyCharge(ref chargeOne);
                    chargeTwo = new ChargeStruct(skill, selectedCharacter, targetTwo);
                    skillModule.ModifyCharge(ref chargeTwo);
                    chargeThree = default;
                    chargeFour = default;
                    break;
                }
                case 3:
                {
                    skillOne = SkillStruct.CreateInstance(skill, selectedCharacter, targetOne);
                    skillOne.ApplyCustomStats();
                    skillModule.ModifySkill(ref skillOne);
                    skillTwo = SkillStruct.CreateInstance(skill, selectedCharacter, targetTwo);
                    skillTwo.ApplyCustomStats();
                    skillModule.ModifySkill(ref skillTwo);
                    skillThree = SkillStruct.CreateInstance(skill, selectedCharacter, targetThree);
                    skillThree.ApplyCustomStats();
                    skillModule.ModifySkill(ref skillThree);
                    skillFour = default;
                    chargeOne = new ChargeStruct(skill, selectedCharacter, targetOne);
                    skillModule.ModifyCharge(ref chargeOne);
                    chargeTwo = new ChargeStruct(skill, selectedCharacter, targetTwo);
                    skillModule.ModifyCharge(ref chargeTwo);
                    chargeThree = new ChargeStruct(skill, selectedCharacter, targetThree);
                    skillModule.ModifyCharge(ref chargeThree);
                    chargeFour = default;
                    break;
                }
                case 4:
                {
                    skillOne = SkillStruct.CreateInstance(skill, selectedCharacter, targetOne);
                    skillOne.ApplyCustomStats();
                    skillModule.ModifySkill(ref skillOne);
                    skillTwo = SkillStruct.CreateInstance(skill, selectedCharacter, targetTwo);
                    skillTwo.ApplyCustomStats();
                    skillModule.ModifySkill(ref skillTwo);
                    skillThree = SkillStruct.CreateInstance(skill, selectedCharacter, targetThree);
                    skillThree.ApplyCustomStats();
                    skillModule.ModifySkill(ref skillThree);
                    skillFour = SkillStruct.CreateInstance(skill, selectedCharacter, targetFour);
                    skillFour.ApplyCustomStats();
                    skillModule.ModifySkill(ref skillFour);
                    chargeOne = new ChargeStruct(skill, selectedCharacter, targetOne);
                    skillModule.ModifyCharge(ref chargeOne);
                    chargeTwo = new ChargeStruct(skill, selectedCharacter, targetTwo);
                    skillModule.ModifyCharge(ref chargeTwo);
                    chargeThree = new ChargeStruct(skill, selectedCharacter, targetThree);
                    skillModule.ModifyCharge(ref chargeThree);
                    chargeFour = new ChargeStruct(skill, selectedCharacter, targetFour);
                    skillModule.ModifyCharge(ref chargeFour);
                    break;
                }
                default:
                {
                    throw new ArgumentOutOfRangeException(nameof(targetCount), targetCount, "Target count must be between 1 and 4");
                }
            }
        }

        private void UpdatePosition()
        {
            Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
            Vector2 desiredScreenPosition = mouseScreenPosition + new Vector2(-5, 5);
            Vector3 desiredWorldPosition = uiCamera.ScreenToWorldPoint(desiredScreenPosition);
            desiredWorldPosition.z = 0;
            selfRect.position = desiredWorldPosition;
            
            selfRect.GetWorldCorners(ReusableCornersArray);
            Vector2 min = ReusableCornersArray[0];
            Vector2 max = ReusableCornersArray[2];
            Vector2 screenSize = new(Screen.width, Screen.height);
            Vector2 minScreen = uiCamera.WorldToScreenPoint(min);
            Vector2 maxScreen = uiCamera.WorldToScreenPoint(max);
            Vector2 offset = Vector2.zero;
            
            if (minScreen.x < 0)
                offset.x = -minScreen.x;
            else if (maxScreen.x > screenSize.x)
                offset.x = screenSize.x - maxScreen.x;
            
            if (minScreen.y < 0)
                offset.y = -minScreen.y;
            else if (maxScreen.y > screenSize.y)
                offset.y = screenSize.y - maxScreen.y;
            
            Vector3 offsetWorld = uiCamera.ScreenToWorldPoint(offset) - uiCamera.ScreenToWorldPoint(Vector3.zero);
            offsetWorld.z = 0;
            
            selfRect.position += offsetWorld;
        }

        private void Update()
        {
            UpdatePosition();
        }

        private void Reset()
        {
            selfRect = (RectTransform) transform;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private TooltipSidePanel CreateSidePanel()
        {
            TooltipSidePanel sidePanel = Instantiate(sidePanelPrefab, sidePanelParent);
            _sidePanels.Add(sidePanel);
            sidePanel.Hide();
            return sidePanel;
        }

        private TooltipSidePanel GetSidePanelAtIndex(int index)
        {
            for (int i = _sidePanels.Count; i < index; i++)
                CreateSidePanel();
            
            return _sidePanels[index];
        }

        public void Show(ISkill skill)
        {
        #region Sanity Checks
            Utils.Patterns.Option<CombatManager> combatManagerOption = CombatManager.Instance;
            if (combatManagerOption.IsNone)
            {
                RawTooltip(skill);
                return;
            }

            CombatManager combatManager = combatManagerOption.Value;
            CharacterStateMachine caster = combatManager.InputHandler.SelectedCharacter.Value;
            if (caster == null)
            {
                RawTooltip(skill);
                return;
            }
            
            int targetCount = 0;
            using Lease<CharacterStateMachine> targets = ArrayPool<CharacterStateMachine>.Shared.Lease(4);

            bool isCasterLeft = caster.PositionHandler.IsLeftSide;
            bool isTargetLeft = skill.AllowAllies ? isCasterLeft : !isCasterLeft;

            foreach (CharacterStateMachine target in combatManager.Characters.GetOnSide(isTargetLeft))
            {
                if (skill.FullCastingAndTargetingOk(caster, target) == false)
                    continue;
                
                targets.Rented[targetCount] = target;
                targetCount++;
            }

            targets.Length = targetCount;
            
            if (targetCount == 0)
            {
                RawTooltip(skill);
                return;
            }
        #endregion
            
        #region Setup
            for (int i = 0; i < StringBuilders.Length; i++) 
                StringBuilders[i].Clear();

            skillNameText.text = skill.DisplayName;
            
            GenerateStructs(skill,                      targetCount,                caster,
                            targets.Rented[0],          targets.Rented[1],          targets.Rented[2],            targets.Rented[3],
                            out SkillStruct skillOne,   out SkillStruct skillTwo,   out SkillStruct skillThree,   out SkillStruct skillFour,
                            out ChargeStruct chargeOne, out ChargeStruct chargeTwo, out ChargeStruct chargeThree, out ChargeStruct chargeFour);
            
        #endregion

        #region Charge
            bool chargeChanged = false;
            for (int i = 0; i < targetCount; i++)
            {
                ref ChargeStruct desiredStruct = ref GetChargeStruct(i, ref chargeOne, ref chargeTwo, ref chargeThree, ref chargeFour);
                if (Math.Abs(desiredStruct.Charge - skill.BaseCharge) > 0.0001f)
                {
                    chargeChanged = true;
                    break;
                }
            }

            if (chargeChanged)
            {
                for (int i = 0; i < targetCount; i++)
                {
                    StringBuilder stringBuilder = StringBuilders[i];
                    ref ChargeStruct desiredStruct = ref GetChargeStruct(i, ref chargeOne, ref chargeTwo, ref chargeThree, ref chargeFour);
                    
                    SingleUseBuilder.Override("Charge: ", desiredStruct.Charge.ToString("0.00"), "s");
                    if (desiredStruct.Charge < skill.BaseCharge) // less charge = better
                        SingleUseBuilder.Surround(ColorReferences.BuffedRichText.start, ColorReferences.BuffedRichText.end);
                    else if (desiredStruct.Charge > skill.BaseCharge)
                        SingleUseBuilder.Surround(ColorReferences.DebuffedRichText.start, ColorReferences.DebuffedRichText.end);

                    stringBuilder.AppendLine(SingleUseBuilder.ToString());
                }
            }
            else
            {
                SetChargeText(skill.BaseCharge);
            }
        #endregion

        #region Recovery
            bool recoveryComparison = false;
            for (int i = 0; i < targetCount; i++)
            {
                ref SkillStruct desiredStruct = ref GetSkillStruct(i, ref skillOne, ref skillTwo, ref skillThree, ref skillFour);
                if (Math.Abs(desiredStruct.Recovery - skill.BaseRecovery) > 0.0001f)
                {
                    recoveryComparison = true;
                    break;
                }
            }
            
            if (recoveryComparison)
            {
                for (int i = 0; i < targetCount; i++)
                {
                    StringBuilder stringBuilder = StringBuilders[i];
                    ref SkillStruct desiredStruct = ref GetSkillStruct(i, ref skillOne, ref skillTwo, ref skillThree, ref skillFour);
                    SingleUseBuilder.Override("Recovery: ", desiredStruct.Recovery.ToString("0.00"), "s");
                    if (desiredStruct.Recovery < skill.BaseRecovery) // less recovery = better
                        SingleUseBuilder.Surround(ColorReferences.BuffedRichText.start, ColorReferences.BuffedRichText.end);
                    else if (desiredStruct.Recovery > skill.BaseRecovery)
                        SingleUseBuilder.Surround(ColorReferences.DebuffedRichText.start, ColorReferences.DebuffedRichText.end);
                    
                    stringBuilder.AppendLine(SingleUseBuilder.ToString());
                }
            }
            else
            {
                SetRecoveryText(skill.BaseRecovery);
            }
        #endregion
            
        #region Power
            if (skill.BaseDamageMultiplier.IsNone)
            {
                damageMultiplierObject.SetActive(false);
                goto AfterPower;
            }
            
            float baseDamageMultiplier = skill.BaseDamageMultiplier.Value;
            bool powerChanged = false;
            for (int i = 0; i < targetCount; i++)
            {
                ref SkillStruct desiredStruct = ref GetSkillStruct(i, ref skillOne, ref skillTwo, ref skillThree, ref skillFour);
                ref ValueListPool<TargetProperties> targetsProperties = ref desiredStruct.TargetProperties;
                for (int j = 0; j < targetsProperties.Count; j++)
                {
                    ref TargetProperties property = ref targetsProperties[j];
                    if (property.DamageModifier.TrySome(out float damageModifier) == false || Math.Abs(damageModifier - baseDamageMultiplier) > 0.0001f)
                    {
                        powerChanged = true;
                        break;
                    }
                }
            }
            
            if (powerChanged)
            {
                for (int i = 0; i < targetCount; i++)
                {
                    StringBuilder stringBuilder = StringBuilders[i];
                    ref SkillStruct desiredStruct = ref GetSkillStruct(i, ref skillOne, ref skillTwo, ref skillThree, ref skillFour);
                    ref ValueListPool<TargetProperties> targetsProperties = ref desiredStruct.TargetProperties;
                    float skillBaseDamageModifier = skill.BaseDamageMultiplier.TrySome(out skillBaseDamageModifier) ? skillBaseDamageModifier : 0f;
                    if (targetsProperties.Count == 1)
                    {
                        float damageModifier = targetsProperties[0].DamageModifier.TrySome(out damageModifier) ? damageModifier : 0f;
                        SingleUseBuilder.Override("Damage Multiplier: ", damageModifier.ToPercentageString());
                        if (damageModifier > skillBaseDamageModifier)
                            SingleUseBuilder.Surround(ColorReferences.BuffedRichText.start, ColorReferences.BuffedRichText.end);
                        else if (damageModifier < skillBaseDamageModifier)
                            SingleUseBuilder.Surround(ColorReferences.DebuffedRichText.start, ColorReferences.DebuffedRichText.end);
                        
                        stringBuilder.AppendLine(SingleUseBuilder.ToString());
                    }
                    else
                    {
                        stringBuilder.AppendLine("Damage Multipliers:<indent=15%>");
                        for (int j = 0; j < targetsProperties.Count; j++)
                        {
                            float damageModifier = targetsProperties[j].DamageModifier.TrySome(out damageModifier) ? damageModifier : 0f;
                            SingleUseBuilder.Override(damageModifier.ToPercentageString(), " > ", targets.Rented[i].Script.CharacterName);
                            if (damageModifier > skillBaseDamageModifier)
                                SingleUseBuilder.Surround(ColorReferences.BuffedRichText.start, ColorReferences.BuffedRichText.end);
                            else if (damageModifier < skillBaseDamageModifier)
                                SingleUseBuilder.Surround(ColorReferences.DebuffedRichText.start, ColorReferences.DebuffedRichText.end);
                            
                            stringBuilder.AppendLine(SingleUseBuilder.ToString());
                        }
                        
                        stringBuilder.Append("</indent>");
                    }
                }
            }
            else
            {
                SetDamageMultiplierText(baseDamageMultiplier);
            }
        AfterPower:
        #endregion
            
        #region Accuracy
            if (skill.BaseAccuracy.IsNone)
            {
                accuracyObject.SetActive(false);
                goto AfterAccuracy;
            }
            
            bool accuracyChanged = false;
            float baseAccuracy = skill.BaseAccuracy.Value;
            for (int i = 0; i < targetCount; i++)
            {
                ref SkillStruct desiredStruct = ref GetSkillStruct(i, ref skillOne, ref skillTwo, ref skillThree, ref skillFour);
                ref ValueListPool<TargetProperties> targetsProperties = ref desiredStruct.TargetProperties;
                
                for (int j = 0; j < targetsProperties.Count; j++)
                {
                    ref TargetProperties property = ref targetsProperties[j];
                    if (property.AccuracyModifier.TrySome(out float accuracyModifier) == false || Math.Abs(accuracyModifier - baseAccuracy) > 0.0001f)
                    {
                        accuracyChanged = true;
                        break;
                    }
                }
            }
            
            if (accuracyChanged)
            {
                for (int i = 0; i < targetCount; i++)
                {
                    StringBuilder stringBuilder = StringBuilders[i];
                    ref SkillStruct desiredStruct = ref GetSkillStruct(i, ref skillOne, ref skillTwo, ref skillThree, ref skillFour);
                    ref ValueListPool<TargetProperties> targetsProperties = ref desiredStruct.TargetProperties;
                    float skillBaseAccuracy = skill.BaseAccuracy.TrySome(out skillBaseAccuracy) ? skillBaseAccuracy : 0f;
                    if (targetsProperties.Count == 1)
                    {
                        float accuracyModifier = targetsProperties[0].AccuracyModifier.TrySome(out accuracyModifier) ? accuracyModifier : 0f;
                        SingleUseBuilder.Override("Accuracy: ", accuracyModifier.ToPercentageString());
                        if (accuracyModifier > skillBaseAccuracy)
                            SingleUseBuilder.Surround(ColorReferences.BuffedRichText.start, ColorReferences.BuffedRichText.end);
                        else if (accuracyModifier < skillBaseAccuracy)
                            SingleUseBuilder.Surround(ColorReferences.DebuffedRichText.start, ColorReferences.DebuffedRichText.end);
                        
                        stringBuilder.AppendLine(SingleUseBuilder.ToString());
                    }
                    else
                    {
                        stringBuilder.AppendLine("Accuracies:<indent=15%>");
                        for (int j = 0; j < targetsProperties.Count; j++)
                        {
                            float accuracyModifier = targetsProperties[j].AccuracyModifier.TrySome(out accuracyModifier) ? accuracyModifier : 0f;
                            SingleUseBuilder.Override(accuracyModifier.ToPercentageString(), " > ", targets.Rented[i].Script.CharacterName);
                            if (accuracyModifier > skillBaseAccuracy)
                                SingleUseBuilder.Surround(ColorReferences.BuffedRichText.start, ColorReferences.BuffedRichText.end);
                            else if (accuracyModifier < skillBaseAccuracy)
                                SingleUseBuilder.Surround(ColorReferences.DebuffedRichText.start, ColorReferences.DebuffedRichText.end);
                            
                            stringBuilder.AppendLine(SingleUseBuilder.ToString());
                        }
                        
                        stringBuilder.Append("</indent>");
                    }
                }
            }
            else
            {
                SetAccuracyText(baseAccuracy);
            }
            
        AfterAccuracy:
        #endregion
            
        #region Critical Chance
            if (skill.BaseCriticalChance.IsNone)
            {
                criticalChanceObject.SetActive(false);
                goto AfterCriticalChance;
            }
            
            bool critChanceChanged = false;
            float baseCritChance = skill.BaseCriticalChance.Value;
            for (int i = 0; i < targetCount; i++)
            {
                ref SkillStruct desiredStruct = ref GetSkillStruct(i, ref skillOne, ref skillTwo, ref skillThree, ref skillFour);
                ref ValueListPool<TargetProperties> targetsProperties = ref desiredStruct.TargetProperties;
                
                for (int j = 0; j < targetsProperties.Count; j++)
                {
                    ref TargetProperties property = ref targetsProperties[j];
                    if (property.CriticalChanceModifier.TrySome(out float criticalChance) == false || Math.Abs(criticalChance - baseCritChance) > 0.0001f)
                    {
                        critChanceChanged = true;
                        break;
                    }
                }
            }
            
            if (critChanceChanged)
            {
                for (int i = 0; i < targetCount; i++)
                {
                    StringBuilder stringBuilder = StringBuilders[i];
                    ref SkillStruct desiredStruct = ref GetSkillStruct(i, ref skillOne, ref skillTwo, ref skillThree, ref skillFour);
                    ref ValueListPool<TargetProperties> targetsProperties = ref desiredStruct.TargetProperties;
                    float skillBaseCritChance = skill.BaseCriticalChance.TrySome(out skillBaseCritChance) ? skillBaseCritChance : 0f;
                    if (targetsProperties.Count == 1)
                    {
                        float criticalChance = targetsProperties[0].CriticalChanceModifier.TrySome(out criticalChance) ? criticalChance : 0f;
                        SingleUseBuilder.Override("Critical Chance: ", criticalChance.ToPercentageString());
                        if (criticalChance > skillBaseCritChance)
                            SingleUseBuilder.Surround(ColorReferences.BuffedRichText.start, ColorReferences.BuffedRichText.end);
                        else if (criticalChance < skillBaseCritChance)
                            SingleUseBuilder.Surround(ColorReferences.DebuffedRichText.start, ColorReferences.DebuffedRichText.end);
                        
                        stringBuilder.AppendLine(SingleUseBuilder.ToString());
                    }
                    else
                    {
                        stringBuilder.AppendLine("Critical Chances:<indent=15%>");
                        for (int j = 0; j < targetsProperties.Count; j++)
                        {
                            float criticalChance = targetsProperties[j].CriticalChanceModifier.TrySome(out criticalChance) ? criticalChance : 0f;
                            SingleUseBuilder.Override(criticalChance.ToPercentageString(), " > ", targets.Rented[i].Script.CharacterName);
                            if (criticalChance > skillBaseCritChance)
                                SingleUseBuilder.Surround(ColorReferences.BuffedRichText.start, ColorReferences.BuffedRichText.end);
                            else if (criticalChance < skillBaseCritChance)
                                SingleUseBuilder.Surround(ColorReferences.DebuffedRichText.start, ColorReferences.DebuffedRichText.end);

                            stringBuilder.AppendLine(SingleUseBuilder.ToString());
                        }
                        
                        stringBuilder.Append("</indent>");
                    }
                }
            }
            else
            {
                SetCriticalChanceText(baseCritChance);
            }
        AfterCriticalChance:
        #endregion
            
            EffectsStringBuilder.Clear();
            
        #region ResiliencePiercing
            if (skill.BaseResiliencePiercing.IsNone)
            {
                goto AfterResiliencePiercing;
            }
            
            bool resiliencePiercingChanged = false;
            float baseResiliencePiercing = skill.BaseResiliencePiercing.Value;
            for (int i = 0; i < targetCount; i++)
            {
                ref SkillStruct desiredStruct = ref GetSkillStruct(i, ref skillOne, ref skillTwo, ref skillThree, ref skillFour);
                ref ValueListPool<TargetProperties> targetsProperties = ref desiredStruct.TargetProperties;
                
                for (int j = 0; j < targetsProperties.Count; j++)
                {
                    ref TargetProperties property = ref targetsProperties[j];
                    if (property.ResiliencePiercingModifier.TrySome(out float resiliencePiercing) == false || Math.Abs(resiliencePiercing - baseResiliencePiercing) > 0.0001f)
                    {
                        resiliencePiercingChanged = true;
                        break;
                    }
                }
            }
            
            if (resiliencePiercingChanged)
            {
                for (int i = 0; i < targetCount; i++)
                {
                    StringBuilder stringBuilder = StringBuilders[i];
                    ref SkillStruct desiredStruct = ref GetSkillStruct(i, ref skillOne, ref skillTwo, ref skillThree, ref skillFour);
                    ref ValueListPool<TargetProperties> targetsProperties = ref desiredStruct.TargetProperties;
                    float skillBaseResiliencePiercing = skill.BaseResiliencePiercing.TrySome(out skillBaseResiliencePiercing) ? skillBaseResiliencePiercing : 0f;
                    if (targetsProperties.Count == 1)
                    {
                        float resiliencePiercing = targetsProperties[0].ResiliencePiercingModifier.TrySome(out resiliencePiercing) ? resiliencePiercing : 0f;
                        SingleUseBuilder.Override("Resilience Ignored (flat): ", resiliencePiercing.ToPercentageString());
                        if (resiliencePiercing > skillBaseResiliencePiercing)
                            SingleUseBuilder.Surround(ColorReferences.BuffedRichText.start, ColorReferences.BuffedRichText.end);
                        else if (resiliencePiercing < skillBaseResiliencePiercing)
                            SingleUseBuilder.Surround(ColorReferences.DebuffedRichText.start, ColorReferences.DebuffedRichText.end);
                        
                        stringBuilder.AppendLine(SingleUseBuilder.ToString());
                    }
                    else
                    {
                        stringBuilder.AppendLine("Resilience Ignored (flat):<indent=15%>");
                        for (int j = 0; j < targetsProperties.Count; j++)
                        {
                            float resiliencePiercing = targetsProperties[j].ResiliencePiercingModifier.TrySome(out resiliencePiercing) ? resiliencePiercing : 0f;
                            SingleUseBuilder.Override(resiliencePiercing.ToPercentageString(), " > ", targets.Rented[i].Script.CharacterName);
                            if (resiliencePiercing > skillBaseResiliencePiercing)
                                SingleUseBuilder.Surround(ColorReferences.BuffedRichText.start, ColorReferences.BuffedRichText.end);
                            else if (resiliencePiercing < skillBaseResiliencePiercing)
                                SingleUseBuilder.Surround(ColorReferences.DebuffedRichText.start, ColorReferences.DebuffedRichText.end);
                            
                            stringBuilder.AppendLine(SingleUseBuilder.ToString());
                        }
                        
                        stringBuilder.Append("</indent>");
                    }
                }
            }
            else if (Mathf.Abs(baseResiliencePiercing) > 0.0001f)
            {
                AppendResiliencePiercingText(baseResiliencePiercing);
            }
        AfterResiliencePiercing:
        #endregion

        #region Effects
            
            ReusableStatusList.Clear();
            ref ValueListPool<IActualStatusScript> targetEffectsOne = ref skillOne.TargetEffects;
            bool targetEffectsChanged = TargetEffectsChanged(skill, ref targetEffectsOne, targetCount, caster, ref skillOne, ref skillTwo, ref skillThree, ref skillFour);

            if (targetEffectsChanged)
            {
                DoDividedEffectsText(skill, ref targetEffectsOne, targetCount);
            }
            else
            {
                DoUnifiedEffectsText(skill, targets.Rented[0], caster);
            }

            Utils.Patterns.Option<uint> currentSkillGetMaxUseCount = skill.GetMaxUseCount;
            if (currentSkillGetMaxUseCount.IsSome)
            {
                EffectsStringBuilder.Append("\n");
                EffectsStringBuilder.Append("<color=red> Max uses per combat: ", currentSkillGetMaxUseCount.Value.ToString("0"), "</color>");
            }
            
            SetEffectsText(EffectsStringBuilder.ToString());
        #endregion
            
            SetFlavorText(skill.FlavorText);
            for (int i = 0; i < castingPositionCircles.Length; i++) 
                castingPositionCircles[i].sprite = skill.CastingPositions[i] ? friendlyCircle : emptyCircle;

            Sprite dotSprite, connectorSprite;

            if (skill.AllowAllies)
            {
                dotSprite = friendlyCircle;
                connectorSprite = friendlyConnector;
            }
            else
            {
                dotSprite = enemyCircle;
                connectorSprite = enemyConnector;
            }
            
            for (int i = 0; i < 4; i++)
                targetingPositionCircles[i].sprite = skill.TargetPositions[i] ? dotSprite : emptyCircle;
            
            for (int i = 0; i < 3; i++)
            {
                Image connector = targetingConnectors[i];
                bool active = skill.MultiTarget && skill.TargetPositions[i] && skill.TargetPositions[i + 1];
                connector.gameObject.SetActive(active);
                connector.sprite = connectorSprite;
            }

            for (int i = 0; i < targetCount; i++)
            {
                StringBuilder stringBuilder = StringBuilders[i];
                TooltipSidePanel sidePanel = GetSidePanelAtIndex(i);
                if (stringBuilder.Length == 0)
                {
                    sidePanel.Hide();
                    continue;
                }

                ref SkillStruct desiredStruct = ref GetSkillStruct(i, ref skillOne, ref skillTwo, ref skillThree, ref skillFour);
                sidePanel.Show($"➡{desiredStruct.FirstTarget.Script.CharacterName}", stringBuilder.ToString());
            }
            
            for (int i = targetCount; i < _sidePanels.Count; i++)
                GetSidePanelAtIndex(i).Hide();

            gameObject.SetActive(true);
            UpdatePosition();

            for (int i = 0; i < targetCount; i++)
            {
                ref SkillStruct desiredStruct = ref GetSkillStruct(i, ref skillOne, ref skillTwo, ref skillThree, ref skillFour);
                desiredStruct.Dispose();
            }
            
            for (int i = 0; i < targetCount && i < ReusableStatusRecordArray.Length; i++)
            {
                StringBuilders[i].Clear();
                ReusableStatusRecordArray[i].effects.Clear();
                ReusableStatusRecordArray[i].target = null;
            }
            
            ReusableStatusList.Clear();

            skillOne.Dispose();
            skillTwo.Dispose();
            skillThree.Dispose();
            skillFour.Dispose();
            chargeOne.Dispose();
            chargeTwo.Dispose();
            chargeThree.Dispose();
            chargeFour.Dispose();
        }

        private static void DoUnifiedEffectsText(ISkill skill, CharacterStateMachine target, CharacterStateMachine caster)
        {
            using Lease<StatusToApply> casterEffects = ArrayPool<StatusToApply>.Shared.Lease(skill.CasterEffects.Count);
            using (Lease<StatusToApply> targetEffects = ArrayPool<StatusToApply>.Shared.Lease(skill.TargetEffects.Count))
            {
                for (int i = 0; i < skill.CasterEffects.Count; i++)
                {
                    StatusToApply record = skill.CasterEffects[i].GetActual.GetStatusToApply(caster, caster, crit: false, skill: skill);
                    record.ProcessModifiers();
                    casterEffects.Rented[i] = record;
                }

                for (int i = 0; i < skill.TargetEffects.Count; i++)
                {
                    StatusToApply record = skill.TargetEffects[i].GetActual.GetStatusToApply(caster, target, crit: false, skill: skill);
                    record.ProcessModifiers();
                    targetEffects.Rented[i] = record;
                }

                EffectsStringBuilder.Append(skill.GetCustomStatsAndEffectsText(casterEffects, targetEffects));
            }
        }

        private static void DoDividedEffectsText(ISkill skill, ref ValueListPool<IActualStatusScript> scriptsOne, int targetCount)
        {
            foreach (ICustomSkillStat customStat in skill.CustomStats)
            {
                Utils.Patterns.Option<string> option = customStat.GetDescription();
                if (option.TrySome(out string description))
                    EffectsStringBuilder.AppendLine(description);
            }

            (CharacterStateMachine target, List<StatusToApply> effects) recordsOne = ReusableStatusRecordArray[0];
            int effectCount = Mathf.Min(scriptsOne.Count, recordsOne.effects.Count);
            for (int i = 0; i < effectCount; i++)
            {
                bool presentInAll = true;
                StatusToApply toApply = recordsOne.effects[i];
                for (int j = 1; j < targetCount && presentInAll; j++)
                {
                    presentInAll = false;
                    (CharacterStateMachine target, List<StatusToApply> effects) recordsJ = ReusableStatusRecordArray[j];
                    for (int k = 0; k < scriptsOne.Count && k < recordsJ.effects.Count; k++)
                    {
                        if (StatusUtils.DoesRecordsHaveSameStats(recordsJ.effects[k], toApply))
                        {
                            presentInAll = true;
                            break;
                        }
                    }
                }
                
                if (presentInAll == false)
                    continue;

                ReusableStatusList.Add(toApply);
                for (int j = 0; j < targetCount; j++)
                    recordsOne.effects.Remove(toApply);
            }

            for (int i = 0; i < targetCount; i++)
            {
                StringBuilder stringBuilder = StringBuilders[i];
                foreach (StatusToApply record in ReusableStatusRecordArray[i].effects)
                    stringBuilder.AppendLine(record.GetDescription());
            }

            for (int i = 0; i < ReusableStatusList.Count; i++)
            {
                StatusToApply statusRecord = ReusableStatusList[i];
                EffectsStringBuilder.AppendLine(statusRecord.GetDescription());
            }
        }

        private bool TargetEffectsChanged(ISkill skill, ref ValueListPool<IActualStatusScript> targetEffectsOne, int targetCount, CharacterStateMachine caster,
                                          ref SkillStruct skillOne, ref SkillStruct skillTwo, ref SkillStruct skillThree, ref SkillStruct skillFour)
        {
            if (skill.TargetEffects.Count != targetEffectsOne.Count)
                return true;

            for (int index = 1; index < targetCount; index++)
            {
                ref SkillStruct desiredStruct = ref GetSkillStruct(index, ref skillOne, ref skillTwo, ref skillThree, ref skillFour);
                ref ValueListPool<IActualStatusScript> targetEffects = ref desiredStruct.TargetEffects;
                if (targetEffects.Count != targetEffectsOne.Count)
                    return true;
            }

            for (int i = 0; i < ReusableStatusRecordArray.Length; i++)
                ReusableStatusRecordArray[i].effects.Clear();

            for (int i = 0; i < targetCount; i++)
            {
                ref SkillStruct desiredStruct = ref GetSkillStruct(i, ref skillOne, ref skillTwo, ref skillThree, ref skillFour);
                ReusableStatusRecordArray[i].target = desiredStruct.FirstTarget;
                ref ValueListPool<IActualStatusScript> targetEffects = ref desiredStruct.TargetEffects;
                for (int j = 0; j < targetEffects.Count; j++)
                {
                    IActualStatusScript statusScript = targetEffects[j];
                    StatusToApply statusRecord = statusScript.GetStatusToApply(caster, desiredStruct.FirstTarget, false, desiredStruct.Skill);
                    statusRecord.ProcessModifiers();
                    ReusableStatusRecordArray[i].effects.Add(statusRecord);
                }
            }

            for (int i = 1; i < targetCount; i++)
                if (ReusableStatusRecordArray[0].effects.Count != ReusableStatusRecordArray[i].effects.Count)
                    return true;

            for (int i = 0; i < targetEffectsOne.Count; i++)
                for (int j = 1; j < targetCount; j++)
                    if (StatusUtils.DoesRecordsHaveSameStats(ReusableStatusRecordArray[j].effects[i], ReusableStatusRecordArray[0].effects[i]) == false)
                        return true;

            return false;
        }

        [Pure]
        private ref ChargeStruct GetChargeStruct(int index, ref ChargeStruct one, ref ChargeStruct two, ref ChargeStruct three, ref ChargeStruct four)
        {
            switch (index)
            {
                case 0:  return ref one;
                case 1:  return ref two;
                case 2:  return ref three;
                case 3:  return ref four;
                default: throw new ArgumentOutOfRangeException(nameof(index), index, $"Index must be between 0 and 3, inclusive, but was {index}.");
            }
        }

        [Pure]
        private ref SkillStruct GetSkillStruct(int index, ref SkillStruct one, ref SkillStruct two, ref SkillStruct three, ref SkillStruct four)
        {
            switch (index)
            {
                case 0:  return ref one;
                case 1:  return ref two;
                case 2:  return ref three;
                case 3:  return ref four;
                default: throw new ArgumentOutOfRangeException(nameof(index), index, $"Index must be between 0 and 3, inclusive, but was {index}.");
            }
        }

        public void RawTooltip(ISkill skill)
        {
            skillNameText.text = skill.DisplayName;
            SetChargeText(skill.BaseCharge);
            SetRecoveryText(skill.BaseRecovery);
            
            if (skill.BaseAccuracy.TrySome(out float accuracy))
                SetAccuracyText(accuracy);
            else
                accuracyObject.SetActive(false);
            
            if (skill.BaseDamageMultiplier.TrySome(out float damageMultiplier))
                SetDamageMultiplierText(damageMultiplier);
            else
                damageMultiplierObject.SetActive(false);
            
            if (skill.BaseCriticalChance.TrySome(out float criticalChance))
                SetCriticalChanceText(criticalChance);
            else
                criticalChanceObject.SetActive(false);

            EffectsStringBuilder.Clear();

            IReadOnlyList<IBaseStatusScript> skillCasterEffects = skill.CasterEffects;
            IReadOnlyList<IBaseStatusScript> skillTargetEffects = skill.TargetEffects;
            using (Lease<IActualStatusScript> casterStatusScripts = ArrayPool<IActualStatusScript>.Shared.Lease(skillCasterEffects.Count))
            using (Lease<IActualStatusScript> targetStatusScripts = ArrayPool<IActualStatusScript>.Shared.Lease(skillTargetEffects.Count))
            {
                for (int i = 0; i < skillCasterEffects.Count; i++)
                {
                    IActualStatusScript casterStatusScript = skillCasterEffects[i].GetActual;
                    casterStatusScripts.Rented[i] = casterStatusScript;
                }
                
                for (int i = 0; i < skillTargetEffects.Count; i++)
                {
                    IActualStatusScript targetStatusScript = skillTargetEffects[i].GetActual;
                    targetStatusScripts.Rented[i] = targetStatusScript;
                }
                
                EffectsStringBuilder.Append(skill.GetCustomStatsAndEffectsText(casterStatusScripts, targetStatusScripts));
            }

            Utils.Patterns.Option<uint> currentSkillGetMaxUseCount = skill.GetMaxUseCount;
            if (currentSkillGetMaxUseCount.IsSome)
            {
                EffectsStringBuilder.Append("\n");
                EffectsStringBuilder.Append("<color=red> Max uses per combat: ", currentSkillGetMaxUseCount.Value.ToString("0"), "</color>");
            }

            SetEffectsText(EffectsStringBuilder.ToString());
            SetFlavorText(skill.FlavorText);

            for (int i = 0; i < castingPositionCircles.Length; i++) 
                castingPositionCircles[i].sprite = skill.CastingPositions[i] ? friendlyCircle : emptyCircle;

            Sprite dotSprite, connectorSprite;

            if (skill.AllowAllies)
            {
                dotSprite = friendlyCircle;
                connectorSprite = friendlyConnector;
            }
            else
            {
                dotSprite = enemyCircle;
                connectorSprite = enemyConnector;
            }
            
            for (int i = 0; i < 4; i++)
                targetingPositionCircles[i].sprite = skill.TargetPositions[i] ? dotSprite : emptyCircle;

            for (int i = 0; i < 3; i++)
            {
                Image connector = targetingConnectors[i];
                bool active = skill.MultiTarget && skill.TargetPositions[i] && skill.TargetPositions[i + 1];
                connector.gameObject.SetActive(active);
                connector.sprite = connectorSprite;
            }

            UpdatePosition();

            gameObject.SetActive(true);
        }

        private void SetChargeText(float value) 
            => chargeText.text = SingleUseBuilder.Override(value.ToString("0.00"), "s").ToString();

        private void SetRecoveryText(float value) 
            => recoveryText.text = SingleUseBuilder.Override(value.ToString("0.00"), "s").ToString();

        private void SetAccuracyText(float value)
        {
            accuracyText.text = value.ToPercentageString();
            accuracyObject.SetActive(true);
        }

        private void SetDamageMultiplierText(float value)
        {
            dmgMultText.text = value.ToPercentageString();
            damageMultiplierObject.SetActive(true);
        }

        private void SetCriticalChanceText(float value)
        {
            criticalChanceText.text = value.ToPercentageString();
            criticalChanceObject.SetActive(true);
        }

        private void SetEffectsText(string value) => effectsText.text = value;
        private void SetFlavorText(string value) => flavorText.text = value;

        private void AppendResiliencePiercingText(float value)
        {
            SingleUseBuilder.Override("Ignores ", value.ToPercentageString(), " of resilience.(flat)");
            EffectsStringBuilder.Append(SingleUseBuilder.ToString());
        }
    }
}