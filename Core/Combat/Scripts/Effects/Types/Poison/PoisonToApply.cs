using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;

namespace Core.Combat.Scripts.Effects.Types.Poison
{
    public record PoisonToApply(CharacterStateMachine Caster, CharacterStateMachine Target, bool FromCrit, ISkill Skill, IBaseStatusScript ScriptOrigin, float Duration, bool IsPermanent, float ApplyChance,
        uint PoisonPerTime) : StatusToApplyDurationBased(Caster, Target, FromCrit, Skill, ScriptOrigin, Duration, IsPermanent)
    {
        public float ApplyChance { get; set; } = ApplyChance;
        public uint PoisonPerTime { get; set; } = PoisonPerTime;
        
        public override StatusResult ApplyEffect() => PoisonScript.ProcessModifiersAndTryApply(this);
        protected override void ProcessModifiersInternal() => PoisonScript.ProcessModifiers(this);

        public override string GetDescription() => StatusToApplyDescriptions.Get(this);
        public override string GetCompactDescription() => StatusToApplyDescriptions.GetCompact(this);
    }
}