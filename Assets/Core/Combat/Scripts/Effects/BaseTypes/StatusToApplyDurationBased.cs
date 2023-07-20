using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Utils.Math;

namespace Core.Combat.Scripts.Effects.BaseTypes
{
    public abstract record StatusToApplyDurationBased(CharacterStateMachine Caster, CharacterStateMachine Target, bool FromCrit, ISkill Skill, IBaseStatusScript ScriptOrigin,
                                                      TSpan Duration, bool Permanent) : StatusToApply(Caster, Target, FromCrit, Skill, ScriptOrigin)
    {
        public string GetDurationString() => Permanent ? StatusUtils.GetPermanentDurationString() : StatusUtils.GetDurationString(Duration);
        public string GetCompactDurationString() => Permanent ? StatusUtils.GetCompactPermanentDurationString() : StatusUtils.GetCompactDurationString(Duration);
        
        public TSpan Duration { get; set; } = Duration;

        public bool Permanent { get; set; } = Permanent;
    }
}