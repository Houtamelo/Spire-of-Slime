using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;

namespace Core.Combat.Scripts.Effects.Types.Arousal
{
    public record ArousalToApply(CharacterStateMachine Caster, CharacterStateMachine Target, bool FromCrit, ISkill Skill,
                                IBaseStatusScript ScriptOrigin, float Duration, bool IsPermanent, float ApplyChance, uint LustPerTime)
        : StatusToApplyDurationBased(Caster, Target, FromCrit, Skill, ScriptOrigin, Duration, IsPermanent)
    {
        public float ApplyChance { get; set; } = ApplyChance;
        public uint LustPerTime { get; set; } = LustPerTime;

        public override StatusResult ApplyEffect() => ArousalScript.ProcessModifiersAndTryApply(this);
        protected override void ProcessModifiersInternal() => ArousalScript.ProcessModifiers(this);

        public override string GetDescription() => StatusToApplyDescriptions.Get(this);
        public override string GetCompactDescription() => StatusToApplyDescriptions.GetCompact(this);
    }
}