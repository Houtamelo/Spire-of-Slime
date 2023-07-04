using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;

namespace Core.Combat.Scripts.Effects.Types.Lust
{
    public record LustToApply(CharacterStateMachine Caster, CharacterStateMachine Target, bool FromCrit, ISkill Skill, IBaseStatusScript ScriptOrigin, int LustLower, int LustUpper) 
        : StatusToApply(Caster, Target, FromCrit, Skill, ScriptOrigin)
    {
        public int LustLower { get; set; } = LustLower;
        public int LustUpper { get; set; } = LustUpper;
        public float Multiplier { get; set; } = 1;
        
        public override StatusResult ApplyEffect() => LustScript.ProcessModifiersAndTryApply(this);
        protected override void ProcessModifiersInternal() => LustScript.ProcessModifiers(this);

        public override string GetDescription() => StatusToApplyDescriptions.Get(this);
        public override string GetCompactDescription() => StatusToApplyDescriptions.GetCompact(this);
    }
}