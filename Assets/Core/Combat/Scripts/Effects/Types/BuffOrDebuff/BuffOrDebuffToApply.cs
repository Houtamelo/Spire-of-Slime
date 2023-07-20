using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Utils.Math;
using JetBrains.Annotations;

namespace Core.Combat.Scripts.Effects.Types.BuffOrDebuff
{
    public record BuffOrDebuffToApply(CharacterStateMachine Caster, CharacterStateMachine Target, bool FromCrit, ISkill Skill,
                                      IBaseStatusScript ScriptOrigin,TSpan Duration,bool Permanent, int ApplyChance, CombatStat Stat, int Delta)
        : StatusToApplyDurationBased(Caster, Target, FromCrit, Skill, ScriptOrigin, Duration, Permanent)
    {
        public int ApplyChance { get; set; } = ApplyChance;
        public CombatStat Stat { get; set; } = Stat;
        public int Delta { get; set; } = Delta;
        
        public bool IsPositive => Delta > 0;
        
        public override StatusResult ApplyEffect() => BuffOrDebuffScript.ProcessModifiersAndTryApply(this);
        protected override void ProcessModifiersInternal() => BuffOrDebuffScript.ProcessModifiers(this);
        
        [NotNull]
        public override string GetDescription() => StatusToApplyDescriptions.Get(this);
        [NotNull]
        public override string GetCompactDescription() => StatusToApplyDescriptions.GetCompact(this);
    }
}