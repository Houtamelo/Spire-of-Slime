using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using Core.Combat.Scripts.Barks;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Cues;
using Core.Combat.Scripts.Effects;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Managers;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Utils.Extensions;
using Core.Utils.Math;
using Core.Utils.Patterns;
using ListPool;
using UnityEngine;
using Utils.Patterns;
using Random = UnityEngine.Random;

namespace Core.Combat.Scripts.Skills
{
    public static class SkillUtils
    {
        private const float MoveBaseSpeed = 1f;
        private static float MoveSpeed => MoveBaseSpeed * IActionSequence.SpeedMultiplier;
        
        private static readonly StringBuilder StringBuilder = new();

        public static ActionResult DoToTarget(ref SkillStruct skillStruct, in ReadOnlyProperties targetProperties, bool isRiposte)
        {
            CharacterStateMachine target = targetProperties.Target;
            if (target.Display.AssertSome(out CharacterDisplay targetDisplay) == false || 
                skillStruct.Caster.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Corpse or CharacterState.Downed or CharacterState.Grappled
                || target.StateEvaluator.PureEvaluate() is CharacterState.Defeated)
            {
                return ActionResult.FromMiss(skillStruct.Skill, skillStruct.Caster, target);
            }

            CharacterState stateBeforeAction = target.StateEvaluator.PureEvaluate();
            bool eligibleForHitAnimation = isRiposte == false && skillStruct.Skill.IsNegative() && 
                                           stateBeforeAction is not CharacterState.Defeated and not CharacterState.Corpse and not CharacterState.Downed and not CharacterState.Grappled;

            Option<float> hitChance = GetHitChance(ref skillStruct, targetProperties);
            if (hitChance.IsSome && Random.value > hitChance.Value)
            {
                CombatCueOptions defaultOptions = CombatCueOptions.Default(text: skillStruct.Skill.IsNegative() ? "Dodge!" : "Miss!", ColorReferences.Accuracy, display: targetDisplay);
                if (CombatTextCueManager.AssertInstance(out CombatTextCueManager cueManager))
                    cueManager.EnqueueAboveCharacter(options: ref defaultOptions, character: targetDisplay);
                
                skillStruct.Caster.PlayBark(BarkType.DodgedAttack);
                ActionResult missResult = ActionResult.FromMiss(skillStruct.Skill, skillStruct.Caster, target);
                target.WasTargetedDuringSkillAnimation(missResult, eligibleForHitAnimation, stateBeforeAction);
                return missResult;
            }

            Option<float> criticalChance = GetCriticalChance(ref skillStruct, targetProperties);
            bool crit = criticalChance.IsSome && Random.value < criticalChance.Value;
            ListPool<StatusResult> statusResults = new();

            ref ValueListPool<IActualStatusScript> targetEffects = ref skillStruct.TargetEffects;
            if (skillStruct.Skill.AllowAllies && target.PositionHandler.IsLeftSide == skillStruct.Caster.PositionHandler.IsLeftSide)
            {
                bool buffBark = false;
                foreach (IActualStatusScript effectScript in targetEffects)
                {
                    StatusResult statusResult = effectScript.ApplyEffect(skillStruct.Caster, target, crit, skillStruct.Skill);
                    statusResults.Add(statusResult);
                    if (statusResult.IsSuccess && effectScript.PlaysBarkAppliedOnAlly)
                        buffBark = true;
                }
                
                if (buffBark)
                    skillStruct.Caster.PlayBark(BarkType.BuffOrHealAlly);
            }
            else if (skillStruct.Skill.AllowAllies == false && target.PositionHandler.IsLeftSide != skillStruct.Caster.PositionHandler.IsLeftSide)
            {
                bool debuffBark = false;
                foreach (IActualStatusScript effectScript in targetEffects)
                {
                    StatusResult statusResult = effectScript.ApplyEffect(caster: skillStruct.Caster, target: target, crit: crit, skill: skillStruct.Skill);
                    statusResults.Add(statusResult);
                    if (statusResult.IsSuccess && effectScript.PlaysBarkAppliedOnEnemy)
                        debuffBark = true;
                }
                
                if (debuffBark)
                {
                    skillStruct.Caster.PlayBark(BarkType.DealtDebuff);
                    target.PlayBark(BarkType.ReceivedDebuff);
                }
            }
            
            if (crit)
            {
                if (targetDisplay.GetCuePosition().TrySome(out Vector3 cuePosition) && CombatTextCueManager.AssertInstance(out CombatTextCueManager cueManager))
                {
                    CombatCueOptions options = new(canShowOnTopOfOthers: true, text: "Critical!", ColorReferences.CriticalChance, position: cuePosition + new Vector3(x: 0, y: 0.1f),
                                                   speed: Vector3.zero, duration: 1.5f, fontSize: CombatCueOptions.DefaultFontSize * 1.5f, fadeOnComplete: true, shake: true);
                    
                    cueManager.IndependentCue(options: ref options);
                }
                
                if (skillStruct.Skill.AllowAllies == false)
                {
                    CombatManager combatManager = targetDisplay.CombatManager;
                    skillStruct.Caster.PlayBark(BarkType.DealtCrit);
                    target.PlayBark(BarkType.ReceivedCrit);

                    foreach (CharacterStateMachine ally in combatManager.Characters.GetOnSide(skillStruct.Caster))
                        if (ally.StateEvaluator.PureEvaluate() is not CharacterState.Defeated and not CharacterState.Corpse and not CharacterState.Downed and not CharacterState.Grappled)
                            ally.PlayBark(BarkType.AllyDealtCrit);

                    foreach (CharacterStateMachine enemy in combatManager.Characters.GetOnSide(skillStruct.Caster.PositionHandler.IsRightSide))
                        if (enemy.StateEvaluator.PureEvaluate() is not CharacterState.Defeated and not CharacterState.Corpse and not CharacterState.Downed and not CharacterState.Grappled)
                            enemy.PlayBark(BarkType.AlyReceivedCrit);
                }
            }
            
            Option<uint> damageDealt;
            if (targetProperties.DamageModifier.IsSome && target.StaminaModule.IsSome && GetDamage(ref skillStruct, targetProperties, crit).TrySome(out (uint lowerDamage, uint upperDamage) damageRange))
            {
                uint damage = (uint)Random.Range(minInclusive: (int)damageRange.lowerDamage, maxExclusive: (int)damageRange.upperDamage + 1);
                damageDealt = Option<uint>.Some(damage);
            }
            else
            {
                damageDealt = Option.None;
            }

            if (damageDealt.IsSome)
                target.StaminaModule.Value.ReceiveDamage(damageDealt.Value, DamageType.Brute, skillStruct.Caster);

            ActionResult result = ActionResult.FromHit(skillStruct.Skill, skillStruct.Caster, target, damageDealt, crit, Option<ListPool<StatusResult>>.Some(statusResults));
            target.WasTargetedDuringSkillAnimation(result, eligibleForHitAnimation, stateBeforeAction);
            return result;
        }
        
        public static ActionResult DoToCaster(ref SkillStruct skillStruct)
        {
            if (skillStruct.Skill.CasterEffects.IsNullOrEmpty())
                return ActionResult.FromHit(skillStruct.Skill, skillStruct.Caster, target: skillStruct.Caster, damageDealt: Option<uint>.None, crit: false, Option<ListPool<StatusResult>>.None);

            Option<float> criticalChanceOption = skillStruct.Skill.BaseCriticalChance;
            
            bool isCrit = criticalChanceOption.IsSome && Random.value < criticalChanceOption.Value;

            ListPool<StatusResult> statusResults = new();
            bool anySuccessBarks = false;
            ref ValueListPool<IActualStatusScript> casterEffects = ref skillStruct.CasterEffects;
            for (int i = 0; i < casterEffects.Count; i++)
            {
                IActualStatusScript effectScript = casterEffects[i];
                StatusToApply effectRecord = effectScript.GetStatusToApply(skillStruct.Caster, target: skillStruct.Caster, isCrit, skillStruct.Skill);
                StatusResult statusResult = effectRecord.ApplyEffect();
                statusResults.Add(statusResult);
                if (statusResult.IsSuccess && effectScript.PlaysBarkAppliedOnCaster)
                    anySuccessBarks = true;
            }
			
            if (anySuccessBarks)
                skillStruct.Caster.PlayBark(BarkType.BuffOrHealSelf);
            
			if (isCrit && CombatTextCueManager.AssertInstance(out CombatTextCueManager cueManager) &&
                skillStruct.Caster.Display.TrySome(out CharacterDisplay casterDisplay) && casterDisplay.GetCuePosition().TrySome(out Vector3 cuePosition))
            {
                CombatCueOptions options = new(canShowOnTopOfOthers: true, text: "Critical!", ColorReferences.CriticalChance, position: cuePosition + new Vector3(x: 0, y: 0.1f), 
                                               speed: Vector3.up, duration: 1.5f, fontSize: CombatCueOptions.DefaultFontSize * 1.5f, fadeOnComplete: true, shake: true);
                
                cueManager.IndependentCue(options: ref options);
            }

            return ActionResult.FromHit(skillStruct.Skill, skillStruct.Caster, skillStruct.Caster, 0, isCrit, statusResults);
        }

        public static (float, AnimationCurve) GetTargetMovement(this ISkill skill, bool isLeftSide) 
            => GetMovement(isLeftSide: isLeftSide, animationMovementType: skill.TargetMovement, skill.TargetAnimationCurve);
        
        public static (float, AnimationCurve) GetCasterMovement(this ISkill skill, bool isLeftSide)
            => GetMovement(isLeftSide: isLeftSide, animationMovementType: skill.CasterMovement, skill.CasterAnimationCurve);
        
        private static (float, AnimationCurve) GetMovement(bool isLeftSide, float animationMovementType, AnimationCurve animationCurve) 
            => (isLeftSide ? animationMovementType * -MoveSpeed : animationMovementType * MoveSpeed, animationCurve);

        public static bool IsNegative(this ISkill skill) => !skill.AllowAllies;

        [Pure]
        public static Option<float> GetCriticalChance(ref SkillStruct skillStruct, in ReadOnlyProperties targetProperties)
        {
            if (targetProperties.CriticalChanceModifier.IsNone)
                return Option<float>.None;
            
            float criticalChance = targetProperties.CriticalChanceModifier.Value + skillStruct.Caster.StatsModule.GetCriticalChance();
            criticalChance = Mathf.Clamp(criticalChance, min: 0, max: 1);
            return criticalChance;
        }

        public static Option<float> GetCriticalChance(ref SkillStruct skillStruct, CharacterStateMachine target)
        {
            Option<ReadOnlyProperties> targetProperties = skillStruct.GetReadOnlyProperties(target);
            if (targetProperties.IsNone)
            {
                Debug.LogWarning($"Skill struct does not have the following target: {target.Script.CharacterName}", target.Display.SomeOrDefault());
                return Option<float>.None;
            }
            
            return GetCriticalChance(ref skillStruct, targetProperties.Value);
        }
        
        [Pure]
        public static Option<float> GetHitChance(ref SkillStruct skillStruct, in ReadOnlyProperties targetProperties)
        {
            if (targetProperties.AccuracyModifier.IsNone)
                return Option<float>.None;
            
            float hitChance = targetProperties.AccuracyModifier.Value + skillStruct.Caster.StatsModule.GetAccuracy() - targetProperties.Target.StatsModule.GetDodge();
            hitChance = Mathf.Clamp(hitChance, min: 0, max: 1);
            return hitChance;
        }
        
        [Pure]
        public static Option<float> GetHitChance(ref SkillStruct skillStruct, CharacterStateMachine target)
        {
            Option<ReadOnlyProperties> targetProperties = skillStruct.GetReadOnlyProperties(target);
            if (targetProperties.IsNone)
            {
                Debug.LogWarning($"Skill struct does not have the following target: {target.Script.CharacterName}", target.Display.SomeOrDefault());
                return Option<float>.None;
            }
            
            return GetHitChance(ref skillStruct, targetProperties.Value);
        }

        public static Option<(uint lowerDamage, uint upperDamage)> GetDamage(ref SkillStruct skillStruct, in ReadOnlyProperties targetProperties, bool crit)
        {
            if (targetProperties.Target.StaminaModule.IsNone)
                return Option.None;
            
            if (targetProperties.Target.StateEvaluator.PureEvaluate() is CharacterState.Downed or CharacterState.Corpse)
                return Option.None;
            
            Option<float> damageMultiplierOption = targetProperties.DamageModifier;
            if (damageMultiplierOption.IsNone)
                return Option.None;

            float damageMultiplier = damageMultiplierOption.Value;
            Option<float> resiliencePiercingOption = targetProperties.ResiliencePiercingModifier;
            float resiliencePiercing = resiliencePiercingOption.SomeOrDefault();

            (float lowerDamage, float upperDamage) = skillStruct.Caster.StatsModule.GetDamageWithMultiplier();
            float targetResilience = Mathf.Clamp(targetProperties.Target.StaminaModule.Value.GetResilience() - resiliencePiercing, 0f, 1f);
            lowerDamage *= damageMultiplier * (1f - targetResilience);
            upperDamage *= damageMultiplier * (1f - targetResilience);

            if (crit)
            {
                upperDamage *= 1.5f;
                lowerDamage = upperDamage;
            }

            uint lowerCeil = lowerDamage.CeilToUInt();
            uint upperCeil = upperDamage.CeilToUInt();
            return (lowerCeil, upperCeil);
        }
        
        public static Option<(uint lowerDamage, uint upperDamage)> GetDamage(ref SkillStruct skillStruct, CharacterStateMachine target, bool crit)
        {
            Option<ReadOnlyProperties> targetProperties = skillStruct.GetReadOnlyProperties(target);
            if (targetProperties.IsNone)
            {
                Debug.LogWarning($"Skill struct does not have the following target: {target.Script.CharacterName}", target.Display.SomeOrDefault());
                return Option.None;
            }
            
            return GetDamage(ref skillStruct, targetProperties.Value, crit);
        }
        
        public static float ComputeHeuristic(ref SkillStruct skillStruct)
        {
            float totalPoints = 0;
            float timeSpent = skillStruct.Skill.BaseCharge + skillStruct.Recovery;
            
            ref ValueListPool<TargetProperties> targets = ref skillStruct.TargetProperties;
            ref ValueListPool<IActualStatusScript> targetEffects = ref skillStruct.TargetEffects;
            for (int targetIndex = 0; targetIndex < targets.Count; targetIndex++)
            {
                ReadOnlyProperties targetProperties = targets[targetIndex].ToReadOnly();
                CharacterStateMachine target = targetProperties.Target;

                float damagePoints = 0;
                Option<(uint lowerDamage, uint upperDamage)> damageOption = GetDamage(ref skillStruct, targetProperties, crit: false);
                if (damageOption.IsSome)
                {
                    (uint lowerDamage, uint upperDamage) damage = damageOption.Value;
                    float averageDamage = (damage.lowerDamage + damage.upperDamage) / 2f;
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

                Option<float> hitChance = GetHitChance(ref skillStruct, targetProperties);
                if (hitChance.IsSome)
                    accumulatedPoints *= hitChance.Value;

                Option<float> criticalChance = GetCriticalChance(ref skillStruct, targetProperties);
                if (criticalChance.IsSome)
                    accumulatedPoints *= (1 + criticalChance.Value / 2f);
                
                totalPoints += accumulatedPoints;
            }
            
            ref ValueListPool<IActualStatusScript> casterEffects = ref skillStruct.CasterEffects;
            for (int i = 0; i < casterEffects.Count; i++)
            {
                IActualStatusScript statusScript = casterEffects[i];
                float points = statusScript.ComputePoints(ref skillStruct, skillStruct.Caster);
                totalPoints += points;
            }

            totalPoints /= timeSpent;
            return totalPoints;
        }

        [Pure]
        public static bool AnySkillWithEffect(this CharacterStateMachine character, EffectType effectType)
        {
            if (character.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Corpse)
                return false;

            foreach (ISkill skill in character.Script.Skills)
                foreach (IBaseStatusScript statusScript in skill.TargetEffects)
                    if (statusScript.EffectType == effectType)
                        return true;

            return false;
        }

        [Pure]
        public static string GetCustomStatsAndEffectsText(this ISkill skill, IEnumerable<IActualStatusScript> casterEffects, IEnumerable<IActualStatusScript> targetEffects)
        {
            StringBuilder.Clear();
            foreach (ICustomSkillStat customStat in skill.CustomStats)
            {
                Option<string> option = customStat.GetDescription();
                if (option.TrySome(out string description))
                    StringBuilder.AppendLine(description);
            }
            
            foreach (IActualStatusScript effect in targetEffects)
                StringBuilder.AppendLine(effect.Description);

            bool anyCaster = false;
            foreach (IActualStatusScript effect in casterEffects)
            {
                if (anyCaster == false)
                {
                    StringBuilder.AppendLine("<align=center>Self:</align>");
                    anyCaster = true;
                }
                StringBuilder.AppendLine(effect.Description);
            }

            return StringBuilder.ToString();
        }
        
        [Pure]
        public static string GetCustomStatsAndEffectsText(this ISkill skill, IEnumerable<StatusToApply> casterEffects, IEnumerable<StatusToApply> targetEffects)
        {
            StringBuilder.Clear();
            foreach (ICustomSkillStat customStat in skill.CustomStats)
            {
                Option<string> option = customStat.GetDescription();
                if (option.TrySome(out string description))
                    StringBuilder.AppendLine(description);
            }
            
            foreach (StatusToApply effect in targetEffects)
                StringBuilder.AppendLine(effect.GetDescription());

            bool anyCaster = false;
            foreach (StatusToApply effect in casterEffects)
            {
                if (anyCaster == false)
                {
                    StringBuilder.AppendLine("<align=center>Self:</align>");
                    anyCaster = true;
                }
                StringBuilder.AppendLine(effect.GetDescription());
            }

            return StringBuilder.ToString();
        }

        [Pure]
        public static string GetFullRawDescription(this ISkill skill)
        {
            StringBuilder.Clear();
            
            StringBuilder.AppendLine($"Charge: {skill.BaseCharge.ToString()}s");
            StringBuilder.AppendLine($"Recovery: {skill.BaseRecovery.ToString()}s");
            
            if (skill.BaseAccuracy.TrySome(out float accuracy))
                StringBuilder.AppendLine($"Accuracy: {accuracy.ToPercentageString()}%");
            
            if (skill.BaseDamageMultiplier.TrySome(out float power))
                StringBuilder.AppendLine($"Power: {power.ToPercentageString()}");
            
            if (skill.BaseCriticalChance.TrySome(out float criticalChance))
                StringBuilder.AppendLine($"Critical: {criticalChance.ToPercentageString()}%");
            
            foreach (ICustomSkillStat customStat in skill.CustomStats)
            {
                Option<string> option = customStat.GetDescription();
                if (option.TrySome(out string description))
                    StringBuilder.AppendLine(description);
            }

            foreach (IActualStatusScript effect in skill.TargetEffects.Select(e => e.GetActual))
                StringBuilder.AppendLine(effect.Description);
            
            if (skill.CasterEffects.Count > 0)
            {
                StringBuilder.AppendLine("<align=center>Self:</align>");
                foreach (IActualStatusScript effect in skill.CasterEffects.Select(e => e.GetActual))
                    StringBuilder.AppendLine(effect.Description);
            }

            string flavorText = skill.FlavorText;
            if (flavorText.IsSome())
            {
                StringBuilder.AppendLine();
                StringBuilder.AppendLine($"<i>{flavorText}</i>");
            }
            
            return StringBuilder.ToString();
        }

        [Pure]
        public static Option<ReadOnlyProperties> GetReadOnlyProperties(this SkillStruct skillStruct, CharacterStateMachine target)
        {
            ref ValueListPool<TargetProperties> targetProperties = ref skillStruct.TargetProperties;
            foreach (TargetProperties property in targetProperties)
                if (property.Target == target)
                    return Option<ReadOnlyProperties>.Some(property.ToReadOnly());

            return Option<ReadOnlyProperties>.None;
        }
    }
}