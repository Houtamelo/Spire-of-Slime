using System;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Enums;
using Core.Utils.Async;
using Core.Utils.Patterns;
using JetBrains.Annotations;

namespace Core.Combat.Scripts.Animations
{
    public class AnimationRoutineInfo
    {
        public static readonly Predicate<CharacterStateMachine> StandardValidation = character =>
        {
            if (character == null)
                return false;

            if (character.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Grappled)
                return false;

            if (character.Display.TrySome(out DisplayModule display) == false)
                return false;

            if (display.AnimationStatus is AnimationStatus.Defeated or AnimationStatus.Grappled)
                return false;
            
            return true;
        };
        public static readonly Predicate<CharacterStateMachine> NoValidation = _ => true;

        private readonly CoroutineWrapper _coroutine;
        private readonly Option<CharacterStateMachine> _character;
        private Predicate<CharacterStateMachine> Validation { get; init; }

        public bool IsFinished => _coroutine.IsFinished;
        public bool HasStarted => _coroutine.HasStarted;

        private AnimationRoutineInfo(CoroutineWrapper coroutine, Option<CharacterStateMachine> character)
        {
            _coroutine = coroutine;
            _character = character;
        }

        [NotNull]
        public static AnimationRoutineInfo WithoutCharacter(CoroutineWrapper coroutine) => new(coroutine, Option<CharacterStateMachine>.None) {Validation = NoValidation};
        
        [NotNull]
        public static AnimationRoutineInfo WithCharacter(CoroutineWrapper coroutine, CharacterStateMachine character, Predicate<CharacterStateMachine> validation)
            => new(coroutine, Option<CharacterStateMachine>.Some(character)) { Validation = validation };

        public bool StartIfValid()
        {
            if (_character.IsSome && Validation(_character.Value) == false)
                return false;
            
            if (_coroutine.HasStarted || _coroutine.IsFinished)
                return true;

            _coroutine.Start();
            return true;
        }
        
        public void ForceFinish() => _coroutine.ForceFinish();
    }
}