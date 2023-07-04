using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Skills.Interfaces;

namespace Core.Combat.Scripts.Effects.BaseTypes
{
    public abstract record StatusToApplyDurationBased(CharacterStateMachine Caster, CharacterStateMachine Target, bool FromCrit, ISkill Skill, IBaseStatusScript ScriptOrigin,
                                                      float Duration, bool IsPermanent) : StatusToApply(Caster, Target, FromCrit, Skill, ScriptOrigin)
    {
        public string GetDurationString() => IsPermanent ? "permanent" : $"for {Duration.ToString("0.00")}s";
        public string GetCompactDurationString() => IsPermanent ? "permanent" : $"{Duration.ToString("0.0")}s";
        public float Duration { get; set; } = Duration;
        public bool IsPermanent { get; set; } = IsPermanent;
    }
}