using Core.Combat.Scripts.Effects.Interfaces;

namespace Core.Combat.Scripts.Effects.Types.Heal
{
    public interface IHealModifier : IModifier
    {
        void Modify(ref HealToApply effectStruct);
    }
}