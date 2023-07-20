using System;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Utils.Patterns;
using UnityEngine;

namespace Core.Combat.Scripts.Skills.Action
{
    public readonly struct StatusResult : IEquatable<StatusResult>
    {
        public readonly CharacterStateMachine Caster;
        public readonly CharacterStateMachine Target;
        public readonly bool IsSuccess;
        public bool IsFailure => !IsSuccess;
        private readonly StatusInstance _statusInstance;
        public Option<StatusInstance> StatusInstance => _generatesInstance && IsSuccess ? _statusInstance : Option<StatusInstance>.None;
        private readonly bool _generatesInstance;
        public readonly EffectType EffectType;

        public StatusResult(CharacterStateMachine caster, CharacterStateMachine target, bool success, StatusInstance statusInstance, bool generatesInstance, EffectType effectType)
        {
            Debug.Assert(!generatesInstance || (success && statusInstance != null) || !success);
            
            Caster = caster;
            Target = target;
            IsSuccess = success;
            _statusInstance = statusInstance;
            _generatesInstance = generatesInstance;
            EffectType = effectType;
        }
        
        public static StatusResult Failure(CharacterStateMachine caster, CharacterStateMachine target, bool generatesInstance) => new(caster, target, success: false, statusInstance: null, generatesInstance: generatesInstance, default);
        public static StatusResult Success(CharacterStateMachine caster, CharacterStateMachine target, StatusInstance statusInstance, bool generatesInstance, EffectType effectType) 
            => new(caster, target, success: true, statusInstance: statusInstance, generatesInstance: generatesInstance, effectType);

        public bool Equals(StatusResult other) =>
            Equals(Caster, other.Caster) && Equals(Target, other.Target) && IsSuccess == other.IsSuccess && Equals(_statusInstance, other._statusInstance)
         && _generatesInstance == other._generatesInstance && EffectType == other.EffectType;

        public override bool Equals(object obj) => obj is StatusResult other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Caster != null ? Caster.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Target != null ? Target.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ IsSuccess.GetHashCode();
                hashCode = (hashCode * 397) ^ (_statusInstance != null ? _statusInstance.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ _generatesInstance.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)EffectType;
                return hashCode;
            }
        }

        public static bool operator ==(StatusResult left, StatusResult right) => left.Equals(right);

        public static bool operator !=(StatusResult left, StatusResult right) => !left.Equals(right);
    }
}