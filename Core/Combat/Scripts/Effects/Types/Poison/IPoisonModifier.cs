using Core.Combat.Scripts.Effects.Interfaces;

namespace Core.Combat.Scripts.Effects.Types.Poison
{
    public interface IPoisonModifier : IModifier
    {
        void Modify(ref PoisonToApply effectStruct);
    }
}