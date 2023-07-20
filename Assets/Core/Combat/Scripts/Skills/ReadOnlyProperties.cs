using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Utils.Patterns;
using JetBrains.Annotations;

namespace Core.Combat.Scripts.Skills
{
    public readonly struct ReadOnlyProperties
    {
        public readonly CharacterStateMachine Target;
        public readonly Option<int> AccuracyModifier;
        public readonly Option<int> Power;
        public readonly Option<int> CriticalChanceModifier;
        public readonly Option<int> ResiliencePiercingModifier;
        
        public ReadOnlyProperties(CharacterStateMachine target, [NotNull] ISkill skill)
        {
            Target = target;
            AccuracyModifier = skill.Accuracy;
            Power = skill.Power;
            CriticalChanceModifier = skill.CriticalChance;
            ResiliencePiercingModifier = skill.ResilienceReduction;
        }

        public ReadOnlyProperties(TargetProperties targetProperties)
        {
            Target = targetProperties.Target;
            AccuracyModifier = targetProperties.AccuracyModifier;
            Power = targetProperties.Power;
            CriticalChanceModifier = targetProperties.CriticalChanceModifier;
            ResiliencePiercingModifier = targetProperties.ResilienceReductionModifier;
        }
    }
}