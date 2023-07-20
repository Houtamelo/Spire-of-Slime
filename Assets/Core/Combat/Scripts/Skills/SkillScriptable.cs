using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Managers;
using Core.Combat.Scripts.Perks;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Action.Overlay;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Localization.Scripts;
using Core.Save_Management.SaveObjects;
using Core.Utils.Math;
using Core.Utils.Patterns;
using DG.Tweening;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

// ReSharper disable Unity.RedundantHideInInspectorAttribute
// ReSharper disable UseArrayEmptyMethod

namespace Core.Combat.Scripts.Skills
{
    [CreateAssetMenu(menuName = "Database/Combat/Skill File", fileName = "skill_character-name_skill-name"),
     SuppressMessage("ReSharper", "ArgumentsStyleLiteral")]
    public class SkillScriptable : ScriptableObject, ISkill
    {
        public CleanString Key => name;

        [SerializeField]
        private LocalizedText displayName;
        public LocalizedText DisplayName => displayName;
        
        [SerializeField, HideInInspector]
        private LocalizedText flavorText;
        public LocalizedText FlavorText => flavorText;

        [SerializeField]
        private TSpan charge;
        public TSpan Charge => charge;
        
        [SerializeField]
        private TSpan recovery = TSpan.FromSeconds(1);
        public TSpan Recovery => recovery;

        [SerializeField]
        private bool canMiss = true;
        
        [SerializeField, Range(-50, 200), ShowIf(nameof(canMiss))]
        private int accuracy;
        public Option<int> Accuracy => canMiss ? accuracy : Option.None;
        
        [SerializeField]
        private bool dealsDamage;
        
        [SerializeField, Range(0, 300), ShowIf(nameof(dealsDamage))]
        private int power;
        public Option<int> Power => dealsDamage ? power : Option.None;

        [SerializeField]
        private bool canCrit;
        private bool CanCrit => canCrit;

        [SerializeField, Range(-50, 300), ShowIf(nameof(CanCrit))]
        private int criticalChance;
        public Option<int> CriticalChance => CanCrit ? criticalChance : Option.None;
        
        [SerializeField, Range(0, 300), ShowIf(nameof(CanCrit)), ShowIf(nameof(dealsDamage))]
        private int resilienceReduction;
        public Option<int> ResilienceReduction => dealsDamage ? resilienceReduction : Option.None;

        [SerializeField]
        private PositionSetup castingPositions;
        public PositionSetup CastingPositions => castingPositions;
        
        [SerializeField]
        private PositionSetup targetPositions;
        public PositionSetup TargetPositions => targetPositions;
        
        [SerializeField]
        private bool multiTarget;
        public bool MultiTarget => multiTarget;

        [SerializeField]
        private bool isPositive;
        public bool IsPositive => isPositive;
        
        [SerializeField, UsedImplicitly]
        private bool hasIcons;
        
        [SerializeField, ShowIf(nameof(hasIcons))]
        private Sprite iconBackground;
        public Sprite IconBackground => iconBackground;
        
        [SerializeField, ShowIf(nameof(hasIcons))]
        private Sprite iconBaseSprite;
        public Sprite IconBaseSprite => iconBaseSprite;
        
        [SerializeField, ShowIf(nameof(hasIcons))]
        private Sprite iconBaseFx;
        public Sprite IconBaseFx => iconBaseFx;
        
        [SerializeField, ShowIf(nameof(hasIcons))]
        private Sprite iconHighlightedSprite;
        public Sprite IconHighlightedSprite => iconHighlightedSprite;
        
        [SerializeField, ShowIf(nameof(hasIcons))]
        private Sprite iconHighlightedFx;
        public Sprite IconHighlightedFx => iconHighlightedFx;

        [SerializeField]
        private SkillAnimationType animationType;
        public SkillAnimationType AnimationType => animationType;

        [SerializeField, ShowIf(nameof(IsStandard))]
        private string animationParameter;
        public string AnimationParameter => animationParameter;

        private bool IsStandard => animationType == SkillAnimationType.Standard;

        [SerializeField, ShowIf(nameof(IsOverlay)), Required]
        private OverlayAnimator overlayPrefab;

        private bool IsOverlay => animationType == SkillAnimationType.Overlay;

        [JetBrains.Annotations.NotNull]
        public IActionSequence CreateActionSequence(PlannedSkill plan, CombatManager combatManager) =>
            animationType switch
            {
                SkillAnimationType.Standard => new DefaultActionSequence(plan, combatManager),
                SkillAnimationType.Overlay  => new OverlayActionSequence(plan, combatManager, overlayPrefab),
                _                           => throw new ArgumentOutOfRangeException(nameof(animationType), animationType, null)
            };

        public Sequence Announce(Announcer announcer, PlannedSkill plan, float startDuration, float popDuration, float speed) 
            => overlayPrefab.Announce(announcer, plan, startDuration, popDuration, speed);

        [SerializeField, ShowIf(nameof(ShowMovement)), Range(-10, 10), InfoBox("Positive means outwards center of screen, negative means towards center.")]
        private float casterMovement;
        public float CasterMovement => casterMovement;
        
        [SerializeField, ShowIf(nameof(ShowMovement))]
        private AnimationCurve casterAnimationCurve = AnimationCurve.Linear(timeStart: 0, valueStart: 0, timeEnd: 1, valueEnd: 1);
        public AnimationCurve CasterAnimationCurve => casterAnimationCurve;

        [SerializeField, ShowIf(nameof(ShowMovement)), Range(-10, 10)]
        private float targetMovement;
        public float TargetMovement => targetMovement;
        
        [SerializeField, ShowIf(nameof(ShowMovement))]
        private AnimationCurve targetAnimationCurve = AnimationCurve.Linear(timeStart: 0, valueStart: 0, timeEnd: 1, valueEnd: 1);
        public AnimationCurve TargetAnimationCurve => targetAnimationCurve;
        
        private bool ShowMovement => animationType == SkillAnimationType.Standard;

        [SerializeField, ShowIf(nameof(ShowMovement))]
        private bool hasCustomPadding;

        [SerializeField, ShowIf(nameof(ShowPaddingSettings))]
        private ActionPaddingSettings paddingSettings = ActionPaddingSettings.Default();
        public ReadOnlyPaddingSettings GetPaddingSettings() => hasCustomPadding ? paddingSettings : ActionPaddingSettings.Default();

        private bool ShowPaddingSettings => hasCustomPadding && animationType == SkillAnimationType.Standard;

        [SerializeField] 
        private SerializedStatusScript[] targetEffects = new SerializedStatusScript[0];
        public ReadOnlySpan<IBaseStatusScript> TargetEffects => targetEffects;
        
        [SerializeField]
        private SerializedStatusScript[] casterEffects = new SerializedStatusScript[0];
        public ReadOnlySpan<IBaseStatusScript> CasterEffects => casterEffects;
        
        [SerializeField]
        private CustomStatScriptable[] customStats = new CustomStatScriptable[0];
        public ReadOnlySpan<ICustomSkillStat> CustomStats => customStats;

        [SerializeField, OnStateUpdate(nameof(OnTargetTypeChanged))]
        private TargetType targetType;
        public TargetType TargetType => targetType;
        
        [SerializeField]
        private bool hasLimitedUses;
        
        [SerializeField, ShowIf(nameof(hasLimitedUses))]
        private int limitedUseCount;
        public Option<int> GetMaxUseCount => hasLimitedUses ? Option<int>.Some(limitedUseCount) : Option.None;
        
        [SerializeField, Required]
        private PerkScriptable[] perkPrerequisites = new PerkScriptable[0];
        public ReadOnlySpan<PerkScriptable> PerkPrerequisites => perkPrerequisites;
        
        [SerializeField, Required]
        private SkillScriptable[] skillPrerequisites = new SkillScriptable[0];
        public ReadOnlySpan<SkillScriptable> SkillPrerequisites => skillPrerequisites;

        private void OnTargetTypeChanged()
        {
#if UNITY_EDITOR
            if (TargetType is TargetType.OnlySelf or TargetType.CanSelf && IsPositive == false)
            {
                isPositive = true;
                UnityEditor.EditorUtility.SetDirty(this);
            }
#endif
        }

#if UNITY_EDITOR
        public void AssignData(string newDisplayName, string newFlavorText)
        {
            displayName = new LocalizedText(newDisplayName);
            flavorText = new LocalizedText(newFlavorText);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        public void AssignIcons(Sprite background, Sprite baseIcon, Sprite baseFx, Sprite highIcon, Sprite highFx)
        {
            hasIcons = true;
            iconBackground = background;
            iconBaseSprite = baseIcon;
            iconBaseFx = baseFx;
            iconHighlightedSprite = highIcon;
            iconHighlightedFx = highFx;
            UnityEditor.EditorUtility.SetDirty(this);
        }

        private void OnValidate()
        {
            if (name.StartsWith("skill_") == false)
                Debug.Log("Warning, file name does not start with \"skill_\"", context: this);
        }

        [UsedImplicitly]
        private void UseHasIcons() // just so that unity stops complaining
        {
            if (hasIcons && dealsDamage)
                Debug.Log(hasIcons);
        }
#endif
    }
}