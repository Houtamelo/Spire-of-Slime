using System;
using Core.Combat.Scripts.Skills.Action;
using Core.Utils.Patterns;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace Core.Combat.Scripts.Animations
{
    public readonly struct CombatAnimation
    {
        private const float SelfHitBaseAudioDelay = 0.1f;
        public static float SelfHitAudioDelay => SelfHitBaseAudioDelay * IActionSequence.DurationMultiplier;
        
        public const string Param_Hit = "Hit";
        public const string Param_Idle = "Idle";
        public const string Param_Downed = "Downed";

        public static readonly CombatAnimation Idle = new(Param_Idle, Option<CasterContext>.None, Option<TargetContext>.None);
        public static readonly CombatAnimation Downed = new(Param_Downed, Option<CasterContext>.None, Option<TargetContext>.None);

        public readonly string ParameterName;
        public readonly int ParameterId;
        public readonly Option<CasterContext> CasterContext;
        public readonly Option<TargetContext> TargetContext;

        public CombatAnimation(string parameterName, Option<CasterContext> actionCasterContext, Option<TargetContext> targetContext, int parameterId)
        {
            ParameterName = parameterName;
            ParameterId = parameterId;
            TargetContext = targetContext;
            CasterContext = actionCasterContext;
        }

        public CombatAnimation(string parameterName, Option<CasterContext> actionCasterContext, Option<TargetContext> targetContext)
        {
            ParameterName = parameterName;
            ParameterId = Animator.StringToHash(parameterName);
            CasterContext = actionCasterContext;
            TargetContext = targetContext;
        }

        public static bool operator ==(CombatAnimation left, CombatAnimation right) => left.ParameterId == right.ParameterId;

        public static bool operator !=(CombatAnimation left, CombatAnimation right) => !(left == right);

        public bool Equals(CombatAnimation other) => ParameterId == other.ParameterId;
        
        public override bool Equals(object obj) => obj is CombatAnimation other && Equals(other);
        
        public override int GetHashCode() => HashCode.Combine(ParameterId);
    }
}