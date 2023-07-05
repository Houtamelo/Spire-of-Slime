using Core.Combat.Scripts.Behaviour;
using Core.Utils.Collections;

namespace Core.Combat.Scripts.Interfaces.Modules
{
    public interface IAIModule : IModule
    {
        float GetMarkedMultiplier(CharacterStateMachine caster);
        float GetMaxChanceToBeTargetedWhenMarked(CharacterStateMachine caster);
        float GetMinChanceToBeTargetedWhenMarked(CharacterStateMachine caster);
        void Heuristic();
        SelfSortingList<IChangeMark> MarkModifiers { get; }
    }
}