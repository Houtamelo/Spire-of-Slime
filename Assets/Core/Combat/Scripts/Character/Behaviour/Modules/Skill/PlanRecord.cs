using System;
using System.Text;
using Core.Combat.Scripts.Skills.Action;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Patterns;
using Core.Utils.Extensions;

namespace Core.Combat.Scripts.Behaviour.Modules
{
    public record PlanRecord(CleanString ScriptKey, Guid Guid, Guid Caster, Guid Target, bool WasTargetLeft, CharacterPositioning TargetPositions, bool IsDone, bool Enqueued)
    {
        public bool IsDataValid(in StringBuilder errors, CharacterRecord[] allCharacters)
        {
            if (SkillDatabase.GetSkill(ScriptKey).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(PlanRecord), ". Character script not found in database: ", ScriptKey.ToString());
                return false;
            }
            
            bool foundCaster = false, foundTarget = false;
            for (int i = 0; i < allCharacters.Length; i++)
            {
                CharacterRecord record = allCharacters[i];
                if (record.Guid == Caster)
                    foundCaster = true;
                
                if (record.Guid == Target)
                    foundTarget = true;
            }
            
            if (foundCaster == false)
            {
                errors.AppendLine("Invalid ", nameof(PlanRecord), ". Caster not found in character list: ", Caster.ToString());
                return false;
            }
            
            if (foundTarget == false)
            {
                errors.AppendLine("Invalid ", nameof(PlanRecord), ". Target not found in character list: ", Target.ToString());
                return false;
            }
            
            return true;
        }
        
        public static Option<PlanRecord> FromInstance(PlannedSkill plannedSkill)
        {
            if (plannedSkill is not { IsDoneOrCancelled: false })
                return Option.None;
            
            return new PlanRecord(plannedSkill.Skill.Key, plannedSkill.Guid, plannedSkill.Caster.Guid, plannedSkill.Target.Guid, 
                                  plannedSkill.WasTargetLeft, plannedSkill.TargetInitialPositions, plannedSkill.IsDoneOrCancelled, plannedSkill.Enqueued);
        }
    }
}