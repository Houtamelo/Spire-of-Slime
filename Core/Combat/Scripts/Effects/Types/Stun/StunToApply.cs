using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;

namespace Core.Combat.Scripts.Effects.Types.Stun
{
    public record StunToApply(CharacterStateMachine Caster, CharacterStateMachine Target, bool FromCrit, ISkill Skill, IBaseStatusScript ScriptOrigin, float Duration, bool IsPermanent)
        : StatusToApplyDurationBased(Caster, Target, FromCrit, Skill, ScriptOrigin, Duration, IsPermanent)
    {
        public override StatusResult ApplyEffect() => StunScript.ProcessModifiersAndTryApply(this);
        protected override void ProcessModifiersInternal() => StunScript.ProcessModifiers(this);

        public override string GetDescription() => StatusToApplyDescriptions.Get(this);
        public override string GetCompactDescription() => StatusToApplyDescriptions.GetCompact(this);
    }
}