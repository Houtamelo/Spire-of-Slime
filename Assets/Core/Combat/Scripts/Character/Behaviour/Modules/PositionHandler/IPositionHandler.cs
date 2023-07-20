using Core.Combat.Scripts.Skills.Interfaces;

namespace Core.Combat.Scripts.Behaviour.Modules
{
    public abstract record PositionHandlerRecord(int Size) : ModuleRecord
    {
        public abstract IPositionHandler Deserialize(CharacterStateMachine owner); 
    }
    
    public interface IPositionHandler : IModule
    {
        bool IsLeftSide { get; }
        bool IsRightSide { get; }
        bool SetSide(bool isLeft);

        int Size { get; set; }

        float GetAveragePosition();
        float GetRequiredGraphicalX();

        bool CanPositionCast(ISkill skill);
        bool CanPositionCast(ISkill skill, CharacterPositioning positions);
        bool CanPositionBeTargetedBy(ISkill skill, CharacterStateMachine caster);
        
        PositionHandlerRecord GetRecord();
    }
}