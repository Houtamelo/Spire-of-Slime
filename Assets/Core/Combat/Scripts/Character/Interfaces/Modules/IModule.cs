using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Enums;

namespace Core.Combat.Scripts.Interfaces.Modules
{
    public interface IModule
    {
        // CharacterStates are "in" so the inheritor is aware that they are readonly
        
        /// <summary> Caller ensures that a display exists and Display is unlocked. </summary>
        void AfterTickUpdate(in float timeStep, in CharacterState previousState, in CharacterState currentState)
        {
            // can be overriden
        }

        void ForceUpdateDisplay(in CharacterDisplay display)
        {
            // can be overriden
        }
    }
}