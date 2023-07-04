using System.Collections.Generic;
using System.Linq;
using Core.Combat.Scripts.Animations;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.Interfaces;
using Core.Combat.Scripts.Effects.Types.BuffOrDebuff;
using Core.Combat.Scripts.Effects.Types.Riposte;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using DG.Tweening;
using Utils.Collections;
using Utils.Patterns;

namespace Core.Combat.Scripts.DefaultModules
{
    public class DefaultSkillModule : ISkillModule
    {
        private readonly CharacterStateMachine _owner;

        public DefaultSkillModule(CharacterStateMachine owner) => _owner = owner;

        public SelfSortingList<ISkillModifier> SkillModifiers { get; } = new(ModifierComparer.Instance);
        public SelfSortingList<IChargeModifier> ChargeModifiers { get; } = new(ModifierComparer.Instance);
        public Dictionary<ISkill, uint> SkillUseCounters { get; } = new();

        private Option<PlannedSkill> _plannedSkill;
        public Option<PlannedSkill> PlannedSkill => _plannedSkill;

        public bool HasChargesIfLimited(ISkill skill)
        {
            Option<uint> maxUseCount = skill.GetMaxUseCount;
            if (maxUseCount.TrySome(out uint limit) && SkillUseCounters.TryGetValue(skill, out uint useCount) && useCount >= limit)
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
            if (_owner.Display.AssertSome(out CharacterDisplay display))
                display.UpdatePredictionIcon(_plannedSkill);
        }

        public bool HasSkill(ISkill skill) => skill != null && _owner.Script.Skills.Contains(skill);

        public void SetActionWithoutNotify(PlannedSkill action)
        {
            _plannedSkill = Option<PlannedSkill>.Some(action);
            if (_owner.Display.AssertSome(out CharacterDisplay display))
                display.UpdatePredictionIcon(_plannedSkill);
        }

        public ActionResult TakeSkillAsTarget(ref SkillStruct skillStruct, ReadOnlyProperties readOnlyProperties, bool isRiposte)
        {
            ActionResult result = SkillUtils.DoToTarget(ref skillStruct, readOnlyProperties, isRiposte);
            
            if (isRiposte == false 
             && _owner.StateEvaluator.PureEvaluate() is not CharacterState.Defeated and not CharacterState.Corpse and not CharacterState.Downed
             && skillStruct.Skill.AllowAllies == false 
             && readOnlyProperties.DamageModifier.IsSome
             && skillStruct.Skill.AnimationType != SkillAnimationType.Overlay
             && _owner.StatusModule.GetAll.FindType<Riposte>().TrySome(out Riposte riposte) && riposte.IsActive)
            {
                CharacterStateMachine other = skillStruct.Caster;
                DOVirtual.DelayedCall(Riposte.Delay, () => ActivateRiposte(other, riposte), ignoreTimeScale: false).SetTarget(target: _owner);
            }
            
            return result;
        }

        public void ActivateRiposte(CharacterStateMachine target, Riposte riposte)
        {
            if (target.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Corpse or CharacterState.Downed
                || _owner.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Corpse or CharacterState.Downed)
            {
                return;
            }
            
            using (NetFabric.Hyperlinq.Lease<ActionResult> results = riposte.Activate(target))
            {
                for (int i = 0; i < results.Length; i++)
                {
                    ref ActionResult result = ref results.Rented[i];
                    _owner.Events.OnRiposteActivated(ref result);
                }
                
                if (_owner.Display.AssertSome(out CharacterDisplay display))
                {
                    CasterContext casterContext = new(results.ToArray());
                    CombatAnimation animation = new(riposte.Skill.AnimationParameter, Option<CasterContext>.Some(casterContext), Option<TargetContext>.None);
                    display.SetAnimationWithoutNotifyStatus(animation);
                }

                for (int i = 0; i < results.Length; i++)
                {
                    ref ActionResult result = ref results.Rented[i];
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

        public void AfterTickUpdate(in float timeStep, in CharacterState previousState, in CharacterState currentState)
        {
            if (PlannedSkill.IsNone || previousState is not CharacterState.Charging ||
                currentState is not (CharacterState.Defeated or CharacterState.Corpse or CharacterState.Downed or CharacterState.Grappled or CharacterState.Grappling) ||
                _plannedSkill.Value.IsDoneOrCancelled != false || _plannedSkill.Value.Enqueued != false)
            {
                return;
            }

            _plannedSkill = Option<PlannedSkill>.None;
            _owner.ChargeModule.Reset();
            if (_owner.Display.AssertSome(out CharacterDisplay display))
                display.UpdatePredictionIcon(_plannedSkill);
        }

        /// <summary> Does not cancel actions already on the queue. </summary>
        public void UnplanIfTargeting(CharacterStateMachine target)
        {
            if (_plannedSkill.IsNone || PlannedSkill.Value.IsDoneOrCancelled || _owner.Display.TrySome(out CharacterDisplay display) == false)
                return;

            PlannedSkill plannedSkill = PlannedSkill.Value;
            if (plannedSkill.Target != target)
                return;

            if (plannedSkill.TryPickAnotherTarget())
                return;
            
            if (plannedSkill.Enqueued)
            {
                display.CombatManager.Animations.CancelActionsOfCharacter(_owner, compensateChargeLost: true);
                _plannedSkill = Option<PlannedSkill>.None;
                _owner.ChargeModule.Reset();
                plannedSkill.NotifyDone();
                display.UpdatePredictionIcon(_plannedSkill);
            }
            else
            {
                _plannedSkill = Option<PlannedSkill>.None;
                CompensateChargeLost();
                _owner.ChargeModule.Reset();
                display.UpdatePredictionIcon(_plannedSkill);
            }
        }
        
        public void CancelPlan(bool compensateChargeLost)
        {
            if (_plannedSkill.IsNone || PlannedSkill.Value.IsDoneOrCancelled || _owner.Display.TrySome(out CharacterDisplay display) == false)
                return;
            
            PlannedSkill plannedSkill = PlannedSkill.Value;
            if (plannedSkill.Enqueued)
            {
                display.CombatManager.Animations.CancelActionsOfCharacter(_owner, compensateChargeLost: compensateChargeLost);
                _owner.ChargeModule.Reset();
                plannedSkill.NotifyDone();
                _plannedSkill = Option.None;
                display.UpdatePredictionIcon(_plannedSkill);
            }
            else
            {
                if (compensateChargeLost)
                    CompensateChargeLost();

                _owner.ChargeModule.Reset();
                plannedSkill.NotifyDone();
                _plannedSkill = Option.None;
                display.UpdatePredictionIcon(_plannedSkill);
            }
        }

        public void CompensateChargeLost()
        {
            float initialCharge = _owner.ChargeModule.GetInitialDuration();
            float remainingCharge = _owner.ChargeModule.GetRemaining();
            float timeLost = initialCharge - remainingCharge;
            if (timeLost > 0) // to compensate charge lost, 25% is lost in the process regardless
            {
                float buffDuration = timeLost * 1.5f;
                BuffOrDebuffScript speedBuff = new(Permanent: false, BaseDuration: buffDuration, BaseApplyChance: 1f, CombatStat.Speed, BaseDelta: 0.5f);
                speedBuff.ApplyEffectWithoutModifying(_owner, _owner, crit: false, skill: null);
            }
        }
    }
}