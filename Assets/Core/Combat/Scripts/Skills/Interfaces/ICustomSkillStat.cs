using Core.Utils.Patterns;

namespace Core.Combat.Scripts.Skills.Interfaces
{
    public interface ICustomSkillStat
    {
        void Apply(ref SkillStruct skillStruct);
        Option<string> GetDescription();
    }
}