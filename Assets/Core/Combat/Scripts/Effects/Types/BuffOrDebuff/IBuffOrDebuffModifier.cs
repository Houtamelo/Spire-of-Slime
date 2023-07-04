using Core.Combat.Scripts.Effects.Interfaces;

namespace Core.Combat.Scripts.Effects.Types.BuffOrDebuff
{
    public interface IBuffOrDebuffModifier : IModifier
    {
        void Modify(ref BuffOrDebuffToApply effectStruct);
    }
}