using System.Collections.Generic;
using Core.Combat.Scripts.Effects.Types.Riposte;
using Core.Combat.Scripts.Managers;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Utils.Collections;
using Core.Utils.Math;
using Core.Utils.Patterns;

namespace Core.Combat.Scripts.Behaviour.Modules
{
    public abstract record SkillModuleRecord : ModuleRecord
    {
        public abstract ISkillModule Deserialize(CharacterStateMachine owner);
        public abstract void ApplySerializedPlan(CharacterStateMachine owner, CombatManager combatManager);
    }
    
    public interface ISkillModule : IModule
    {
        Option<PlannedSkill> PlannedSkill { get; }
        TSpan PlanSkill(ISkill skill, CharacterStateMachine target);
        void SetActionWithoutNotify(PlannedSkill action);
        void UnplanIfTargeting(CharacterStateMachine target);
        void CancelPlan();
        
        bool HasSkill(ISkill skill);
        ActionResult TakeSkillAsTarget(ref SkillStruct skillStruct, ReadOnlyProperties readOnlyProperties, bool isRiposte);

        SelfSortingList<ISkillModifier> SkillModifiers { get; }
        Dictionary<ISkill, int> SkillUseCounters { get; }
        void ModifySkill(ref SkillStruct skillStruct);

        SelfSortingList<IChargeModifier> ChargeModifiers { get; }
        void ModifyCharge(ref ChargeStruct chargeStruct);

        bool HasChargesIfLimited(ISkill skill);

        void ActivateRiposte(CharacterStateMachine target, Riposte riposte);
        
        SkillModuleRecord GetRecord();
    }
}