using System;
using System.Collections.Generic;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Managers;
using Core.Combat.Scripts.Perks;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Action.Overlay;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Save_Management.SaveObjects;
using Core.Utils.Patterns;
using DG.Tweening;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils.Patterns;
// ReSharper disable Unity.RedundantHideInInspectorAttribute
// ReSharper disable UseArrayEmptyMethod

namespace Core.Combat.Scripts.Skills
{
    [CreateAssetMenu(menuName = "Database/Combat/Skill File", fileName = "skill_character-name_skill-name")]
    public class SkillScriptable : ScriptableObject, ISkill
    {
        public CleanString Key => name;
        
        [field: SerializeField] 
        public string DisplayName { get; private set; }
        
        [field: SerializeField, HideInInspector]
        public string FlavorText { get; private set; }
        
        [field: SerializeField, PropertyRange(0, 4f), LabelText("Charge"), PropertyOrder(1)]
        public float BaseCharge { get; private set; }
        
        [field: SerializeField, PropertyRange(0, 4f), LabelText("Recovery"), PropertyOrder(2)]
        public float BaseRecovery { get; private set; }

        [SerializeField, PropertyOrder(3)]
        private bool canMiss = true;
        
        [field: SerializeField, SuffixLabel("%"), PropertyRange(-50, 200), LabelText("Accuracy"), PropertyOrder(4), ShowIf(nameof(canMiss))]
        private int serializedAccuracy;
        public Option<float> BaseAccuracy => canMiss ? serializedAccuracy / 100f : Option<float>.None;
        
        [SerializeField, PropertyOrder(5)]
        private bool dealsDamage;
        
        [field: SerializeField, SuffixLabel("%"), PropertyRange(0, 300), LabelText("Damage Multiplier"), PropertyOrder(6), ShowIf(nameof(dealsDamage))]
        private int serializedDamageMultiplier;
        public Option<float> BaseDamageMultiplier => dealsDamage ? serializedDamageMultiplier / 100f : Option<float>.None;

        [field: SerializeField, PropertyOrder(7)]
        private bool CanCrit { get; set; }

        [field: SerializeField, SuffixLabel("%"), PropertyRange(-50, 300), ShowIf(nameof(CanCrit)), LabelText("Critical Chance"), PropertyOrder(8)]
        private int serializedCriticalChance;
        public Option<float> BaseCriticalChance => CanCrit ? serializedCriticalChance / 100f : Option<float>.None;
        
        [field: SerializeField, SuffixLabel("%"), PropertyRange(0, 300), ShowIf(nameof(CanCrit)), LabelText("Resilience Piercing"), PropertyOrder(9), ShowIf(nameof(dealsDamage))]
        private int serializedResiliencePiercing;
        public Option<float> BaseResiliencePiercing => dealsDamage ? serializedResiliencePiercing / 100f : Option<float>.None;

        [field: SerializeField, PropertyOrder(10)]
        public PositionSetup CastingPositions { get; private set; }
        
        [field: SerializeField, PropertyOrder(11)]
        public PositionSetup TargetPositions { get; private set; }
        
        [field: SerializeField, PropertyOrder(12)]
        public bool MultiTarget { get; private set; }
        
        [field: SerializeField, PropertyOrder(13)]
        public bool AllowAllies { get; private set; }
        
        [field: SerializeField, PropertyOrder(-1), UsedImplicitly]
        private bool hasIcons;
        
        [field: SerializeField, ShowIf(nameof(hasIcons))]
        public Sprite IconBackground { get; private set; }
        
        [field: SerializeField, ShowIf(nameof(hasIcons))]
        public Sprite IconBaseSprite { get; private set; }
        
        [field: SerializeField, ShowIf(nameof(hasIcons))]
        public Sprite IconBaseFx { get; private set; }
        
        [field: SerializeField, ShowIf(nameof(hasIcons))] 
        public Sprite IconHighlightedSprite { get; private set; }
        
        [field: SerializeField, ShowIf(nameof(hasIcons))]
        public Sprite IconHighlightedFx { get; private set; }

        [SerializeField]
        private SkillAnimationType animationType;
        public SkillAnimationType AnimationType => animationType;

        [field: SerializeField, ShowIf(nameof(ShowParameter))]
        public string AnimationParameter { get; private set; }

        [SerializeField, ShowIf(nameof(ShowOverlayPrefab))]
        private OverlayAnimator overlayPrefab;

        private bool ShowParameter => animationType == SkillAnimationType.Standard;
        private bool ShowOverlayPrefab => animationType == SkillAnimationType.Overlay;

        public IActionSequence CreateActionSequence(PlannedSkill plan, CombatManager combatManager)
        {
            return animationType switch
            {
                SkillAnimationType.Standard => new DefaultActionSequence(plan, combatManager),
                SkillAnimationType.Overlay  => new OverlayActionSequence(plan, combatManager, overlayPrefab),
                _                           => throw new ArgumentOutOfRangeException(nameof(animationType), animationType, null)
            };
        }

        public Sequence Announce(Announcer announcer, PlannedSkill plan, float startDuration, float popDuration, float speed) 
            => overlayPrefab.Announce(announcer, plan, startDuration, popDuration, speed);

        [field: SerializeField, ShowIf(nameof(ShowMovement)), PropertyRange(-10, 10), InfoBox("Positive means outwards center of screen, negative means towards center.")]
        public float CasterMovement { get; private set; }
        
        [SerializeField, ShowIf(nameof(ShowMovement))]
        private AnimationCurve casterAnimationCurve = AnimationCurve.Linear(timeStart: 0, valueStart: 0, timeEnd: 1, valueEnd: 1);
        public AnimationCurve CasterAnimationCurve => casterAnimationCurve;
        
        [field: SerializeField, ShowIf(nameof(ShowMovement)), PropertyRange(-10, 10)]
        public float TargetMovement { get; private set; }
        
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
        public IReadOnlyList<IBaseStatusScript> TargetEffects => targetEffects;
        
        [SerializeField]
        private SerializedStatusScript[] casterEffects = new SerializedStatusScript[0];
        public IReadOnlyList<IBaseStatusScript> CasterEffects => casterEffects;
        
        [SerializeField]
        private CustomStatScriptable[] customStats = new CustomStatScriptable[0];
        public IReadOnlyList<ICustomSkillStat> CustomStats => customStats;

        [field: SerializeField, OnStateUpdate(nameof(OnTargetTypeChanged))] 
        public TargetType TargetType { get; private set; }
        
        [SerializeField]
        private bool hasLimitedUses;
        
        [SerializeField, ShowIf(nameof(hasLimitedUses))]
        private uint limitedUseCount;
        public Option<uint> GetMaxUseCount => hasLimitedUses ? Option<uint>.Some(limitedUseCount) : Option.None;
        
        [field: SerializeField]
        public PerkScriptable[] PerkPrerequisites { get; private set; } = new PerkScriptable[0];
        
        [field: SerializeField]
        public SkillScriptable[] SkillPrerequisites { get; private set; } = new SkillScriptable[0];

        private void OnTargetTypeChanged()
        {
            if (TargetType is TargetType.OnlySelf or TargetType.CanSelf && AllowAllies == false)
                AllowAllies = true;
        }

#if UNITY_EDITOR
        public void AssignData(string displayName, string flavorText)
        {
            DisplayName = displayName;
            FlavorText = flavorText;
            UnityEditor.EditorUtility.SetDirty(this);
        }

        public void AssignIcons(Sprite background, Sprite baseIcon, Sprite baseFx, Sprite highIcon, Sprite highFx)
        {
            hasIcons = true;
            IconBackground = background;
            IconBaseSprite = baseIcon;
            IconBaseFx = baseFx;
            IconHighlightedSprite = highIcon;
            IconHighlightedFx = highFx;
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