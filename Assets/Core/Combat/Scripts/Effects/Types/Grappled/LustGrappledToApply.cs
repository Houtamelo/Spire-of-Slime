using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Utils.Math;
using JetBrains.Annotations;

namespace Core.Combat.Scripts.Effects.Types.Grappled
{
    public record LustGrappledToApply(CharacterStateMachine Caster, CharacterStateMachine Target, bool FromCrit, ISkill Skill, IBaseStatusScript ScriptOrigin, TSpan Duration, 
                                      bool Permanent, string TriggerName, float GraphicalX, int LustPerSecond, int TemptationDeltaPerTime)
	    : StatusToApplyDurationBased(Caster, Target, FromCrit, Skill, ScriptOrigin, Duration, Permanent)
    {
        public string TriggerName { get; set; } = TriggerName;
        public float GraphicalX { get; set; } = GraphicalX;
        public int LustPerSecond { get; set; } = LustPerSecond;
        public int TemptationDeltaPerTime { get; set; } = TemptationDeltaPerTime;
        
        public override StatusResult ApplyEffect() => LustGrappledScript.ProcessModifiersAndTryApply(this);
        protected override void ProcessModifiersInternal() => LustGrappledScript.ProcessModifiers(this);
        
        [NotNull]
        public override string GetDescription() => StatusToApplyDescriptions.Get(this);
        [NotNull]
        public override string GetCompactDescription() => StatusToApplyDescriptions.GetCompact(this);
    }
}