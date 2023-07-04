using Core.Combat.Scripts.Effects.Interfaces;

namespace Core.Combat.Scripts.Effects.Types.Grappled
{
    public interface ILustGrappledModifier : IModifier
    {
        void Modify(ref LustGrappledToApply effectStruct);
    }
}