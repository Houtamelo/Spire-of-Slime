using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.Combat.Scripts.Barks;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Behaviour.Modules;
using Core.Combat.Scripts.Cues;
using Core.Combat.Scripts.Effects;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Managers;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Localization.Scripts;
using Core.Utils.Collections;
using Core.Utils.Extensions;
using Core.Utils.Math;
using Core.Utils.Patterns;
using JetBrains.Annotations;
using ListPool;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Core.Combat.Scripts.Skills
{
    public static class SkillUtils
    {
        private const float BaseMoveSpeed = 1f;
        private static float MoveSpeed => BaseMoveSpeed * IActionSequence.SpeedMultiplier;

        private static readonly StringBuilder Builder = new();

        public static (float, AnimationCurve) GetTargetMovement([NotNull] this ISkill skill, bool isLeftSide) 
            => GetMovement(isLeftSide, skill.TargetMovement, skill.TargetAnimationCurve);
        
        public static (float, AnimationCurve) GetCasterMovement([NotNull] this ISkill skill, bool isLeftSide)
            => GetMovement(isLeftSide, skill.CasterMovement, skill.CasterAnimationCurve);
        
        private static (float, AnimationCurve) GetMovement(bool isLeftSide, float animationMovementType, AnimationCurve animationCurve) 
            => (isLeftSide ? animationMovementType * -MoveSpeed : animationMovementType * MoveSpeed, animationCurve);

        public static bool IsNegative([NotNull] this ISkill skill) => skill.IsPositive == false;

        public static float ComputeHeuristic(ref SkillStruct skillStruct)
        {
            float totalPoints = 0;
            TSpan timeSpent = skillStruct.Skill.Charge + skillStruct.Recovery;
            
            ref CustomValuePooledList<TargetProperties> targets = ref skillStruct.TargetProperties;
            ref CustomValuePooledList<IActualStatusScript> targetEffects = ref skillStruct.TargetEffects;
            
            for (int targetIndex = 0; targetIndex < targets.Count; targetIndex++)
            {
                ReadOnlyProperties targetProperties = targets[targetIndex].ToReadOnly();
                CharacterStateMachine target = targetProperties.Target;

                float damagePoints = 0;
                Option<(int lowerDamage, int upperDamage)> damageOption = SkillCalculator.FinalDamage(ref skillStruct, targetProperties, crit: false);
                if (damageOption.IsSome)
                {
                    (int lowerDamage, int upperDamage) damage = damageOption.Value;
                    float averageDamage = ((damage.lowerDamage + damage.upperDamage) * target.StatsModule.GetPower()) / 200f;
                    damagePoints = averageDamage * HeuristicConstants.DamageMultiplier;
                } 
                
                float effectPoints = 0; // positive means good for the target
                for (int i = 0; i < targetEffects.Count; i++)
                {
                    IActualStatusScript statusScript = targetEffects[i];
                    float points = statusScript.ComputePoints(ref skillStruct, target);
                    effectPoints += points; //statusScript.IsPositive ? points : -points;
                }

                if (skillStruct.Caster.PositionHandler.IsLeftSide == target.PositionHandler.IsLeftSide)
                    damagePoints *= -1f;
                else
                    effectPoints *= -1f; // if target is an enemy, negative points are good for the caster

                float accumulatedPoints = damagePoints + effectPoints;

                Option<int> hitChance = SkillCalculator.FinalHitChance(ref skillStruct, targetProperties);
                if (hitChance.IsSome)
                    accumulatedPoints *= hitChance.Value / 100f;

                Option<int> criticalChance = SkillCalculator.FinalCriticalChance(ref skillStruct, targetProperties);
                if (criticalChance.IsSome)
                    accumulatedPoints *= 1 + (criticalChance.Value / 200f);
                
                totalPoints += accumulatedPoints;
            }
            
            ref CustomValuePooledList<IActualStatusScript> casterEffects = ref skillStruct.CasterEffects;
            
            for (int i = 0; i < casterEffects.Count; i++)
            {
                IActualStatusScript statusScript = casterEffects[i];
                float points = statusScript.ComputePoints(ref skillStruct, skillStruct.Caster);
                totalPoints += points;
            }

            totalPoints /= timeSpent.FloatSeconds;
            return totalPoints;
        }

        [System.Diagnostics.Contracts.Pure]
        public static bool AnySkillWithEffect([NotNull] this CharacterStateMachine character, EffectType effectType)
        {
            if (character.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Corpse)
                return false;

            foreach (ISkill skill in character.Script.Skills)
            {
                ReadOnlySpan<IBaseStatusScript> skillTargetEffects = skill.TargetEffects;
                for (int index = 0; index < skillTargetEffects.Length; index++)
                {
                    if (skillTargetEffects[index].EffectType == effectType)
                        return true;
                }
            }

            return false;
        }

        public static readonly LocalizedText SelfTrans = new("skill_tooltip_self");

        [System.Diagnostics.Contracts.Pure, NotNull]
        public static string GetCustomStatsAndEffectsText([NotNull] this ISkill skill, ReadOnlySpan<IActualStatusScript> casterEffects, ReadOnlySpan<IActualStatusScript> targetEffects)
        {
            Builder.Clear();
            foreach (ICustomSkillStat customStat in skill.CustomStats)
            {
                if (customStat.GetDescription().TrySome(out string description))
                    Builder.AppendLine(description);
            }

            for (int index = 0; index < targetEffects.Length; index++)
                Builder.AppendLine(targetEffects[index].Description);

            bool anyCaster = false;

            for (int index = 0; index < casterEffects.Length; index++)
            {
                IActualStatusScript effect = casterEffects[index];

                if (anyCaster == false)
                {
                    Builder.AppendLine("<align=center>", SelfTrans.Translate().GetText(), "</align>");
                    anyCaster = true;
                }

                Builder.AppendLine(effect.Description);
            }

            return Builder.ToString();
        }

        [System.Diagnostics.Contracts.Pure, NotNull]
        public static string GetCustomStatsAndEffectsText([NotNull] this ISkill skill, ReadOnlySpan<StatusToApply> casterEffects, ReadOnlySpan<StatusToApply> targetEffects)
        {
            Builder.Clear();
            foreach (ICustomSkillStat customStat in skill.CustomStats)
            {
                if (customStat.GetDescription().TrySome(out string description))
                    Builder.AppendLine(description);
            }

            for (int index = 0; index < targetEffects.Length; index++)
                Builder.AppendLine(targetEffects[index].GetDescription());

            bool anyCaster = false;

            for (int index = 0; index < casterEffects.Length; index++)
            {
                StatusToApply effect = casterEffects[index];

                if (anyCaster == false)
                {
                    Builder.AppendLine("<align=center>", SelfTrans.Translate().GetText(), "</align>");
                    anyCaster = true;
                }

                Builder.AppendLine(effect.GetDescription());
            }

            return Builder.ToString();
        }

        [System.Diagnostics.Contracts.Pure]
        public static Option<ReadOnlyProperties> GetReadOnlyProperties(this SkillStruct skillStruct, CharacterStateMachine target)
        {
            ref CustomValuePooledList<TargetProperties> targetProperties = ref skillStruct.TargetProperties;
            
            foreach (TargetProperties property in targetProperties)
            {
                if (property.Target == target)
                    return property.ToReadOnly();
            }

            return Option.None;
        }
    }
}