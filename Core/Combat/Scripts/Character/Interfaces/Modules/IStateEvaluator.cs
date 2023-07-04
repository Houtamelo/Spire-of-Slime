using System.Diagnostics.Contracts;
using Core.Combat.Scripts.Enums;

namespace Core.Combat.Scripts.Interfaces.Modules
{
    public interface IStateEvaluator : IModule
    {
        [Pure] CharacterState PureEvaluate();
        [Pure] FullCharacterState FullPureEvaluate();

        (CharacterState previous, CharacterState current) OncePerTickStateEvaluation();

        bool CanBeTargeted();
        
        void OutOfForces();
    }
}