using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using JetBrains.Annotations;

namespace Core.Combat.Scripts.Effects.Types.Tempt
{
    public record TemptToApply(CharacterStateMachine Caster, CharacterStateMachine Target, bool FromCrit, ISkill Skill, IBaseStatusScript ScriptOrigin, int Power) 
        : StatusToApply(Caster, Target, FromCrit, Skill, ScriptOrigin)
    {
        public int Power { get; set; } = Power;
        
        public override StatusResult ApplyEffect() => TemptScript.ProcessModifiersAndTryApply(this);
        protected override void ProcessModifiersInternal() => TemptScript.ProcessModifiers(this);
        
        [NotNull]
        public override string GetDescription() => StatusToApplyDescriptions.Get(this);
        [NotNull]
        public override string GetCompactDescription() => StatusToApplyDescriptions.GetCompact(this);
    }
}