using Core.Combat.Scripts.Behaviour;
using Core.Utils.Math;

namespace Core.Combat.Scripts.Timeline
{
    public readonly struct TimelineData
    {
        public readonly CharacterStateMachine Owner;
        public readonly TSpan MaxTime;
        public readonly string Description;
        public readonly CombatEvent.Type EventType;
        private readonly int _hashCode;
            
        public TimelineData(CharacterStateMachine owner, TSpan maxTime, string description, int hashCode, CombatEvent.Type eventType)
        {
            Owner = owner;
            MaxTime = maxTime;
            Description = description;
            _hashCode = hashCode;
            EventType = eventType;
        }

        public override int GetHashCode() => _hashCode;
    }
}