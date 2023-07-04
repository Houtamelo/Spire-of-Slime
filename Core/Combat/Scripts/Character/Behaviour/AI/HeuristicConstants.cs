using Core.Combat.Scripts.Interfaces.Modules;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace Core.Combat.Scripts.Behaviour
{
    public static class HeuristicConstants
    {
        /// <summary>
        /// // Use this as base for pow with duration as the exponent, e.g: Mathf.Pow(PenaltyForOvertime, duration + 1)
        /// </summary>
        public const float PenaltyForOvertime = 0.9666667f;


        public const float LustMultiplier = 0.005f;
        public const float TemptationMultiplier = LustMultiplier * ILustModule.MaxLust;
        
        public const float DurationMultiplier = 1f;
        public const float BuffOrDebuffMultiplier = 0.05f;
        public const float GuardedMultiplier = 0.15f;
        public const float AllyAlreadyHasGuardedMultiplier = 0.25f;
        public const float DamageMultiplier = 0.02f;
        public const float HealMultiplier = 0.015f;
        public const float MarkedMultiplier = 0.1f;
        public const float AllyAlreadyHasMarkedMultiplier = 0.3f;
        public const float PerkMultiplier = 0.15f;
        public const float PermanentMultiplier = 5f;
        public const float RiposteMultiplier = 0.15f;
        public const float AlreadyHasRiposteMultiplier = 0.25f;
        public const float StunMultiplier = 0.5f;
        public const float MoveMultiplier = 0.35f;

        public static float GetHealPriority(float staminaPercentage)
        {
            float x = staminaPercentage;
            float healPriority = Mathf.Clamp((9.0833f * x * x * x) - (12.6690f * x * x) + (3.4006f * x) + 0.7452f, 0f, 1f); // achieved through nonlinear regression
            return healPriority;
        }


        public const float Summon_StaminaMultiplier = 0.04f;
        public const float Summon_ResilienceMultiplier = 0.7f;
        public const float Summon_AccuracyMultiplier = 0.35f;
        public const float Summon_CriticalMultiplier = 0.5f;
        public const float Summon_DodgeMultiplier = 0.35f;
        public const float Summon_DamageMultiplier = 0.1f;
        public static float ProcessSummonResistance(float resistance) => 1 + resistance;
        public static float ProcessSummonStunRecoverySpeed(float speed) => 1 + (speed - 1) * 0.35f;
        
        public const float MultiplierForBeingTargetedWhenMarked = 2f;
        public const float MaxTargetChanceUnmarked = 0.75f;
        public const float MaxTargetChanceMarked = 0.8f;
        public const float MinTargetChanceMarked = 0.55f;
    }
}