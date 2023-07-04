using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Skills.Interfaces;

namespace Core.Combat.Scripts.Interfaces.Modules
{
    public interface IPositionHandler : IModule
    {
        bool IsLeftSide { get; }
        bool IsRightSide { get; }
        bool SetSide(bool isLeft);

        byte Size { get; set; }

        float GetAveragePosition();
        float GetRequiredGraphicalX();

        bool CanPositionCast(ISkill skill);
        bool CanPositionCast(ISkill skill, CharacterPositioning positionses);
        bool CanPositionBeTargetedBy(ISkill skill, CharacterStateMachine caster);
    }
}