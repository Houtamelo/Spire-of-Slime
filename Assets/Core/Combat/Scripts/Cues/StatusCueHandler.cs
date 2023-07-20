using System;
using System.Collections.Generic;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects;
using Core.Combat.Scripts.Enums;
using JetBrains.Annotations;
using UnityEngine;

namespace Core.Combat.Scripts.Cues
{
    public class StatusCueHandler : IEquatable<StatusCueHandler>
    {
        public static readonly Predicate<CharacterStateMachine> NoValidator = _ => true;

        public static readonly Predicate<CharacterStateMachine> StandardValidator = character =>
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
        
        public readonly CharacterStateMachine Character;
        private readonly Predicate<CharacterStateMachine> _validator;
        public CueType Type { get; init; }

        private EffectType EffectType { get; init; }
        public EffectType GetEffectType() => Type == CueType.EffectApplied ? EffectType : throw new InvalidOperationException($"Cannot get effect type from: {Type} cue");

        private bool Success { get; init; }
        public bool GetSuccess() => Type == CueType.EffectApplied ? Success : throw new InvalidOperationException($"Cannot get success from: {Type} cue");

        private int PoisonAmount { get; init; }
        public int GetPoisonAmount() => Type is CueType.PoisonTick ? PoisonAmount : throw new InvalidOperationException($"Cannot get poison amount from: {Type} cue");
        
        private int LustDelta { get; init; }
        public int GetLustDelta() => Type is CueType.LustTick ? LustDelta : throw new InvalidOperationException($"Cannot get lust delta from: {Type} cue");
        
        private int TemptationAmount { get; init; }
        public int GetTemptationAmount() => Type == CueType.TemptationTick ? TemptationAmount : throw new InvalidOperationException($"Cannot get temptation amount from: {Type} cue");

        public event Action Started;
        private bool _started;

        private StatusCueHandler(CharacterStateMachine character, Predicate<CharacterStateMachine> validator)
        {
            Character = character;
            _validator = validator;
        }

        [NotNull]
        public static StatusCueHandler FromAppliedStatus(CharacterStateMachine character, Predicate<CharacterStateMachine> validator, EffectType effectType, bool success) 
            => new(character, validator) { Type = CueType.EffectApplied, EffectType = effectType, Success = success };
        
        [NotNull]
        public static StatusCueHandler FromPoisonTick(CharacterStateMachine character, Predicate<CharacterStateMachine> validator, int amount) 
            => new(character, validator) { Type = CueType.PoisonTick, PoisonAmount = amount };
        
        [NotNull]
        public static StatusCueHandler FromLustTick(CharacterStateMachine character, Predicate<CharacterStateMachine> validator, int delta)
            => new(character, validator) { Type = CueType.LustTick, LustDelta = delta };
        
        [NotNull]
        public static StatusCueHandler FromTemptationTick(CharacterStateMachine character, Predicate<CharacterStateMachine> validator, int amount)
            => new(character, validator) { Type = CueType.TemptationTick, TemptationAmount = amount };

        public bool IsValid() => Character != null && _validator(Character);

        public void NotifyStart()
        {
            if (_started)
            {
                Debug.LogWarning("Status cue already started, should not notify twice.");
                return;
            }
            
            _started = true;
            Started?.Invoke();
        }

        public bool CanGroupWith([NotNull] StatusCueHandler other)
        {
            if (Type != other.Type)
                return false;

            return Type switch
            {
                CueType.EffectApplied  => EffectType == other.EffectType && Success == other.Success,
                CueType.PoisonTick     => true,
                CueType.LustTick       => true,
                CueType.TemptationTick => true,
                _                      => throw new ArgumentOutOfRangeException(nameof(Type), $"Unhandled cue type: {Type}")
            };
        }

        public bool Equals(StatusCueHandler other) => EqualityComparer<StatusCueHandler>.Default.Equals(this, other);

        public enum CueType
        {
            EffectApplied,
            PoisonTick,
            LustTick,
            TemptationTick
        }
    }
}