using System;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;

namespace Core.Combat.Scripts.Effects.BaseTypes
{
    public interface IActualStatusScript : IBaseStatusScript, IEquatable<IActualStatusScript>
    {
        StatusResult ApplyEffect(CharacterStateMachine caster, CharacterStateMachine target, bool crit, ISkill skill = null);
        string Description { get; }
        
        /// <returns>Positive if good for the target, otherwise negative</returns>
        float ComputePoints(ref SkillStruct skillStruct, CharacterStateMachine target);
        
        StatusToApply GetStatusToApply(CharacterStateMachine caster, CharacterStateMachine target, bool crit, ISkill skill = null);
        bool PlaysBarkAppliedOnCaster { get; }
        bool PlaysBarkAppliedOnEnemy { get; }
        bool PlaysBarkAppliedOnAlly { get; }
    }
}