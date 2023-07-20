using Core.Combat.Scripts.Behaviour;

namespace Core.Combat.Scripts.Effects.Interfaces
{
    public interface IBaseAttributeModifier : IModifier
    {
        void Modify(ref int value, CharacterStateMachine self);
    }
}