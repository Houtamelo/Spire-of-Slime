using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Utils.Math;
using JetBrains.Annotations;

namespace Core.Combat.Scripts.Effects.Types.OvertimeHeal
{
    public record OvertimeHealToApply(CharacterStateMachine Caster, CharacterStateMachine Target, bool FromCrit, ISkill Skill,
                                      IBaseStatusScript ScriptOrigin, TSpan Duration, bool Permanent, int HealPerSecond)
        : StatusToApplyDurationBased(Caster, Target, FromCrit, Skill, ScriptOrigin, Duration, Permanent)
    {
        public int HealPerSecond { get; set; } = HealPerSecond;
        
        public override StatusResult ApplyEffect() => OvertimeHealScript.ProcessModifiersAndTryApply(this);
        protected override void ProcessModifiersInternal() => OvertimeHealScript.ProcessModifiers(this);

        [NotNull]
        public override string GetDescription() => StatusToApplyDescriptions.Get(this);
        [NotNull]
        public override string GetCompactDescription() => StatusToApplyDescriptions.GetCompact(this);
    }
}