using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Utils.Math;
using JetBrains.Annotations;

namespace Core.Combat.Scripts.Effects.Types.Arousal
{
    public record ArousalToApply(CharacterStateMachine Caster, CharacterStateMachine Target, bool FromCrit, ISkill Skill, IBaseStatusScript ScriptOrigin, TSpan Duration, bool Permanent, int ApplyChance, int LustPerSecond)
        : StatusToApplyDurationBased(Caster, Target, FromCrit, Skill, ScriptOrigin, Duration, Permanent)
    {
        public int ApplyChance { get; set; } = ApplyChance;
        public int LustPerSecond { get; set; } = LustPerSecond;

        public override StatusResult ApplyEffect() => ArousalScript.ProcessModifiersAndTryApply(this);
        protected override void ProcessModifiersInternal() => ArousalScript.ProcessModifiers(this);

        [NotNull]
        public override string GetDescription() => StatusToApplyDescriptions.Get(this);
        [NotNull]
        public override string GetCompactDescription() => StatusToApplyDescriptions.GetCompact(this);
    }
}