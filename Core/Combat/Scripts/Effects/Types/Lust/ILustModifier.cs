using Core.Combat.Scripts.Effects.Interfaces;

namespace Core.Combat.Scripts.Effects.Types.Lust
{
    public interface ILustModifier : IModifier
    {
        void Modify(ref LustToApply effectStruct);
    }
}