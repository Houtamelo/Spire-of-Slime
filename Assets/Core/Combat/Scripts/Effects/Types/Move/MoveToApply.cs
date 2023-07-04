using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;

namespace Core.Combat.Scripts.Effects.Types.Move
{
    public record MoveToApply(CharacterStateMachine Caster, CharacterStateMachine Target, bool FromCrit, ISkill Skill, IBaseStatusScript ScriptOrigin, float ApplyChance, int MoveDelta)
        : StatusToApply(Caster, Target, FromCrit, Skill, ScriptOrigin)
    {
        public float ApplyChance { get; set; } = ApplyChance;
        public int MoveDelta { get; set; } = MoveDelta;
        
        public override StatusResult ApplyEffect() => MoveScript.ProcessModifiersAndTryApply(this);
        protected override void ProcessModifiersInternal() => MoveScript.ProcessModifiers(this);

        public override string GetDescription() => StatusToApplyDescriptions.Get(this);
        public override string GetCompactDescription() => StatusToApplyDescriptions.GetCompact(this);
    }
}