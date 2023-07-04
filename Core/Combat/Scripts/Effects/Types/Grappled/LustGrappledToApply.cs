using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;

namespace Core.Combat.Scripts.Effects.Types.Grappled
{
    public record LustGrappledToApply(CharacterStateMachine Caster, CharacterStateMachine Target, bool FromCrit, ISkill Skill, IBaseStatusScript ScriptOrigin, float Duration, bool IsPermanent,
                                     string TriggerName, float GraphicalX, uint LustPerTime, float TemptationDeltaPerTime) : StatusToApplyDurationBased(Caster, Target, FromCrit, Skill, ScriptOrigin, Duration, IsPermanent)
    {
        public string TriggerName { get; set; } = TriggerName;
        public float GraphicalX { get; set; } = GraphicalX;
        public uint LustPerTime { get; set; } = LustPerTime;
        public float TemptationDeltaPerTime { get; set; } = TemptationDeltaPerTime;
        
        public override StatusResult ApplyEffect() => LustGrappledScript.ProcessModifiersAndTryApply(this);
        protected override void ProcessModifiersInternal() => LustGrappledScript.ProcessModifiers(this);
        
        public override string GetDescription() => StatusToApplyDescriptions.Get(this);
        public override string GetCompactDescription() => StatusToApplyDescriptions.GetCompact(this);
    }
}