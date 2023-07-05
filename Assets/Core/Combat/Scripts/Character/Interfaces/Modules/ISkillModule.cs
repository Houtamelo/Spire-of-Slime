using System.Collections.Generic;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.Types.Riposte;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Utils.Collections;
using Core.Utils.Patterns;
using Utils.Patterns;

namespace Core.Combat.Scripts.Interfaces.Modules
{
    public interface ISkillModule : IModule
    {
        Option<PlannedSkill> PlannedSkill { get; }
        void PlanSkill(ISkill skill, CharacterStateMachine target);
        void SetActionWithoutNotify(PlannedSkill action);
        void UnplanIfTargeting(CharacterStateMachine target);
        void CancelPlan(bool compensateChargeLost);
        
        bool HasSkill(ISkill skill);
        ActionResult TakeSkillAsTarget(ref SkillStruct skillStruct, ReadOnlyProperties readOnlyProperties, bool isRiposte);

        SelfSortingList<ISkillModifier> SkillModifiers { get; }
        Dictionary<ISkill, uint> SkillUseCounters { get; }
        void ModifySkill(ref SkillStruct skillStruct);

        SelfSortingList<IChargeModifier> ChargeModifiers { get; }
        void ModifyCharge(ref ChargeStruct chargeStruct);

        bool HasChargesIfLimited(ISkill skill);
        void CompensateChargeLost();

        void ActivateRiposte(CharacterStateMachine target, Riposte riposte);
    }
}