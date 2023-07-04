using Core.Combat.Scripts.Effects.Interfaces;

namespace Core.Combat.Scripts.Effects.Types.OvertimeHeal
{
    public interface IOvertimeHealModifier : IModifier
    {
        void Modify(ref OvertimeHealToApply effectStruct);
    }
}