using System.Collections.Generic;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Managers;
using Core.Combat.Scripts.Skills.Action;
using DG.Tweening;
using Save_Management;
using TMPro;
using UnityEngine;
using Utils.Patterns;

namespace Core.Combat.Scripts.Skills.Interfaces
{
    public interface ISkill
    {
        CleanString Key { get; }
        string DisplayName { get; }
        string FlavorText { get; }

        float BaseCharge { get; }
        float BaseRecovery { get; }
        
        Option<float> BaseAccuracy { get; }
        Option<float> BaseDamageMultiplier { get; }
        Option<float> BaseCriticalChance { get; }
        Option<float> BaseResiliencePiercing { get; }
        
        PositionSetup CastingPositions { get; }
        PositionSetup TargetPositions { get; }
        
        bool MultiTarget { get; }
        bool AllowAllies { get; }
        
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

        IReadOnlyList<IBaseStatusScript> CasterEffects { get; }
        IReadOnlyList<IBaseStatusScript> TargetEffects { get; }
        
        IReadOnlyList<ICustomSkillStat> CustomStats { get; }
        TargetType TargetType { get; }
        
        /// <summary> Per combat, if option is none then max use is unlimited. </summary>
        Option<uint> GetMaxUseCount { get; }

        ReadOnlyPaddingSettings GetPaddingSettings();
        
        IActionSequence CreateActionSequence(PlannedSkill plan, CombatManager combatManager);
        
        Sequence Announce(Announcer announcer, PlannedSkill plan, float startDuration, float popDuration, float speed);
    }
}