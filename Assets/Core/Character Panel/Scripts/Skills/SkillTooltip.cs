using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Behaviour.Modules;
using Core.Combat.Scripts.Effects;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Combat.Scripts.UI;
using Core.Localization.Scripts;
using Core.Pause_Menu.Scripts;
using Core.Utils.Async;
using Core.Utils.Collections;
using Core.Utils.Extensions;
using Core.Utils.Math;
using Core.Utils.Patterns;
using JetBrains.Annotations;
using ListPool;
using NetFabric.Hyperlinq;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using CombatManager = Core.Combat.Scripts.Managers.CombatManager;
using IBaseStatusScript = Core.Combat.Scripts.Effects.BaseTypes.IBaseStatusScript;

namespace Core.Character_Panel.Scripts.Skills
{
    public class SkillTooltip : Singleton<SkillTooltip>
    {
        private static readonly StringBuilder SingleUseBuilder = new();
        
        private static readonly StringBuilder[] TargetStringBuilders = { new(), new(), new(), new() };
        
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

        private static readonly LocalizedText MaxUsesPerCombatTrans = new("skill_tooltip_maxusespercombat"),
                                              ResilienceReductionTrans = new("skill_tooltip_resiliencereduction"),
                                              CriticalChanceTrans = new("skill_tooltip_criticalchance"),
                                              AccuracyTrans = new("skill_tooltip_accuracy"),
                                              RecoveryTrans = new("skill_tooltip_recovery"),
                                              PowerTrans = new("skill_tooltip_power"),
                                              ChargeTrans = new("skill_tooltip_charge");
            

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
                CreateSidePanel();

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

        [System.Diagnostics.Contracts.Pure]
        private static void GenerateStructs([NotNull] ISkill skill, int targetCount, [NotNull] CharacterStateMachine selectedCharacter, 
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

        [NotNull]
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

        public void Show([NotNull] ISkill skill)
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
            bool isTargetLeft = skill.IsPositive ? isCasterLeft : !isCasterLeft;

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
            
            for (int i = 0; i < TargetStringBuilders.Length; i++) 
                TargetStringBuilders[i].Clear();

            skillNameText.text = skill.DisplayName.Translate().GetText();
            
            GenerateStructs(skill,                      targetCount,                caster,
                            targets.Rented[0],          targets.Rented[1],          targets.Rented[2],            targets.Rented[3],
                            out SkillStruct skillOne,   out SkillStruct skillTwo,   out SkillStruct skillThree,   out SkillStruct skillFour,
                            out ChargeStruct chargeOne, out ChargeStruct chargeTwo, out ChargeStruct chargeThree, out ChargeStruct chargeFour);
            
            DoCharge(skill, targetCount, ref chargeOne, ref chargeTwo, ref chargeThree, ref chargeFour);
            DoRecovery(skill, targetCount, ref skillOne, ref skillTwo, ref skillThree, ref skillFour);
            DoPower(skill, targetCount, ref skillOne, targets, ref skillTwo, ref skillThree, ref skillFour);
            DoAccuracy(skill, targetCount, ref skillOne, targets, ref skillTwo, ref skillThree, ref skillFour);
            DoCriticalChance(skill, targetCount, ref skillOne, targets, ref skillTwo, ref skillThree, ref skillFour);
            
            EffectsStringBuilder.Clear();
            DoResiliencePiercing(skill, targetCount, ref skillOne, targets, ref skillTwo, ref skillThree, ref skillFour);
            DoEffects(skill, ref skillOne, targetCount, caster, targets, ref skillTwo, ref skillThree, ref skillFour);
            
            SetFlavorText(skill.FlavorText.Translate().GetText());
            for (int i = 0; i < castingPositionCircles.Length; i++) 
                castingPositionCircles[i].sprite = skill.CastingPositions[i] ? friendlyCircle : emptyCircle;

            (Sprite dotSprite, Sprite connectorSprite) = skill.IsPositive ? (friendlyCircle, friendlyConnector) : (enemyCircle, enemyConnector);
            
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
                StringBuilder stringBuilder = TargetStringBuilders[i];
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
                TargetStringBuilders[i].Clear();
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

        private void DoEffects([NotNull] ISkill skill, ref SkillStruct skillOne, int targetCount, CharacterStateMachine caster, Lease<CharacterStateMachine> targets,
                               ref SkillStruct skillTwo, ref SkillStruct skillThree, ref SkillStruct skillFour)
        {
            ReusableStatusList.Clear();
            ref CustomValuePooledList<IActualStatusScript> targetEffectsOne = ref skillOne.TargetEffects;
            bool targetEffectsChanged = TargetEffectsChanged(skill, ref targetEffectsOne, targetCount, caster, ref skillOne, ref skillTwo, ref skillThree, ref skillFour);

            if (targetEffectsChanged)
                DoDividedEffectsText(skill, ref targetEffectsOne, targetCount);
            else
                DoUnifiedEffectsText(skill, targets.Rented[0], caster);

            Utils.Patterns.Option<int> currentSkillGetMaxUseCount = skill.GetMaxUseCount;

            if (currentSkillGetMaxUseCount.IsSome)
                EffectsStringBuilder.Append("\n<color=red> ", MaxUsesPerCombatTrans.Translate().GetText(), currentSkillGetMaxUseCount.Value.ToString("0"), "</color>");

            SetEffectsText(EffectsStringBuilder.ToString());
        }

        private void DoResiliencePiercing([NotNull] ISkill skill, int targetCount, ref SkillStruct skillOne, Lease<CharacterStateMachine> targets, ref SkillStruct skillTwo, ref SkillStruct skillThree, ref SkillStruct skillFour)
        {
            if (skill.ResilienceReduction.IsNone)
                return;

            bool resiliencePiercingChanged = false;
            int baseResiliencePiercing = skill.ResilienceReduction.Value;

            for (int i = 0; i < targetCount; i++)
            {
                ref SkillStruct desiredStruct = ref GetSkillStruct(i, ref skillOne, ref skillTwo, ref skillThree, ref skillFour);
                ref CustomValuePooledList<TargetProperties> targetsProperties = ref desiredStruct.TargetProperties;

                for (int j = 0; j < targetsProperties.Count; j++)
                {
                    ref TargetProperties property = ref targetsProperties[j];

                    if (property.ResilienceReductionModifier.TrySome(out int resiliencePiercing) == false || resiliencePiercing != baseResiliencePiercing)
                    {
                        resiliencePiercingChanged = true;
                        break;
                    }
                }
            }

            if (resiliencePiercingChanged == false && baseResiliencePiercing > 0)
            {
                AppendResiliencePiercingText(baseResiliencePiercing);
                return;
            }

            for (int i = 0; i < targetCount; i++)
            {
                StringBuilder stringBuilder = TargetStringBuilders[i];
                ref SkillStruct desiredStruct = ref GetSkillStruct(i, ref skillOne, ref skillTwo, ref skillThree, ref skillFour);
                ref CustomValuePooledList<TargetProperties> targetsProperties = ref desiredStruct.TargetProperties;

                skill.ResilienceReduction.TrySome(out int skillBaseResilienceReduction);

                string resilienceReductionTrans = ResilienceReductionTrans.Translate().GetText();

                if (targetsProperties.Count == 1)
                {
                    targetsProperties[0].ResilienceReductionModifier.TrySome(out int resiliencePiercing);

                    SingleUseBuilder.Override(resilienceReductionTrans, ' ', resiliencePiercing.ToString("0"));

                    if (resiliencePiercing > skillBaseResilienceReduction)
                        SingleUseBuilder.Surround(ColorReferences.BuffedRichText);
                    else if (resiliencePiercing < skillBaseResilienceReduction)
                        SingleUseBuilder.Surround(ColorReferences.DebuffedRichText);

                    stringBuilder.AppendLine(SingleUseBuilder.ToString());
                }
                else
                {
                    stringBuilder.AppendLine(resilienceReductionTrans, "<indent=15%>");

                    for (int j = 0; j < targetsProperties.Count; j++)
                    {
                        targetsProperties[j].ResilienceReductionModifier.TrySome(out int resilienceReduction);

                        SingleUseBuilder.Override(resilienceReduction.ToString("0"), " > ", targets.Rented[i].Script.CharacterName.Translate().GetText());

                        if (resilienceReduction > skillBaseResilienceReduction)
                            SingleUseBuilder.Surround(ColorReferences.BuffedRichText);
                        else if (resilienceReduction < skillBaseResilienceReduction)
                            SingleUseBuilder.Surround(ColorReferences.DebuffedRichText);

                        stringBuilder.AppendLine(SingleUseBuilder.ToString());
                    }

                    stringBuilder.Append("</indent>");
                }
            }
        }

        private void DoCriticalChance([NotNull] ISkill skill, int targetCount, ref SkillStruct skillOne, Lease<CharacterStateMachine> targets, ref SkillStruct skillTwo, ref SkillStruct skillThree, ref SkillStruct skillFour)
        {
            if (skill.CriticalChance.IsNone)
            {
                criticalChanceObject.SetActive(false);
                return;
            }

            bool critChanceChanged = false;
            int baseCritChance = skill.CriticalChance.Value;
            
            for (int i = 0; i < targetCount; i++)
            {
                ref SkillStruct desiredStruct = ref GetSkillStruct(i, ref skillOne, ref skillTwo, ref skillThree, ref skillFour);
                ref CustomValuePooledList<TargetProperties> targetsProperties = ref desiredStruct.TargetProperties;

                for (int j = 0; j < targetsProperties.Count; j++)
                {
                    ref TargetProperties property = ref targetsProperties[j];

                    if (property.CriticalChanceModifier.TrySome(out int criticalChance) == false || criticalChance != baseCritChance)
                    {
                        critChanceChanged = true;
                        break;
                    }
                }
            }

            if (critChanceChanged == false)
            {
                SetCriticalChanceText(baseCritChance);
                return;
            }

            for (int i = 0; i < targetCount; i++)
            {
                StringBuilder stringBuilder = TargetStringBuilders[i];
                ref SkillStruct desiredStruct = ref GetSkillStruct(i, ref skillOne, ref skillTwo, ref skillThree, ref skillFour);
                ref CustomValuePooledList<TargetProperties> targetsProperties = ref desiredStruct.TargetProperties;

                skill.CriticalChance.TrySome(out int skillBaseCritChance);

                string criticalChanceTrans = CriticalChanceTrans.Translate().GetText();

                if (targetsProperties.Count == 1)
                {
                    targetsProperties[0].CriticalChanceModifier.TrySome(out int criticalChance);
                    SingleUseBuilder.Override(criticalChanceTrans, ' ', criticalChance.ToString("0"));

                    if (criticalChance > skillBaseCritChance)
                        SingleUseBuilder.Surround(ColorReferences.BuffedRichText);
                    else if (criticalChance < skillBaseCritChance)
                        SingleUseBuilder.Surround(ColorReferences.DebuffedRichText);

                    stringBuilder.AppendLine(SingleUseBuilder.ToString());
                }
                else
                {
                    stringBuilder.AppendLine(criticalChanceTrans, "<indent=15%>");

                    for (int j = 0; j < targetsProperties.Count; j++)
                    {
                        targetsProperties[j].CriticalChanceModifier.TrySome(out int criticalChance);

                        SingleUseBuilder.Override(criticalChance.ToString("0"), " > ", targets.Rented[i].Script.CharacterName.Translate().GetText());

                        if (criticalChance > skillBaseCritChance)
                            SingleUseBuilder.Surround(ColorReferences.BuffedRichText);
                        else if (criticalChance < skillBaseCritChance)
                            SingleUseBuilder.Surround(ColorReferences.DebuffedRichText);

                        stringBuilder.AppendLine(SingleUseBuilder.ToString());
                    }

                    stringBuilder.Append("</indent>");
                }
            }
        }

        private void DoAccuracy([NotNull] ISkill skill, int targetCount, ref SkillStruct skillOne, Lease<CharacterStateMachine> targets, ref SkillStruct skillTwo, ref SkillStruct skillThree, ref SkillStruct skillFour)
        {
            if (skill.Accuracy.IsNone)
            {
                accuracyObject.SetActive(false);
                return;
            }

            bool accuracyChanged = false;
            int baseAccuracy = skill.Accuracy.Value;

            for (int i = 0; i < targetCount; i++)
            {
                ref SkillStruct desiredStruct = ref GetSkillStruct(i, ref skillOne, ref skillTwo, ref skillThree, ref skillFour);
                ref CustomValuePooledList<TargetProperties> targetsProperties = ref desiredStruct.TargetProperties;

                for (int j = 0; j < targetsProperties.Count; j++)
                {
                    ref TargetProperties property = ref targetsProperties[j];

                    if (property.AccuracyModifier.TrySome(out int accuracyModifier) == false || accuracyModifier != baseAccuracy)
                    {
                        accuracyChanged = true;
                        break;
                    }
                }
            }

            if (accuracyChanged == false)
            {
                SetAccuracyText(baseAccuracy);
                return;
            }

            for (int i = 0; i < targetCount; i++)
            {
                StringBuilder stringBuilder = TargetStringBuilders[i];
                ref SkillStruct desiredStruct = ref GetSkillStruct(i, ref skillOne, ref skillTwo, ref skillThree, ref skillFour);
                ref CustomValuePooledList<TargetProperties> targetsProperties = ref desiredStruct.TargetProperties;
                skill.Accuracy.TrySome(out int skillBaseAccuracy);

                string accuracyTrans = AccuracyTrans.Translate().GetText();

                if (targetsProperties.Count == 1)
                {
                    targetsProperties[0].AccuracyModifier.TrySome(out int accuracyModifier);
                    SingleUseBuilder.Override(accuracyTrans, ' ', accuracyModifier.ToString("0"));

                    if (accuracyModifier > skillBaseAccuracy)
                        SingleUseBuilder.Surround(ColorReferences.BuffedRichText);
                    else if (accuracyModifier < skillBaseAccuracy)
                        SingleUseBuilder.Surround(ColorReferences.DebuffedRichText);

                    stringBuilder.AppendLine(SingleUseBuilder.ToString());
                }
                else
                {
                    stringBuilder.AppendLine(accuracyTrans, "<indent=15%>");

                    for (int j = 0; j < targetsProperties.Count; j++)
                    {
                        targetsProperties[j].AccuracyModifier.TrySome(out int accuracyModifier);
                        
                        SingleUseBuilder.Override(accuracyModifier.ToString("0"), " > ", targets.Rented[i].Script.CharacterName.Translate().GetText());

                        if (accuracyModifier > skillBaseAccuracy)
                            SingleUseBuilder.Surround(ColorReferences.BuffedRichText);
                        else if (accuracyModifier < skillBaseAccuracy)
                            SingleUseBuilder.Surround(ColorReferences.DebuffedRichText);

                        stringBuilder.AppendLine(SingleUseBuilder.ToString());
                    }

                    stringBuilder.Append("</indent>");
                }
            }
        }

        private void DoRecovery([NotNull] ISkill skill, int targetCount, ref SkillStruct skillOne, ref SkillStruct skillTwo, ref SkillStruct skillThree, ref SkillStruct skillFour)
        {
            bool recoveryChanged = false;

            for (int i = 0; i < targetCount; i++)
            {
                ref SkillStruct desiredStruct = ref GetSkillStruct(i, ref skillOne, ref skillTwo, ref skillThree, ref skillFour);

                if (desiredStruct.Recovery != skill.Recovery)
                {
                    recoveryChanged = true;
                    break;
                }
            }

            if (recoveryChanged == false)
            {
                SetRecoveryText(skill.Recovery);
                return;
            }

            for (int i = 0; i < targetCount; i++)
            {
                StringBuilder stringBuilder = TargetStringBuilders[i];
                ref SkillStruct desiredStruct = ref GetSkillStruct(i, ref skillOne, ref skillTwo, ref skillThree, ref skillFour);
                SingleUseBuilder.Override(RecoveryTrans.Translate().GetText(), ' ', desiredStruct.Recovery.Seconds.ToString("0.0"), 's');

                if (desiredStruct.Recovery < skill.Recovery) // less recovery = better
                    SingleUseBuilder.Surround(ColorReferences.BuffedRichText);
                else if (desiredStruct.Recovery > skill.Recovery)
                    SingleUseBuilder.Surround(ColorReferences.DebuffedRichText);

                stringBuilder.AppendLine(SingleUseBuilder.ToString());
            }
        }

        private void DoPower([NotNull] ISkill skill, int targetCount, ref SkillStruct skillOne, Lease<CharacterStateMachine> targets, ref SkillStruct skillTwo, ref SkillStruct skillThree, ref SkillStruct skillFour)
        {
            if (skill.Power.IsNone)
            {
                damageMultiplierObject.SetActive(false);
                return;
            }

            int basePower = skill.Power.Value;
            bool powerChanged = false;

            for (int i = 0; i < targetCount; i++)
            {
                ref SkillStruct desiredStruct = ref GetSkillStruct(i, ref skillOne, ref skillTwo, ref skillThree, ref skillFour);
                ref CustomValuePooledList<TargetProperties> targetsProperties = ref desiredStruct.TargetProperties;

                for (int j = 0; j < targetsProperties.Count; j++)
                {
                    ref TargetProperties property = ref targetsProperties[j];
                    
                    if (property.Power.TrySome(out int power) == false || power != basePower)
                    {
                        powerChanged = true;
                        break;
                    }
                }
            }

            if (powerChanged == false)
            {
                SetPowerText(basePower);
                return;
            }

            for (int i = 0; i < targetCount; i++)
            {
                StringBuilder stringBuilder = TargetStringBuilders[i];
                ref SkillStruct desiredStruct = ref GetSkillStruct(i, ref skillOne, ref skillTwo, ref skillThree, ref skillFour);
                ref CustomValuePooledList<TargetProperties> targetsProperties = ref desiredStruct.TargetProperties;
                skill.Power.TrySome(out int skillBasePower);
                
                string powerTrans = PowerTrans.Translate().GetText();
                
                if (targetsProperties.Count == 1)
                {
                    targetsProperties[0].Power.TrySome(out int damageModifier);
                    SingleUseBuilder.Override(powerTrans, ' ', damageModifier.ToString("0"));
                    
                    if (damageModifier > skillBasePower)
                        SingleUseBuilder.Surround(ColorReferences.BuffedRichText);
                    else if (damageModifier < skillBasePower)
                        SingleUseBuilder.Surround(ColorReferences.DebuffedRichText);
                    
                    stringBuilder.AppendLine(SingleUseBuilder.ToString());
                }
                else
                {
                    stringBuilder.AppendLine(powerTrans, "<indent=15%>");

                    for (int j = 0; j < targetsProperties.Count; j++)
                    {
                        targetsProperties[j].Power.TrySome(out int power);
                        SingleUseBuilder.Override(power.ToString("0"), " > ", targets.Rented[i].Script.CharacterName.Translate().GetText());

                        if (power > skillBasePower)
                            SingleUseBuilder.Surround(ColorReferences.BuffedRichText);
                        else if (power < skillBasePower)
                            SingleUseBuilder.Surround(ColorReferences.DebuffedRichText);
                        
                        stringBuilder.AppendLine(SingleUseBuilder.ToString());
                    }

                    stringBuilder.Append("</indent>");
                }
            }
        }

        private void DoCharge([NotNull] ISkill skill, int targetCount, ref ChargeStruct chargeOne, ref ChargeStruct chargeTwo, ref ChargeStruct chargeThree, ref ChargeStruct chargeFour)
        {
            bool chargeChanged = false;

            for (int i = 0; i < targetCount; i++)
            {
                ref ChargeStruct desiredStruct = ref GetChargeStruct(i, ref chargeOne, ref chargeTwo, ref chargeThree, ref chargeFour);

                if (desiredStruct.Charge != skill.Charge)
                {
                    chargeChanged = true;
                    break;
                }
            }

            if (chargeChanged == false)
            {
                SetChargeText(skill.Charge);
                return;
            }

            for (int i = 0; i < targetCount; i++)
            {
                StringBuilder stringBuilder = TargetStringBuilders[i];
                ref ChargeStruct desiredStruct = ref GetChargeStruct(i, ref chargeOne, ref chargeTwo, ref chargeThree, ref chargeFour);

                SingleUseBuilder.Override(ChargeTrans.Translate().GetText(), ' ', desiredStruct.Charge.ToString("0.0"), 's');

                if (desiredStruct.Charge < skill.Charge) // less charge = better
                    SingleUseBuilder.Surround(ColorReferences.BuffedRichText);
                else if (desiredStruct.Charge > skill.Charge)
                    SingleUseBuilder.Surround(ColorReferences.DebuffedRichText);

                stringBuilder.AppendLine(SingleUseBuilder.ToString());
            }
        }

        private static void DoUnifiedEffectsText([NotNull] ISkill skill, CharacterStateMachine target, CharacterStateMachine caster)
        {
            ReadOnlySpan<IBaseStatusScript> skillCasterEffects = skill.CasterEffects;
            ReadOnlySpan<IBaseStatusScript> skillTargetEffects = skill.TargetEffects;

            using (CustomValuePooledList<StatusToApply> casterEffects = new(skillCasterEffects.Length))
            using (CustomValuePooledList<StatusToApply> targetEffects = new(skillTargetEffects.Length))
            {
                for (int i = 0; i < skillCasterEffects.Length; i++)
                {
                    StatusToApply record = skillCasterEffects[i].GetActual.GetStatusToApply(caster, caster, crit: false, skill: skill);
                    record.ProcessModifiers();
                    casterEffects.Add(record);
                }

                for (int i = 0; i < skillTargetEffects.Length; i++)
                {
                    StatusToApply record = skillTargetEffects[i].GetActual.GetStatusToApply(caster, target, crit: false, skill: skill);
                    record.ProcessModifiers();
                    targetEffects.Add(record);
                }

                EffectsStringBuilder.Append(skill.GetCustomStatsAndEffectsText(casterEffects.AsSpan(), targetEffects.AsSpan()));
            }
        }

        private static void DoDividedEffectsText([NotNull] ISkill skill, ref CustomValuePooledList<IActualStatusScript> scriptsOne, int targetCount)
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
                StringBuilder stringBuilder = TargetStringBuilders[i];
                foreach (StatusToApply record in ReusableStatusRecordArray[i].effects)
                    stringBuilder.AppendLine(record.GetDescription());
            }

            for (int i = 0; i < ReusableStatusList.Count; i++)
            {
                StatusToApply statusRecord = ReusableStatusList[i];
                EffectsStringBuilder.AppendLine(statusRecord.GetDescription());
            }
        }

        private bool TargetEffectsChanged([NotNull] ISkill skill, ref CustomValuePooledList<IActualStatusScript> targetEffectsOne, int targetCount, CharacterStateMachine caster,
                                          ref SkillStruct skillOne, ref SkillStruct skillTwo, ref SkillStruct skillThree, ref SkillStruct skillFour)
        {
            if (skill.TargetEffects.Length != targetEffectsOne.Count)
                return true;

            for (int index = 1; index < targetCount; index++)
            {
                ref SkillStruct desiredStruct = ref GetSkillStruct(index, ref skillOne, ref skillTwo, ref skillThree, ref skillFour);
                ref CustomValuePooledList<IActualStatusScript> targetEffects = ref desiredStruct.TargetEffects;
                if (targetEffects.Count != targetEffectsOne.Count)
                    return true;
            }

            for (int i = 0; i < ReusableStatusRecordArray.Length; i++)
                ReusableStatusRecordArray[i].effects.Clear();

            for (int i = 0; i < targetCount; i++)
            {
                ref SkillStruct desiredStruct = ref GetSkillStruct(i, ref skillOne, ref skillTwo, ref skillThree, ref skillFour);
                ReusableStatusRecordArray[i].target = desiredStruct.FirstTarget;
                ref CustomValuePooledList<IActualStatusScript> targetEffects = ref desiredStruct.TargetEffects;
                for (int j = 0; j < targetEffects.Count; j++)
                {
                    IActualStatusScript statusScript = targetEffects[j];
                    StatusToApply statusRecord = statusScript.GetStatusToApply(caster, desiredStruct.FirstTarget, crit: false, desiredStruct.Skill);
                    statusRecord.ProcessModifiers();
                    ReusableStatusRecordArray[i].effects.Add(statusRecord);
                }
            }

            for (int i = 1; i < targetCount; i++)
            {
                if (ReusableStatusRecordArray[0].effects.Count != ReusableStatusRecordArray[i].effects.Count)
                    return true;
            }

            for (int i = 0; i < targetEffectsOne.Count; i++)
            {
                for (int j = 1; j < targetCount; j++)
                {
                    if (StatusUtils.DoesRecordsHaveSameStats(ReusableStatusRecordArray[j].effects[i], ReusableStatusRecordArray[0].effects[i]) == false)
                        return true;
                }
            }

            return false;
        }

        [System.Diagnostics.Contracts.Pure]
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

        [System.Diagnostics.Contracts.Pure]
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

        public void RawTooltip([NotNull] ISkill skill)
        {
            skillNameText.text = skill.DisplayName.Translate().GetText();
            SetChargeText(skill.Charge);
            SetRecoveryText(skill.Recovery);
            
            if (skill.Accuracy.TrySome(out int accuracy))
                SetAccuracyText(accuracy);
            else
                accuracyObject.SetActive(false);
            
            if (skill.Power.TrySome(out int basePower))
                SetPowerText(basePower);
            else
                damageMultiplierObject.SetActive(false);
            
            if (skill.CriticalChance.TrySome(out int criticalChance))
                SetCriticalChanceText(criticalChance);
            else
                criticalChanceObject.SetActive(false);

            EffectsStringBuilder.Clear();

            ReadOnlySpan<IBaseStatusScript> skillCasterEffects = skill.CasterEffects;
            ReadOnlySpan<IBaseStatusScript> skillTargetEffects = skill.TargetEffects;
            using (CustomValuePooledList<IActualStatusScript> casterStatusScripts = new(skillCasterEffects.Length))
            using (CustomValuePooledList<IActualStatusScript> targetStatusScripts = new(skillTargetEffects.Length))
            {
                for (int i = 0; i < skillCasterEffects.Length; i++)
                    casterStatusScripts.Add(skillCasterEffects[i].GetActual);

                for (int i = 0; i < skillTargetEffects.Length; i++)
                    targetStatusScripts.Add(skillTargetEffects[i].GetActual);

                EffectsStringBuilder.Append(skill.GetCustomStatsAndEffectsText(casterStatusScripts.AsSpan(), targetStatusScripts.AsSpan()));
            }

            Utils.Patterns.Option<int> currentSkillGetMaxUseCount = skill.GetMaxUseCount;
            if (currentSkillGetMaxUseCount.IsSome)
                EffectsStringBuilder.Append("\n<color=red> ", MaxUsesPerCombatTrans.Translate().GetText(), currentSkillGetMaxUseCount.Value.ToString("0"), "</color>");

            SetEffectsText(EffectsStringBuilder.ToString());
            SetFlavorText(skill.FlavorText.Translate().GetText());

            for (int i = 0; i < castingPositionCircles.Length; i++) 
                castingPositionCircles[i].sprite = skill.CastingPositions[i] ? friendlyCircle : emptyCircle;

            (Sprite dotSprite, Sprite connectorSprite) = skill.IsPositive ? (friendlyCircle, friendlyConnector) : (enemyCircle, enemyConnector);
            
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

        private void SetChargeText(TSpan value) 
            => chargeText.text = SingleUseBuilder.Override(value.ToString("0.0"), "s").ToString();

        private void SetRecoveryText(TSpan value) 
            => recoveryText.text = SingleUseBuilder.Override(value.ToString("0.0"), "s").ToString();

        private void SetAccuracyText(int value)
        {
            accuracyText.text = value.ToString("0");
            accuracyObject.SetActive(true);
        }

        private void SetPowerText(int value)
        {
            dmgMultText.text = value.ToString("0");
            damageMultiplierObject.SetActive(true);
        }

        private void SetCriticalChanceText(int value)
        {
            criticalChanceText.text = value.ToString("0");
            criticalChanceObject.SetActive(true);
        }

        private void SetEffectsText(string value) => effectsText.text = value;
        private void SetFlavorText(string value) => flavorText.text = value;

        private void AppendResiliencePiercingText(int value)
        {
            SingleUseBuilder.Override(ResilienceReductionTrans.Translate().GetText(), value.ToString());
            EffectsStringBuilder.Append(SingleUseBuilder.ToString());
        }
    }
}