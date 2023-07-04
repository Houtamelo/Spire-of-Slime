using Core.Combat.Scripts.Effects.Interfaces;

namespace Core.Combat.Scripts.Skills.Interfaces
{
    public interface ISkillModifier : IModifier
    {
        void Modify(ref SkillStruct skillStruct);
    }
}