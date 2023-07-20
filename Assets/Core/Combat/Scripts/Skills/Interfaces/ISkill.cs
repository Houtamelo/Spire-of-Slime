using System;
using System.Collections.Generic;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Managers;
using Core.Combat.Scripts.Skills.Action;
using Core.Localization.Scripts;
using Core.Save_Management.SaveObjects;
using Core.Utils.Math;
using Core.Utils.Patterns;
using DG.Tweening;
using UnityEngine;

namespace Core.Combat.Scripts.Skills.Interfaces
{
    public interface ISkill
    {
        CleanString Key { get; }
        LocalizedText DisplayName { get; }
        LocalizedText FlavorText { get; }

        TSpan Charge { get; }
        TSpan Recovery { get; }
        
        Option<int> Accuracy { get; }
        Option<int> Power { get; }
        Option<int> CriticalChance { get; }
        Option<int> ResilienceReduction { get; }
        
        PositionSetup CastingPositions { get; }
        PositionSetup TargetPositions { get; }
        
        bool MultiTarget { get; }
        bool IsPositive { get; }
        
        Sprite IconBackground { get; }
        Sprite IconBaseSprite { get; }
        Sprite IconBaseFx { get; }
        Sprite IconHighlightedSprite { get; }
        Sprite IconHighlightedFx { get; }
        
        SkillAnimationType AnimationType { get; }
        string AnimationParameter { get; }
        float CasterMovement { get; }
        AnimationCurve CasterAnimationCurve { get; }
        float TargetMovement { get; }
        AnimationCurve TargetAnimationCurve { get; }

        ReadOnlySpan<IBaseStatusScript> CasterEffects { get; }
        ReadOnlySpan<IBaseStatusScript> TargetEffects { get; }
        
        ReadOnlySpan<ICustomSkillStat> CustomStats { get; }
        TargetType TargetType { get; }
        
        /// <summary> Per combat, if option is none then max use is unlimited. </summary>
        Option<int> GetMaxUseCount { get; }

        ReadOnlyPaddingSettings GetPaddingSettings();
        
        IActionSequence CreateActionSequence(PlannedSkill plan, CombatManager combatManager);
        
        Sequence Announce(Announcer announcer, PlannedSkill plan, float startDuration, float popDuration, float speed);
    }
}