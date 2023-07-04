using Core.Combat.Scripts.Effects.Interfaces;

namespace Core.Combat.Scripts.Effects.Types.Stun
{
    public interface IStunModifier : IModifier
    {
        void Modify(ref StunToApply effectStruct);
    }
}