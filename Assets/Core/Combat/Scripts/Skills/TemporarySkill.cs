using System.Collections.Generic;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Managers;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using DG.Tweening;
using Save_Management;
using TMPro;
using UnityEngine;
using Utils.Patterns;

namespace Core.Combat.Scripts.Skills
{
    public class TemporarySkill : ISkill
    {
        public CleanString Key { get; init; }
        public string DisplayName { get; init; }
        public string FlavorText { get; init; }
        public float BaseCharge { get; init; }
        public float BaseRecovery { get; init; }
        public Option<float> BaseAccuracy { get; init; }
        public Option<float> BaseDamageMultiplier { get; init; }
        public Option<float> BaseCriticalChance { get; init; }
        public Option<float> BaseResiliencePiercing { get; init; }
        public PositionSetup CastingPositions { get; init; }
        public PositionSetup TargetPositions { get; init; }
        public bool MultiTarget { get; init; }
        public bool AllowAllies { get; init; }
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
        public IReadOnlyList<IBaseStatusScript> TargetEffects { get; init; }
        public IReadOnlyList<IBaseStatusScript> CasterEffects { get; init; }
        public IReadOnlyList<ICustomSkillStat> CustomStats { get; init; }
        public TargetType TargetType { get; init; }
        public Option<uint> GetMaxUseCount { get; init; }
        public ActionPaddingSettings PaddingSettings { get; init; }
        public ReadOnlyPaddingSettings GetPaddingSettings() => PaddingSettings;

        public SkillAnimationType AnimationType => SkillAnimationType.Standard;
        public IActionSequence CreateActionSequence(PlannedSkill plan, CombatManager combatManager) => new DefaultActionSequence(plan, combatManager);

        public Sequence Announce(Announcer announcer, PlannedSkill plan, float startDuration, float popDuration, float speed) 
            => Announcer.DefaultAnnounce(announcer, DisplayName, delayBeforeStart: Option<float>.None, startDuration, speed);
    }
}