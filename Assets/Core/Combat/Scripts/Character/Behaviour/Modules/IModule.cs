using Core.Combat.Scripts.Enums;
using Core.Utils.Math;

namespace Core.Combat.Scripts.Behaviour.Modules
{
    public interface IModule
    {
        // CharacterStates are "in" so the inheritor is aware that they are readonly
        
        /// <summary> Caller ensures that a display exists and Display is unlocked. </summary>
        void AfterTickUpdate(in TSpan timeStep, in CharacterState previousState, in CharacterState currentState)
        {
            // can be overriden
        }

        void ForceUpdateDisplay(in DisplayModule display)
        {
            // can be overriden
        }
    }
}