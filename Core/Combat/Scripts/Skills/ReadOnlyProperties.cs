using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Skills.Interfaces;
using Utils.Patterns;

namespace Core.Combat.Scripts.Skills
{
    public readonly struct ReadOnlyProperties
    {
        public readonly CharacterStateMachine Target;
        public readonly Option<float> AccuracyModifier;
        public readonly Option<float> DamageModifier;
        public readonly Option<float> CriticalChanceModifier;
        public readonly Option<float> ResiliencePiercingModifier;
        
        public ReadOnlyProperties(CharacterStateMachine target, ISkill skill)
        {
            Target = target;
            AccuracyModifier = skill.BaseAccuracy;
            DamageModifier = skill.BaseDamageMultiplier;
            CriticalChanceModifier = skill.BaseCriticalChance;
            ResiliencePiercingModifier = skill.BaseResiliencePiercing;
        }

        public ReadOnlyProperties(TargetProperties targetProperties)
        {
            Target = targetProperties.Target;
            AccuracyModifier = targetProperties.AccuracyModifier;
            DamageModifier = targetProperties.DamageModifier;
            CriticalChanceModifier = targetProperties.CriticalChanceModifier;
            ResiliencePiercingModifier = targetProperties.ResiliencePiercingModifier;
        }
    }
}