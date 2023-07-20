using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Utils.Math;
using JetBrains.Annotations;

namespace Core.Combat.Scripts.Effects.Types.Stun
{
    public record StunToApply(CharacterStateMachine Caster, CharacterStateMachine Target, bool FromCrit, ISkill Skill, IBaseStatusScript ScriptOrigin, int StunPower, bool Permanent)
        : StatusToApply(Caster, Target, FromCrit, Skill, ScriptOrigin)
    {
        public int StunPower { set; get; } = StunPower;
        
        public override StatusResult ApplyEffect() => StunScript.ProcessModifiersAndTryApply(this);
        protected override void ProcessModifiersInternal() => StunScript.ProcessModifiers(this);

        [NotNull]
        public override string GetDescription() => StatusToApplyDescriptions.Get(this);
        [NotNull]
        public override string GetCompactDescription() => StatusToApplyDescriptions.GetCompact(this);
    }
}