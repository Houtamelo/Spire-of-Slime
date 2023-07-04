using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.Interfaces;

namespace Core.Combat.Scripts.Interfaces
{
    public interface IChangeMark : IModifier
    {
        void ChangeMultiplierTargetChance(CharacterStateMachine caster, CharacterStateMachine target, ref float chance);
        void ChangeMaxTargetChance(CharacterStateMachine caster, CharacterStateMachine target, ref float chance);
        void ChangeMinTargetChance(CharacterStateMachine caster, CharacterStateMachine target, ref float chance);
    }
}