using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.Combat.Scripts.Effects;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Effects.Interfaces;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Interfaces;
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Combat.Scripts.Managers;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Interfaces;
using ListPool;
using NetFabric.Hyperlinq;
using UnityEngine;
using UnityEngine.Pool;
using Utils.Collections;
using Utils.Extensions;
using Utils.Math;
using Random = UnityEngine.Random;

// ReSharper disable HeuristicUnreachableCode
#pragma warning disable CS0162

namespace Core.Combat.Scripts.Behaviour
{
    public class DefaultAIModule : IAIModule
    {
        private const bool LogHeuristics = false;
        
        private readonly CharacterStateMachine _owner;

        public DefaultAIModule(CharacterStateMachine owner) => _owner = owner;

        public SelfSortingList<IChangeMark> MarkModifiers { get; } = new(ModifierComparer.Instance);

        public float GetMarkedMultiplier(CharacterStateMachine caster)
        {
            float multiplier = HeuristicConstants.MultiplierForBeingTargetedWhenMarked;
            foreach (IChangeMark modifier in MarkModifiers)
                modifier.ChangeMultiplierTargetChance(caster, target: _owner, chance: ref multiplier);

            return multiplier;
        }

        public float GetMaxChanceToBeTargetedWhenMarked(CharacterStateMachine caster)
        {
            float maxChance = HeuristicConstants.MaxTargetChanceMarked;
            foreach (IChangeMark modifier in MarkModifiers)
                modifier.ChangeMaxTargetChance(caster, target: _owner, chance: ref maxChance);

            return maxChance;
        }

        public float GetMinChanceToBeTargetedWhenMarked(CharacterStateMachine caster)
        {
            float minChance = HeuristicConstants.MinTargetChanceMarked;
            foreach (IChangeMark modifier in MarkModifiers)
                modifier.ChangeMinTargetChance(caster, target: _owner, chance: ref minChance);

            return minChance;
        }

        private IPositionHandler PositionHandler => _owner.PositionHandler;
        private ISkillModule SkillModule => _owner.SkillModule;
        private IRecoveryModule RecoveryModule => _owner.RecoveryModule;
        private ICharacterScript Script => _owner.Script;
        
        /// <summary>
        /// Do not use unless confirmed that _owner.Display.IsSome
        /// </summary>
        private CombatManager CombatManager => _owner.Display.Value.CombatManager;

        public void Heuristic()
        {
            if (_owner.StateEvaluator.PureEvaluate() is not CharacterState.Idle || _owner.Display.IsNone)
                return;

            FixedEnumerable<CharacterStateMachine> allies = CombatManager.Characters.GetOnSide(_owner);
            FixedEnumerable<CharacterStateMachine> enemies = CombatManager.Characters.GetEnemies(_owner);
            
            using PooledObject<Dictionary<ISkill, Lease<(CharacterStateMachine target, float rawPoints, float points)>>> pool =
                DictionaryPool<ISkill, Lease<(CharacterStateMachine target, float rawPoints, float points)>>
                .Get(out Dictionary<ISkill, Lease<(CharacterStateMachine target, float rawPoints, float points)>> calculatedSkills);
            
            ValueListPool<string> logs = new(capacity: 16); 

            for (int i = 0; i < Script.Skills.Count; i++)
            {
                ISkill skill = Script.Skills[i];
                if (skill.CastingPositionOk(_owner) == false)
                {
                    logs.Add($"{skill.DisplayName} cannot be cast due to position");
                    continue;
                }
                
                if (skill.BellowUseLimit(_owner) == false)
                {
                    logs.Add($"{skill.DisplayName} cannot be cast due to not having uses, maximum: {skill.GetMaxUseCount}");
                    continue;
                }
                
                Lease<(CharacterStateMachine target, float rawPoints, float points)> lease = ArrayPool<(CharacterStateMachine target, float rawPoints, float points)>.Shared.Lease(4);
                calculatedSkills[skill] = lease;
                int skillCountOnArray = 0;
                ref FixedEnumerable<CharacterStateMachine> possibleTargets = ref skill.AllowAllies ? ref allies : ref enemies;
                for (int j = 0; j < possibleTargets.Length; j++)
                {
                    CharacterStateMachine target = possibleTargets[j];
                    if (skill.TargetingTypeOk(_owner, target) == false)
                    {
                        logs.Add($"{skill.DisplayName} cannot be cast on {target.Script.CharacterName} due to targeting type: {skill.TargetType}");
                        continue;
                    }

                    if (skill.TargetPositionOk(_owner, target) == false)
                    {
                        CharacterPositioning targetPositioning = CombatManager.PositionManager.ComputePositioning(target);
                        logs.Add($"{skill.DisplayName} cannot be cast on {target.Script.CharacterName} due to position, target is at: {targetPositioning.ToString()}");
                        continue;
                    }

                    if (target.TargetStateOk() == false)
                    {
                        logs.Add($"{skill.DisplayName} cannot be cast on {target.Script.CharacterName} due to target's state: {target.StateEvaluator.PureEvaluate()}");
                        continue;
                    }
                    
                    // commented due to design change, might reverse it later
                    // if (skill.MultiTarget == false && target.StateEvaluator.SoftEvaluate() is CharacterState.Defeated or CharacterState.Corpse or CharacterState.Grappled)
                    // {
                    //     logs.Add($"{skill.DisplayName} cannot be cast on {target.Script.CharacterName} due to not being multi target and being in state {target.StateEvaluator.SoftEvaluate()}");
                    //     continue;
                    // }

                    SkillStruct skillStruct = SkillStruct.CreateInstance(skill, _owner, target);
                    skillStruct.ApplyCustomStats();
                    float computedPoints = SkillUtils.ComputeHeuristic(ref skillStruct);
                    if (computedPoints > 0)
                    {
                        lease.Rented[skillCountOnArray] = (target, computedPoints, computedPoints);
                        skillCountOnArray++;
                    }
                    else
                    {
                        logs.Add($"{skill.DisplayName} cannot be cast on {target.Script.CharacterName} due to computed points being: {computedPoints}, allow allies: {skill.AllowAllies}");
                    }

                    skillStruct.Dispose();
                }

                lease.Length = skillCountOnArray;
                skillCountOnArray = CheckMarksAndEnsureTargetChanceLimit(skill, lease);
                if (lease.Length == 0)
                {
                    calculatedSkills.Remove(skill);
                    lease.Dispose();
                    continue;
                }
                
                if (skillCountOnArray == 1)
                    continue;

                float pointsSum = lease.GetPointsSum();
                float pointsAverage = pointsSum / skillCountOnArray;
                float divisor = 1;
                for (int j = 0; j < lease.Length; j++)
                {
                    (_, _, float points) = lease.Rented[j];
                    float relativeError = Mathf.Abs(points - pointsAverage) / pointsAverage;
                    divisor += 1 - relativeError;
                }

                divisor = Mathf.Min(divisor, 1);
                
                for (int j = 0; j < lease.Length; j++)
                {
                    (CharacterStateMachine target, float rawPoints, float points) = lease.Rented[j];
                    float postPoints = points / divisor;
                    lease.Rented[j] = (target, rawPoints, postPoints);
                    logs.Add($"{skill.DisplayName}=>{target.Script.CharacterName}: Raw:{rawPoints}, Post:{postPoints}");
                }
            }

            bool any = false;
            foreach (Lease<(CharacterStateMachine target, float rawPoints, float points)> lease in calculatedSkills.Values)
            {
                if (lease.Length > 0)
                {
                    any = true;
                    break;
                }
            }
            
            if (any)
            {
#if UNITY_EDITOR
                if (LogHeuristics)
                    Log(ref logs);
#endif
                (ISkill skill, CharacterStateMachine target) = GetWeightedRandom(calculatedSkills);
                SkillModule.PlanSkill(skill, target);
            }
            else
            {
                Debug.Log($"Character: {Script.CharacterName} could not find any skill to cast.");
#if UNITY_EDITOR
                if (LogHeuristics)
                    Log(ref logs);
#endif
                RecoveryModule.SetInitial(1);
            }

            foreach ((ISkill _, Lease<(CharacterStateMachine target, float rawPoints, float points)> value) in calculatedSkills)
                value.Dispose();
            
            logs.Dispose();
            allies.Dispose();
            enemies.Dispose();

#if UNITY_EDITOR
            void Log(ref ValueListPool<string> refLogs)
            {
                StringBuilder builder = new();
                builder.AppendLine($"{Script.CharacterName}--------------");
                foreach (string line in refLogs)
                    builder.AppendLine(line);
                
                // normalize calculated points to % of total
                
                builder.AppendLine("Percentages--------------");
                
                float pointsSum = 0;
                foreach (Lease<(CharacterStateMachine target, float rawPoints, float points)> lease in calculatedSkills.Values)
                    pointsSum += lease.GetPointsSum();
                
                foreach ((ISkill skill, Lease<(CharacterStateMachine target, float rawPoints, float points)> lease) in calculatedSkills)
                {
                    foreach ((CharacterStateMachine target, float rawPoints, float points) in lease)
                    {
                        float percentage = points / pointsSum;
                        builder.AppendLine($"{skill.DisplayName}=>{target.Script.CharacterName}: Raw:{rawPoints}, Post:{points}, {percentage.ToPercentageString()}");
                    }
                }

                Debug.Log(builder.ToString());
            }
#endif
        }
        
        /// <returns>Adjusted length of source lease.</returns>
        private int CheckMarksAndEnsureTargetChanceLimit(ISkill skill, Lease<(CharacterStateMachine target, float rawPoints, float points)> source)
        {
            if (skill.MultiTarget || skill.AllowAllies)
                return RemoveNegativesAndZeroes(source);

            using ValueListPool<int> charactersWithMark = new();
            for (int index = 0; index < source.Length; index++)
            {
                (CharacterStateMachine target, float _, float _) = source.Rented[index];
                foreach (StatusInstance status in target.StatusModule.GetAll)
                {
                    if (status.EffectType is not EffectType.Marked || status.IsDeactivated)
                        continue;
                    
                    charactersWithMark.Add(index);
                    break;
                }
            }

            int markedCount = charactersWithMark.Count;
            float totalPoints;
            if (markedCount == 0)
            {
                int newLength = RemoveNegativesAndZeroes(source);
                if (newLength <= 1)
                    return newLength;
                
                totalPoints = source.GetPointsSum();
                Utils.Patterns.Option<(int, float)> indexAboveLimitOption = Utils.Patterns.Option<(int, float)>.None;
                for (int i = 0; i < newLength; i++)
                {
                    (CharacterStateMachine _, float _, float points) = source.Rented[i];
                    float ratio = points / totalPoints;
                    float difference = HeuristicConstants.MaxTargetChanceUnmarked - ratio;
                    if (difference < 0)
                    {
                        indexAboveLimitOption = Utils.Patterns.Option<(int, float)>.Some((i, difference * totalPoints));
                        break;
                    }
                }

                if (indexAboveLimitOption.IsNone)
                    return RemoveNegativesAndZeroes(source);

                int indexAboveLimit = indexAboveLimitOption.Value.Item1;
                float pointsAboveLimit = indexAboveLimitOption.Value.Item2;
                
                float totalPointsBellowLimit = 0f;
                for (int i = 0; i < source.Length; i++)
                    if (i != indexAboveLimit)
                        totalPointsBellowLimit += source.Rented[i].points;

                for (int i = 0; i < newLength; i++)
                {
                    (CharacterStateMachine target, float rawPoints, float points) = source.Rented[i];
                    if (i == indexAboveLimit)
                    {
                        source.Rented[i] = (target, rawPoints, HeuristicConstants.MaxTargetChanceUnmarked * totalPoints);
                        continue;
                    }
                    
                    float ratio = points / totalPointsBellowLimit;
                    source.Rented[i] = (target, rawPoints, points + ratio * pointsAboveLimit);
                }
                
                return RemoveNegativesAndZeroes(source);
            }

            for (int i = 0; i < source.Length; i++)
            {
                if (charactersWithMark.Contains(i) || source.Rented[i].points > 0)
                    continue;
                
                // remove element at _owner index
                for (int j = i; j < source.Length - 1; j++)
                    source.Rented[j] = source.Rented[j + 1];
                
                source.Length -= 1;
            }
            
            if (source.Length <= 1 || source.Length == markedCount)
                return RemoveNegativesAndZeroes(source);

            totalPoints = source.GetPointsSum();
            if (markedCount == 1)
            {
                int index = charactersWithMark[0];
                (CharacterStateMachine target, float rawPoints, float points) = source.Rented[index];
                float minChance = target.AIModule.GetMinChanceToBeTargetedWhenMarked(caster: _owner);
                float maxChance = target.AIModule.GetMaxChanceToBeTargetedWhenMarked(caster: _owner);
                float markedMultiplier = target.AIModule.GetMarkedMultiplier(caster: _owner);
                
                float modifiedPoints = points * markedMultiplier;
                float modifiedChance = modifiedPoints / (totalPoints + modifiedPoints - points);
                modifiedChance = Mathf.Clamp(modifiedChance, minChance, maxChance);

                float desiredPoints = modifiedChance * totalPoints;
                float delta = points - desiredPoints;
                source.Rented[index] = (target, rawPoints, desiredPoints);

                float totalWithoutMarked = totalPoints - points;
                for (int i = 0; i < source.Length; i++)
                {
                    if (i == index)
                        continue;
                    
                    (CharacterStateMachine nonMarkedTarget, float nonMarkedRawPoints, float nonMarkedPoints) = source.Rented[i];
                    float ratio = nonMarkedPoints / totalWithoutMarked;
                    source.Rented[i] = (nonMarkedTarget, nonMarkedRawPoints, nonMarkedPoints + ratio * delta);
                }
                
                return RemoveNegativesAndZeroes(source);
            }

            float markedRatio = markedCount / (float)source.Length;
            float minChanceReservedForMarkeds = 0f;
            float maxChanceReservedForMarkeds = 0f;
            for (int i = 0; i < markedCount; i++)
            {
                CharacterStateMachine target = source.Rented[charactersWithMark[i]].target;
                float minChance = target.AIModule.GetMinChanceToBeTargetedWhenMarked(caster: _owner);
                float maxChance = target.AIModule.GetMaxChanceToBeTargetedWhenMarked(caster: _owner);
                maxChanceReservedForMarkeds += maxChance;
                minChanceReservedForMarkeds += minChance;
            }

            float minChancePostProcess = (markedCount - minChanceReservedForMarkeds) * markedRatio / 1.5f;
            float newMinChanceReserved = minChanceReservedForMarkeds + minChancePostProcess;
            
            float maxChancePostProcess = (markedCount - maxChanceReservedForMarkeds) * markedRatio / 2f;
            float newMaxChanceReserved = maxChanceReservedForMarkeds + maxChancePostProcess;

            float totalPointsWithoutMarked = totalPoints;
            float accumulatedDelta = 0f;
            for (int i = 0; i < markedCount; i++)
            {
                int index = charactersWithMark[i];
                (CharacterStateMachine target, float rawPoints, float points) = source.Rented[index];
                float minChance = (target.AIModule.GetMinChanceToBeTargetedWhenMarked(caster: _owner) / minChanceReservedForMarkeds) * newMinChanceReserved;
                float maxChance = (target.AIModule.GetMaxChanceToBeTargetedWhenMarked(caster: _owner) / maxChanceReservedForMarkeds) * newMaxChanceReserved;
                float markedMultiplier = target.AIModule.GetMarkedMultiplier(caster: _owner);
                
                float modifiedPoints = points * markedMultiplier;
                float modifiedChance = modifiedPoints / (totalPoints + modifiedPoints - points);
                modifiedChance = Mathf.Clamp(modifiedChance, minChance, maxChance);
                
                float desiredPoints = modifiedChance * totalPoints;
                accumulatedDelta += points - desiredPoints;
                source.Rented[index] = (target, rawPoints, desiredPoints);
                
                totalPointsWithoutMarked -= points;
            }
            
            for (int i = 0; i < source.Length; i++)
            {
                if (charactersWithMark.Contains(i))
                    continue;
                
                (CharacterStateMachine nonMarkedTarget, float nonMarkedRawPoints, float nonMarkedPoints) = source.Rented[i];
                float ratio = nonMarkedPoints / totalPointsWithoutMarked;
                source.Rented[i] = (nonMarkedTarget, nonMarkedRawPoints, nonMarkedPoints + ratio * accumulatedDelta);
            }

            return RemoveNegativesAndZeroes(source);
        }


        /// <returns> Adjusted length of source lease. </returns>
        private static int RemoveNegativesAndZeroes(Lease<(CharacterStateMachine target, float rawPoints, float points)> source)
        {
            for (int i = 0; i < source.Length; i++)
            {
                (CharacterStateMachine _, float _, float points) = source.Rented[i];
                if (points > 0)
                    continue;
                        
                // remove element at _owner index
                for (int j = i; j < source.Length - 1; j++)
                    source.Rented[j] = source.Rented[j + 1];
                    
                source.Length -= 1;
            }
            
            return source.Length;
        }

        private static (ISkill skill, CharacterStateMachine target) GetWeightedRandom(Dictionary<ISkill, Lease<(CharacterStateMachine, float, float)>> source)
        {
            float pointsSum = 0f;
            foreach (Lease<(CharacterStateMachine, float, float)> lease in source.Values)
                pointsSum += lease.GetPointsSum();
            
            if (pointsSum <= 0)
            {
                KeyValuePair<ISkill, Lease<(CharacterStateMachine target, float rawPoints, float points)>> first = source.First();
                return (first.Key, first.Value.Rented[0].target);
            }
            
            float current = Random.value * pointsSum;
            foreach ((ISkill skill, Lease<(CharacterStateMachine, float, float)> lease) in source)
            {
                foreach ((CharacterStateMachine character, float _, float points) in lease)
                {
                    current -= points;
                    if (current > 0)
                        continue;

                    return (skill, character);
                }
            }

            {
                KeyValuePair<ISkill, Lease<(CharacterStateMachine target, float rawPoints, float points)>> first = source.First();
                return (first.Key, first.Value.Rented[0].target);
            }
        }
    }
}