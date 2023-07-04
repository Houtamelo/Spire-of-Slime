using Core.Combat.Scripts.Effects.Interfaces;

namespace Core.Combat.Scripts.Effects.Types.Marked
{
    public interface IMarkedModifier : IModifier
    {
        void Modify(ref MarkedToApply effectStruct);
    }
}