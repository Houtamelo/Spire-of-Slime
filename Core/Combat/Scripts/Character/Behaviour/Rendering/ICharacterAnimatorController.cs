using Core.Combat.Scripts.Animations;

namespace Core.Combat.Scripts.Behaviour.Rendering
{
    public interface ICharacterAnimatorController
    {
        void SetAnimation(in CombatAnimation combatAnimation);
    }
}