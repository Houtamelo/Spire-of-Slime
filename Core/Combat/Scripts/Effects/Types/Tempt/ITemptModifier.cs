using Core.Combat.Scripts.Effects.Interfaces;

namespace Core.Combat.Scripts.Effects.Types.Tempt
{
    public interface ITemptModifier : IModifier
    {
        void Modify(ref TemptToApply effectStruct);
    }
}