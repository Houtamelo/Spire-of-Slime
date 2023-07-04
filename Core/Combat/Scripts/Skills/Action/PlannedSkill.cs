using System;
using System.Collections.Generic;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Managers;
using Core.Combat.Scripts.Skills.Interfaces;
using ListPool;
using Main_Database.Combat;
using UnityEngine;
using Utils.Patterns;

namespace Core.Combat.Scripts.Skills.Action
{
    public sealed class PlannedSkill
    {
        public readonly CharacterStateMachine Caster;
        public CharacterStateMachine Target;
        public readonly ISkill Skill;
        
        public readonly Guid Guid;
        public readonly bool WasTargetLeft;
        public readonly CharacterPositioning TargetInitialPositions;
        public bool IsDoneOrCancelled { get; private set; }
        public bool Enqueued { get; private set; }
        public bool CostFree { get; }

        public PlannedSkill(ISkill skill, CharacterStateMachine caster, CharacterStateMachine target, bool costFree)
        {
            Skill = skill;
            Caster = caster;
            Target = target;
            
            WasTargetLeft = target.PositionHandler.IsLeftSide;
            
            TargetInitialPositions = target.Display.TrySome(out CharacterDisplay targetDisplay) ? targetDisplay.CombatManager.PositionManager.ComputePositioning(target) : CharacterPositioning.None;
            
			Guid = Guid.NewGuid();
            CostFree = costFree;
        }

        private PlannedSkill(ISkill skill, CharacterStateMachine caster, CharacterStateMachine target, bool wasTargetLeft, CharacterPositioning targetInitialPositions, bool isDoneOrCancelled, bool enqueued, Guid guid)
        {
            Skill = skill;
            Caster = caster;
            Target = target;
            WasTargetLeft = wasTargetLeft;
            TargetInitialPositions = targetInitialPositions;
            IsDoneOrCancelled = isDoneOrCancelled;
            Enqueued = enqueued;
            Guid = guid;
        }

        public static Option<PlannedSkill> CreateInstance(PlanRecord record, CombatManager combatManager)
        {
            Option<SkillScriptable> skill = SkillDatabase.GetSkill(record.ScriptKey);
            
            if (skill.IsNone)
            {
                Debug.LogWarning($"Skill {record.ScriptKey} not found");
                return Option<PlannedSkill>.None;
            }

            Option<CharacterStateMachine> caster = combatManager.Characters.GetByGuid(record.Caster);
            if (caster.IsNone)
            {
                Debug.LogWarning($"Caster {record.Caster} not found");
                return Option<PlannedSkill>.None;
            }
            
            Option<CharacterStateMachine> target = combatManager.Characters.GetByGuid(record.Target);
            if (target.IsNone)
            {
                Debug.LogWarning($"Target {record.Target} not found");
                return Option<PlannedSkill>.None;
            }
            
            bool wasTargetLeft = record.WasTargetLeft;
            CharacterPositioning targetPositions = record.TargetPositions;
            
            bool isDone = record.IsDone;
            bool enqueued = record.Enqueued;
            
            PlannedSkill plannedSkill = new(skill.Value, caster.Value, target.Value, wasTargetLeft, targetPositions, isDone, enqueued, record.Guid);

            return Option<PlannedSkill>.Some(plannedSkill);
        }

        public void NotifyDone()
        {
            IsDoneOrCancelled = true;
        }

        public void Enqueue()
        {
            if (Caster.Display.AssertSome(out CharacterDisplay display))
            {
                Enqueued = true;
                IActionSequence actionSequence = Skill.CreateActionSequence(plan: this, display.CombatManager);
                display.CombatManager.Animations.Enqueue(actionSequence);
            }
        }

        public bool TryPickAnotherTarget()
        {
            if (Skill.MultiTarget == false || Caster.Display.TrySome(out CharacterDisplay display) == false)
                return false;

            CombatManager combatManager = display.CombatManager;
            int previousPos = TargetInitialPositions.size == 0 ? 0 : TargetInitialPositions.Min;
            bool isLeftSide = WasTargetLeft;

            return previousPos switch
            {
                0 => RecursivePlus(current: 0),
                1 => RecursivePlus(current: 1) || RecursiveMinus(current: 0),
                2 => RecursivePlus(current: 2) || RecursiveMinus(current: 1),
                3 => RecursiveMinus(current: 3),
                _ => false
            };

            bool RecursivePlus(int current)
            {
                while (true)
                {
                    Option<CharacterStateMachine> targetOption = combatManager.Characters.GetCharacterAt(current, isLeftSide);
                    if (targetOption.TrySome(out CharacterStateMachine possibleTarget) == false) // if there's no target here then there won't be any target in the next positions
                        return false;

                    if (Skill.TargetPositionOk(Caster, possibleTarget) &&
                        Skill.TargetingTypeOk(Caster, possibleTarget) &&
                        possibleTarget.TargetStateOk())
                    {
                        Target = possibleTarget;
                        return true;
                    }

                    current += 1;
                    if (current is < 0 or > 3)
                        return false;
                }
            }

            bool RecursiveMinus(int current)
            {
                while (true)
                {
                    Option<CharacterStateMachine> targetOption = combatManager.Characters.GetCharacterAt(position: current, isLeftSide: isLeftSide);
                    if (targetOption.TrySome(out CharacterStateMachine possibleTarget) &&
                        Skill.TargetPositionOk(Caster, possibleTarget) &&
                        Skill.TargetingTypeOk(Caster, possibleTarget) &&
                        possibleTarget.TargetStateOk())
                    {
                        Target = possibleTarget;
                        return true;
                    }

                    current -= 1;
                    if (current is < 0 or > 3)
                        return false;
                }
            }
        }
        
        public void FillTargetList(ISet<CharacterStateMachine> targets, ISet<CharacterStateMachine> outsiders)
        {
            if (Caster.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Corpse or CharacterState.Downed or CharacterState.Grappled || Caster.Display.IsNone)
                return;
            
            TargetResolver resolver = new(Skill, Caster, Target);
            using (ValueListPool<CharacterStateMachine> targetList = resolver.GetTargetList())
            {
                foreach (CharacterStateMachine target in targetList)
                    targets.Add(target);
            }

            foreach (CharacterStateMachine character in Caster.Display.Value.CombatManager.Characters.GetAllFixed())
                if (targets.Contains(character) == false && character != Caster)
                    outsiders.Add(character);
        }
    }
}