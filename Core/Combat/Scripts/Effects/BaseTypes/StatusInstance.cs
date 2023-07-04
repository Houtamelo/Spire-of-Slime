using System;
using System.Collections.Generic;
using Core.Combat.Scripts.Behaviour;
using Utils.Patterns;

namespace Core.Combat.Scripts.Effects.BaseTypes
{
    public abstract class StatusInstance : IEquatable<StatusInstance>
    {
        public bool Equals(StatusInstance other) => EqualityComparer<StatusInstance>.Default.Equals(this, other);

        public CharacterStateMachine Owner { get; private set; }
        public abstract EffectType EffectType { get; }
        public abstract bool IsPositive { get; }
        public bool IsNegative => !IsPositive;
        
        public bool IsPermanent { get; protected set; }
        private float _duration;
        public bool IsDeactivated { get; private set; }
        public bool IsActive => !IsDeactivated;
        public int EffectId => CombatUtils.GetEffectId(effect: this);
        public abstract StatusRecord GetRecord();
        public abstract Option<string> GetDescription();

        public float Duration
        {
            get => _duration;
            set
            {
                if (IsPermanent)
                    return;
                
                _duration = value;
                if (_duration < 0)
                    RequestDeactivation();
            }
        }

        protected StatusInstance(float duration, bool isPermanent, CharacterStateMachine owner)
        {
            Owner = owner;
            IsPermanent = isPermanent;
            Duration = duration;
        }

        protected StatusInstance(StatusRecord record, CharacterStateMachine owner)
        {
            Owner = owner;
            IsPermanent = record.IsPermanent;
            Duration = record.Duration;
        }

        public string GetDurationString() => IsPermanent ? "Permanent" : $"{Duration.ToString(format: "0.00")}s";
        
        public string GetCompactDurationString() => IsPermanent ? "permanent" : $"{Duration.ToString(format: "0.0")}s";
        
        public virtual void Tick(float timeStep)
        {
            Duration -= timeStep;
        }

        public virtual void RequestDeactivation()
        {
            if (IsDeactivated)
                return;
            
            IsDeactivated = true;
            Owner.StatusModule.RemoveStatus(effectInstance: this);
        }

        public virtual void CharacterDefeated(CharacterStateMachine character, bool becomesCorpseOnDefeat)
        {
            if (character == Owner)
                RequestDeactivation();
        }
    }
}