using System;
using System.Collections.Generic;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Managers;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Localization.Scripts;
using Core.Save_Management.SaveObjects;
using Core.Utils.Math;
using Core.Utils.Patterns;
using DG.Tweening;
using JetBrains.Annotations;
using UnityEngine;

namespace Core.Combat.Scripts.Skills
{
    public class TemporarySkill : ISkill
    {
        public CleanString Key { get; init; }
        public LocalizedText DisplayName { get; init; }
        public LocalizedText FlavorText { get; init; }

        public TSpan Charge { get; init; }
        public TSpan Recovery { get; init; }

        public Option<int> Accuracy { get; init; }
        public Option<int> Power { get; init; }
        public Option<int> CriticalChance { get; init; }
        public Option<int> ResilienceReduction { get; init; }

        public PositionSetup CastingPositions { get; init; }
        public PositionSetup TargetPositions { get; init; }
        public bool MultiTarget { get; init; }

        public bool IsPositive { get; init; }

        public Sprite IconBackground { get; init; }
        public Sprite IconBaseSprite { get; init; }
        public Sprite IconBaseFx { get; init; }
        public Sprite IconHighlightedSprite { get; init; }
        public Sprite IconHighlightedFx { get; init; }

        public string AnimationParameter { get; init; }
        public float CasterMovement { get; init; }
        public AnimationCurve CasterAnimationCurve { get; init; }
        public float TargetMovement { get; init; }
        public AnimationCurve TargetAnimationCurve { get; init; }

        public IBaseStatusScript[] targetEffects { get; init; }
        public ReadOnlySpan<IBaseStatusScript> TargetEffects => targetEffects;

        public IBaseStatusScript[] casterEffects { get; init; }
        public ReadOnlySpan<IBaseStatusScript> CasterEffects => casterEffects;
        
        public ICustomSkillStat[] customStats { get; init; }
        public ReadOnlySpan<ICustomSkillStat> CustomStats => customStats;
        
        public TargetType TargetType { get; init; }
        
        public Option<int> GetMaxUseCount { get; init; }
        
        public ActionPaddingSettings PaddingSettings { get; init; }
        public ReadOnlyPaddingSettings GetPaddingSettings() => PaddingSettings;

        public SkillAnimationType AnimationType => SkillAnimationType.Standard;
        [NotNull]
        public IActionSequence CreateActionSequence(PlannedSkill plan, CombatManager combatManager) => new DefaultActionSequence(plan, combatManager);

        public Sequence Announce([NotNull] Announcer announcer, PlannedSkill plan, float startDuration, float popDuration, float speed) 
            => Announcer.DefaultAnnounce(announcer, DisplayName.Translate().GetText(), delayBeforeStart: Option<float>.None, startDuration, speed);
    }
}