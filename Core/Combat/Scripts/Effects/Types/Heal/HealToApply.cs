using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;

namespace Core.Combat.Scripts.Effects.Types.Heal
{
    public record HealToApply(CharacterStateMachine Caster, CharacterStateMachine Target, bool FromCrit, ISkill Skill, IBaseStatusScript ScriptOrigin, float Power)
        : StatusToApply(Caster, Target, FromCrit, Skill, ScriptOrigin)
    {
        public float Power { get; set; } = Power;
        
        public override StatusResult ApplyEffect() => HealScript.ProcessModifiersAndTryApply(this);
        protected override void ProcessModifiersInternal() => HealScript.ProcessModifiers(this);

        public override string GetDescription() => StatusToApplyDescriptions.Get(this);
        public override string GetCompactDescription() => StatusToApplyDescriptions.GetCompact(this);
    }
}