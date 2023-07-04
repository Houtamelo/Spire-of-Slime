using Core.Combat.Scripts.Effects.Interfaces;

namespace Core.Combat.Scripts.Effects.Types.Guarded
{
    public interface IGuardedModifier : IModifier
    {
        void Modify(ref GuardedToApply effectStruct);
    }
}