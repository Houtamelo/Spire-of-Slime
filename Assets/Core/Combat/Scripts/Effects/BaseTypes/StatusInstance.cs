using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Timeline;
using Core.Utils.Collections;
using Core.Utils.Math;
using Core.Utils.Patterns;
using JetBrains.Annotations;
using UnityEngine;

namespace Core.Combat.Scripts.Effects.BaseTypes
{
    public abstract class StatusInstance
    {
        public CharacterStateMachine Owner { get; }
        
        public bool IsDeactivated { get; private set; }
        public bool IsActive => IsDeactivated == false;

        public bool Permanent { get; protected set; }
        private TSpan _duration;
        public TSpan Duration
        {
            get => _duration;
            set
            {
                if (Permanent || IsDeactivated)
                    return;
                
                _duration = value;
                if (_duration.Ticks <= 0)
                    RequestDeactivation();
            }
        }

        protected StatusInstance(TSpan duration, bool permanent, CharacterStateMachine owner)
        {
            Owner = owner;
            Permanent = permanent;
            Duration = duration;
        }

        protected StatusInstance([NotNull] StatusRecord record, CharacterStateMachine owner)
        {
            Owner = owner;
            Permanent = record.Permanent;
            Duration = record.Duration;
        }
        
        public virtual void Tick(TSpan timeStep)
        {
            Duration -= timeStep;
        }

        public virtual void RequestDeactivation()
        {
            if (IsDeactivated)
                return;
            
            IsDeactivated = true;
            Owner.StatusReceiverModule.RemoveStatus(effectInstance: this);
        }

        public virtual void CharacterDefeated(CharacterStateMachine character, bool becomesCorpseOnDefeat)
        {
            if (character == Owner)
                RequestDeactivation();
        }

        public virtual void FillTimelineEvents(SelfSortingList<CombatEvent> events)
        {
            Debug.Assert(IsActive, message: "Trying to fill timeline events for inactive status");
            
            if (Permanent == false && Duration.Ticks > 0)
                events.Add(CombatEvent.FromStatusEnd(Owner, Duration, status: this));
        }
        
        public int EffectId => CombatUtils.GetEffectId(effect: this);
        public abstract EffectType EffectType { get; }
        public abstract Option<string> GetDescription();

        public abstract bool IsPositive { get; }
        public bool IsNegative => IsPositive == false;

        public string GetDurationString() => Permanent ? StatusUtils.GetPermanentDurationString() : StatusUtils.GetDurationString(Duration);
        public string GetCompactDurationString() => Permanent ? StatusUtils.GetCompactPermanentDurationString() : StatusUtils.GetCompactDurationString(Duration);
        
        public abstract StatusRecord GetRecord();
    }
}