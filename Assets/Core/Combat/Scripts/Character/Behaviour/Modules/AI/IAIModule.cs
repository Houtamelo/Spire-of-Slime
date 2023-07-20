using Core.Combat.Scripts.Interfaces;
using Core.Utils.Collections;

namespace Core.Combat.Scripts.Behaviour.Modules
{
    public abstract record AIRecord : ModuleRecord
    {
        public abstract IAIModule Deserialize(CharacterStateMachine owner);
    }
    
    public interface IAIModule : IModule
    {
        float GetMarkedMultiplier(CharacterStateMachine caster);
        float GetMaxChanceToBeTargetedWhenMarked(CharacterStateMachine caster);
        float GetMinChanceToBeTargetedWhenMarked(CharacterStateMachine caster);
        SelfSortingList<IChangeMark> MarkModifiers { get; }

        void Heuristic();
        
        AIRecord GetRecord();
    }
}