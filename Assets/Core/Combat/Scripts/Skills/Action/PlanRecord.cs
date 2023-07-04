using System;
using System.Runtime.Serialization;
using Core.Combat.Scripts.Behaviour;
using Save_Management;
using Utils.Patterns;

namespace Core.Combat.Scripts.Skills.Action
{
    [DataContract]
    public record PlanRecord(CleanString ScriptKey, Guid Guid, Guid Caster, Guid Target, bool WasTargetLeft, CharacterPositioning TargetPositions, bool IsDone, bool Enqueued)
    {
        public static Option<PlanRecord> FromInstance(PlannedSkill plannedSkill)
        {
            if (plannedSkill is not { IsDoneOrCancelled: false })
                return Option.None;
            
            return new PlanRecord(plannedSkill.Skill.Key, plannedSkill.Guid, plannedSkill.Caster.Guid, plannedSkill.Target.Guid, 
                                  plannedSkill.WasTargetLeft, plannedSkill.TargetInitialPositions, plannedSkill.IsDoneOrCancelled, plannedSkill.Enqueued);
        }
    }
}