using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.Combat.Scripts.Animations;
using Core.Combat.Scripts.Effects.Interfaces;
using Core.Combat.Scripts.Effects.Types.BuffOrDebuff;
using Core.Combat.Scripts.Effects.Types.Riposte;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Managers;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Utils.Collections;
using Core.Utils.Math;
using Core.Utils.Patterns;
using DG.Tweening;
using JetBrains.Annotations;
using UnityEngine;

namespace Core.Combat.Scripts.Behaviour.Modules
{
    public record DefaultSkillModuleRecord(PlanRecord Plan) : SkillModuleRecord
    {
        [NotNull]
        public override ISkillModule Deserialize(CharacterStateMachine owner) => new DefaultSkillModule(owner);

        public override void ApplySerializedPlan(CharacterStateMachine owner, CombatManager combatManager)
        {
            if (Plan == null || Plan.Enqueued ==  true ||  Plan.IsDone == false )
                return;
                
            Option<PlannedSkill> planInstance = PlannedSkill.FromRecord(record: Plan, combatManager);
            if (planInstance.IsNone)
            {
                Debug.LogWarning("Failed to create skill action from json while loading combat from save...");
                return;
            }
                    
            owner.SkillModule.SetActionWithoutNotify(action: planInstance.Value);
        }

        public override bool IsDataValid(in StringBuilder errors, CharacterRecord[] allCharacters)
        {
            if (Plan != null && Plan.IsDataValid(errors, allCharacters) == false)
                return false;
            
            return true;
        }
    }
    
    public class DefaultSkillModule : ISkillModule
    {
        private readonly CharacterStateMachine _owner;

        public DefaultSkillModule(CharacterStateMachine owner) => _owner = owner;

        public SelfSortingList<ISkillModifier> SkillModifiers { get; } = new(ModifierComparer.Instance);
        public SelfSortingList<IChargeModifier> ChargeModifiers { get; } = new(ModifierComparer.Instance);
        public Dictionary<ISkill, int> SkillUseCounters { get; } = new();

        private Option<PlannedSkill> _plannedSkill;
        public Option<PlannedSkill> PlannedSkill => _plannedSkill;

        [NotNull]
        public SkillModuleRecord GetRecord()
        {
            PlanRecord plan = null;
            if (PlannedSkill.TrySome(out PlannedSkill plannedSkill))
                PlanRecord.FromInstance(plannedSkill).TrySome(out plan);
            
            return new DefaultSkillModuleRecord(plan);
        }

        public bool HasChargesIfLimited([NotNull] ISkill skill)
        {
            Option<int> maxUseCount = skill.GetMaxUseCount;
            if (maxUseCount.TrySome(out int limit) && SkillUseCounters.TryGetValue(skill, out int useCount) && useCount >= limit)
                return false;
            
            return true;
        }

        public void PlanSkill(ISkill skill, CharacterStateMachine target)
        {
            if (PlannedSkill is { IsSome: true, Value: { IsDoneOrCancelled: false } })
                return;
            
            ChargeStruct chargeStruct = new(skill, _owner, target);
            ModifyCharge(ref chargeStruct);
            _owner.ChargeModule.SetInitial(duration: chargeStruct.Charge);
            
            chargeStruct.Dispose();
            _plannedSkill = new PlannedSkill(skill, _owner, target, costFree: false);
            if (_owner.Display.AssertSome(out DisplayModule display))
                display.UpdatePredictionIcon(_plannedSkill);
        }

        public bool HasSkill([CanBeNull] ISkill skill)
        {
            if (skill == null)
                return false;

            ReadOnlySpan<ISkill> skills = _owner.Script.Skills;
            for (int i = 0; i < skills.Length; i++)
            {
                if (skills[i] == skill)
                    return true;
            }
            
            return false;
        }

        public void SetActionWithoutNotify(PlannedSkill action)
        {
            _plannedSkill = Option<PlannedSkill>.Some(action);
            if (_owner.Display.AssertSome(out DisplayModule display))
                display.UpdatePredictionIcon(_plannedSkill);
        }

        public ActionResult TakeSkillAsTarget(ref SkillStruct skillStruct, ReadOnlyProperties readOnlyProperties, bool isRiposte)
        {
            ActionResult result = SkillCalculator.DoToTarget(ref skillStruct, readOnlyProperties, isRiposte);
            
            if (isRiposte == false 
             && _owner.StateEvaluator.PureEvaluate() is not CharacterState.Defeated and not CharacterState.Corpse and not CharacterState.Downed
             && skillStruct.Skill.IsPositive == false 
             && readOnlyProperties.Power.IsSome
             && skillStruct.Skill.AnimationType != SkillAnimationType.Overlay
             && _owner.StatusReceiverModule.GetAll.FindType<Riposte>().TrySome(out Riposte riposte) && riposte.IsActive)
            {
                CharacterStateMachine other = skillStruct.Caster;
                DOVirtual.DelayedCall(Riposte.Delay, () => ActivateRiposte(other, riposte), ignoreTimeScale: false).SetTarget(target: _owner);
            }
            
            return result;
        }

        public void ActivateRiposte([NotNull] CharacterStateMachine target, Riposte riposte)
        {
            if (target.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Corpse or CharacterState.Downed
                || _owner.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Corpse or CharacterState.Downed)
                return;

            using (CustomValuePooledList<ActionResult> results = riposte.Activate(target))
            {
                for (int i = 0; i < results.Count; i++)
                {
                    ref ActionResult result = ref results[i];
                    _owner.Events.OnRiposteActivated(ref result);
                }
                
                if (_owner.Display.AssertSome(out DisplayModule display))
                {
                    CasterContext casterContext = new(results.ToArray());
                    CombatAnimation animation = new(riposte.Skill.AnimationParameter, Option<CasterContext>.Some(casterContext), Option<TargetContext>.None);
                    display.SetAnimationWithoutNotifyStatus(animation);
                }

                for (int i = 0; i < results.Count; i++)
                {
                    ref ActionResult result = ref results[i];
                    result.Dispose();
                }
            }
        }

        public void ModifySkill(ref SkillStruct skillStruct)
        {
            foreach (ISkillModifier skillModifier in SkillModifiers) 
                skillModifier.Modify(ref skillStruct);
        }

        public void ModifyCharge(ref ChargeStruct chargeStruct)
        {
            foreach (IChargeModifier chargeModifier in ChargeModifiers) 
                chargeModifier.Modify(ref chargeStruct);
        }

        public void AfterTickUpdate(in TSpan timeStep, in CharacterState previousState, in CharacterState currentState)
        {
            if (PlannedSkill.IsNone || previousState is not CharacterState.Charging ||
                currentState is not (CharacterState.Defeated or CharacterState.Corpse or CharacterState.Downed or CharacterState.Grappled or CharacterState.Grappling) ||
                _plannedSkill.Value.IsDoneOrCancelled != false || _plannedSkill.Value.Enqueued != false)
                return;

            _plannedSkill = Option<PlannedSkill>.None;
            _owner.ChargeModule.Reset();
            if (_owner.Display.AssertSome(out DisplayModule display))
                display.UpdatePredictionIcon(_plannedSkill);
        }

        /// <summary> Does not cancel actions already on the queue. </summary>
        public void UnplanIfTargeting(CharacterStateMachine target)
        {
            if (_plannedSkill.IsNone || PlannedSkill.Value.IsDoneOrCancelled || _owner.Display.TrySome(out DisplayModule display) == false)
                return;

            PlannedSkill plannedSkill = PlannedSkill.Value;
            if (plannedSkill.Target != target)
                return;

            if (plannedSkill.TryPickAnotherTarget())
                return;
            
            if (plannedSkill.Enqueued)
            {
                display.CombatManager.Animations.CancelActionsOfCharacter(_owner);
                _plannedSkill = Option.None;
                _owner.ChargeModule.Reset();
                plannedSkill.NotifyDone();
                display.UpdatePredictionIcon(_plannedSkill);
            }
            else
            {
                _plannedSkill = Option.None;
                _owner.ChargeModule.Reset();
                display.UpdatePredictionIcon(_plannedSkill);
            }
        }
        
        public void CancelPlan()
        {
            if (_plannedSkill.IsNone || PlannedSkill.Value.IsDoneOrCancelled || _owner.Display.TrySome(out DisplayModule display) == false)
                return;
            
            PlannedSkill plannedSkill = PlannedSkill.Value;
            if (plannedSkill.Enqueued)
            {
                display.CombatManager.Animations.CancelActionsOfCharacter(_owner);
                _owner.ChargeModule.Reset();
                plannedSkill.NotifyDone();
                _plannedSkill = Option.None;
                display.UpdatePredictionIcon(_plannedSkill);
            }
            else
            {
                _owner.ChargeModule.Reset();
                plannedSkill.NotifyDone();
                _plannedSkill = Option.None;
                display.UpdatePredictionIcon(_plannedSkill);
            }
        }
    }
}