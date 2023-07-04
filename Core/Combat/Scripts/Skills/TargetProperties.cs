using System;
using System.Diagnostics.Contracts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Skills.Interfaces;
using Utils.Patterns;

namespace Core.Combat.Scripts.Skills
{
    public struct TargetProperties : IEquatable<TargetProperties>
    {
        public CharacterStateMachine Target;
        public Option<float> AccuracyModifier;
        public Option<float> DamageModifier;
        public Option<float> CriticalChanceModifier;
        public Option<float> ResiliencePiercingModifier;

        public TargetProperties(CharacterStateMachine target, ISkill skill)
        {
            Target = target;
            AccuracyModifier = skill.BaseAccuracy;
            DamageModifier = skill.BaseDamageMultiplier;
            CriticalChanceModifier = skill.BaseCriticalChance;
            ResiliencePiercingModifier = skill.BaseResiliencePiercing;
        }
        
        [Pure]
        public ReadOnlyProperties ToReadOnly() => new(this);

        public bool Equals(TargetProperties other)
        {
            return Equals(Target, other.Target)
                   && AccuracyModifier.Equals(other.AccuracyModifier)
                   && DamageModifier.Equals(other.DamageModifier)
                   && CriticalChanceModifier.Equals(other.CriticalChanceModifier)
                   && ResiliencePiercingModifier.Equals(other.ResiliencePiercingModifier);
        }

        public override bool Equals(object obj)
        {
            return obj is TargetProperties other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (Target != null ? Target.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ AccuracyModifier.GetHashCode();
                hashCode = (hashCode * 397) ^ DamageModifier.GetHashCode();
                hashCode = (hashCode * 397) ^ CriticalChanceModifier.GetHashCode();
                hashCode = (hashCode * 397) ^ ResiliencePiercingModifier.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(TargetProperties left, TargetProperties right) { return left.Equals(right); }
        public static bool operator !=(TargetProperties left, TargetProperties right) { return !left.Equals(right); }
        public static implicit operator ReadOnlyProperties(TargetProperties targetProperties) { return targetProperties.ToReadOnly(); }
    }
}