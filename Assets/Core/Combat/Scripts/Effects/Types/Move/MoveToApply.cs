using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using JetBrains.Annotations;

namespace Core.Combat.Scripts.Effects.Types.Move
{
    public record MoveToApply(CharacterStateMachine Caster, CharacterStateMachine Target, bool FromCrit, ISkill Skill, IBaseStatusScript ScriptOrigin, int ApplyChance, int MoveDelta)
        : StatusToApply(Caster, Target, FromCrit, Skill, ScriptOrigin)
    {
        public int ApplyChance { get; set; } = ApplyChance;
        public int MoveDelta { get; set; } = MoveDelta;
        
        public override StatusResult ApplyEffect() => MoveScript.ProcessModifiersAndTryApply(this);
        protected override void ProcessModifiersInternal() => MoveScript.ProcessModifiers(this);

        [NotNull]
        public override string GetDescription() => StatusToApplyDescriptions.Get(this);
        [NotNull]
        public override string GetCompactDescription() => StatusToApplyDescriptions.GetCompact(this);
    }
}