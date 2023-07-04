using Core.Combat.Scripts.Effects.Interfaces;

namespace Core.Combat.Scripts.Skills.Interfaces
{
    public interface IChargeModifier : IModifier
    {
        void Modify(ref ChargeStruct chargeStruct);
    }
}