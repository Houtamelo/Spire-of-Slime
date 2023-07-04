using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;

namespace Core.Combat.Scripts.Effects.Types.BuffOrDebuff
{
    public record BuffOrDebuffToApply(CharacterStateMachine Caster, CharacterStateMachine Target, bool FromCrit, ISkill Skill, IBaseStatusScript ScriptOrigin, float Duration, bool IsPermanent,
                                     float ApplyChance, CombatStat Stat, float Delta) : StatusToApplyDurationBased(Caster, Target, FromCrit, Skill, ScriptOrigin, Duration, IsPermanent)
    {
        public float ApplyChance { get; set; } = ApplyChance;
        public CombatStat Stat { get; set; } = Stat;
        public float Delta { get; set; } = Delta;
        
        public bool IsPositive => Delta > 0;
        
        public override StatusResult ApplyEffect() => BuffOrDebuffScript.ProcessModifiersAndTryApply(this);
        protected override void ProcessModifiersInternal() => BuffOrDebuffScript.ProcessModifiers(this);
        
        public override string GetDescription() => StatusToApplyDescriptions.Get(this);
        public override string GetCompactDescription() => StatusToApplyDescriptions.GetCompact(this);
    }
}