using Core.Combat.Scripts.Behaviour;

namespace Core.Combat.Scripts.Effects.Interfaces
{
    public interface IBaseIntAttributeModifier : IModifier
    {
        void Modify(ref int value, CharacterStateMachine self);
    }
}