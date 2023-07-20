using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using JetBrains.Annotations;

namespace Core.Combat.Scripts.Effects.Types.Lust
{
    public record LustToApply(CharacterStateMachine Caster, CharacterStateMachine Target, bool FromCrit, ISkill Skill, IBaseStatusScript ScriptOrigin, int LustLower, int LustUpper, int LustPower) 
        : StatusToApply(Caster, Target, FromCrit, Skill, ScriptOrigin)
    {
        public int LustLower { get; set; } = LustLower;
        public int LustUpper { get; set; } = LustUpper;
        public int LustPower { get; set; } = LustPower;
        
        public override StatusResult ApplyEffect() => LustScript.ProcessModifiersAndTryApply(this);
        protected override void ProcessModifiersInternal() => LustScript.ProcessModifiers(this);

        [NotNull]
        public override string GetDescription() => StatusToApplyDescriptions.Get(this);
        [NotNull]
        public override string GetCompactDescription() => StatusToApplyDescriptions.GetCompact(this);
    }
}