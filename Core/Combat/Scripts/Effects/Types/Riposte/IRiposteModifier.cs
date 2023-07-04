using Core.Combat.Scripts.Effects.Interfaces;

namespace Core.Combat.Scripts.Effects.Types.Riposte
{
    public interface IRiposteModifier : IModifier
    {
        void Modify(ref RiposteToApply effectStruct);
    }
}