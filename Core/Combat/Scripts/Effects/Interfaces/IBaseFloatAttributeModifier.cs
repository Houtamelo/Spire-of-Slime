using Core.Combat.Scripts.Behaviour;

namespace Core.Combat.Scripts.Effects.Interfaces
{
    public interface IBaseFloatAttributeModifier : IModifier
    {
        void Modify(ref float value, CharacterStateMachine self);
    }
}