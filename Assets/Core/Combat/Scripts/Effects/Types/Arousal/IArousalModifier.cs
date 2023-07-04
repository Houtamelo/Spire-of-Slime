using Core.Combat.Scripts.Effects.Interfaces;

namespace Core.Combat.Scripts.Effects.Types.Arousal
{
    public interface IArousalModifier : IModifier
    {
        void Modify(ref ArousalToApply effectStruct);
    }
}