using System;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Utils.Patterns;
using JetBrains.Annotations;

namespace Core.Combat.Scripts.Skills
{
    public struct TargetProperties : IEquatable<TargetProperties>
    {
        public CharacterStateMachine Target { get; }
        public Option<int> AccuracyModifier;
        public Option<int> Power;
        public Option<int> CriticalChanceModifier;
        public Option<int> ResilienceReductionModifier;

        public TargetProperties(CharacterStateMachine target, [NotNull] ISkill skill)
        {
            Target = target;
            AccuracyModifier = skill.Accuracy;
            Power = skill.Power;
            CriticalChanceModifier = skill.CriticalChance;
            ResilienceReductionModifier = skill.ResilienceReduction;
        }
        
        [System.Diagnostics.Contracts.Pure]
        public ReadOnlyProperties ToReadOnly() => new(this);

        public bool Equals(TargetProperties other) =>
            Equals(Target, other.Target)
         && AccuracyModifier.Equals(other.AccuracyModifier)
         && Power.Equals(other.Power)
         && CriticalChanceModifier.Equals(other.CriticalChanceModifier)
         && ResilienceReductionModifier.Equals(other.ResilienceReductionModifier);

        public override bool Equals(object obj) => obj is TargetProperties other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (Target != null ? Target.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ AccuracyModifier.GetHashCode();
                hashCode = (hashCode * 397) ^ Power.GetHashCode();
                hashCode = (hashCode * 397) ^ CriticalChanceModifier.GetHashCode();
                hashCode = (hashCode * 397) ^ ResilienceReductionModifier.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(TargetProperties left, TargetProperties right) => left.Equals(right);
        public static bool operator !=(TargetProperties left, TargetProperties right) => left.Equals(right) == false;
        public static implicit operator ReadOnlyProperties(TargetProperties targetProperties) => targetProperties.ToReadOnly();
    }
}