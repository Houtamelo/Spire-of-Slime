using Core.Combat.Scripts.Effects.Interfaces;

namespace Core.Combat.Scripts.Effects.Types.Move
{
    public interface IMoveModifier : IModifier
    {
        void Modify(ref MoveToApply moveStruct);
    }
}