using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Utils.Math;
using JetBrains.Annotations;

namespace Core.Combat.Scripts.Effects.Types.Poison
{
    public record PoisonToApply(CharacterStateMachine Caster, CharacterStateMachine Target, bool FromCrit, ISkill Skill, IBaseStatusScript ScriptOrigin, TSpan Duration,
                                bool Permanent, int ApplyChance, int PoisonPerSecond) : StatusToApplyDurationBased(Caster, Target, FromCrit, Skill, ScriptOrigin, Duration, Permanent)
    {
        public int ApplyChance { get; set; } = ApplyChance;
        public int PoisonPerSecond { get; set; } = PoisonPerSecond;
        
        public override StatusResult ApplyEffect() => PoisonScript.ProcessModifiersAndTryApply(this);
        protected override void ProcessModifiersInternal() => PoisonScript.ProcessModifiers(this);

        [NotNull]
        public override string GetDescription() => StatusToApplyDescriptions.Get(this);
        [NotNull]
        public override string GetCompactDescription() => StatusToApplyDescriptions.GetCompact(this);
    }
}