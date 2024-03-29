﻿using System.Diagnostics.Contracts;
using Core.Combat.Scripts.Enums;

namespace Core.Combat.Scripts.Behaviour.Modules
{
    public abstract record StateEvaluatorRecord : ModuleRecord
    {
        public abstract IStateEvaluator Deserialize(CharacterStateMachine owner);
    }
    
    public interface IStateEvaluator : IModule
    {
        [Pure] CharacterState PureEvaluate();
        [Pure] FullCharacterState FullPureEvaluate();

        (CharacterState previous, CharacterState current) OncePerTickStateEvaluation();

        bool CanBeTargeted();
        
        void OutOfForces();

        StateEvaluatorRecord GetRecord();
    }
}