using Core.Combat.Scripts.Effects.Interfaces;

namespace Core.Combat.Scripts.Effects.Types.Summon
{
    public interface ISummonModifier : IModifier
    {
        void Modify(ref SummonToApply effectStruct);
    }
}