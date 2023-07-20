using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Perks;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Utils.Math;
using JetBrains.Annotations;

namespace Core.Combat.Scripts.Effects.Types.Perk
{
    public record PerkStatusToApply(CharacterStateMachine Caster, CharacterStateMachine Target, bool FromCrit, ISkill Skill,
                                    IBaseStatusScript ScriptOrigin, TSpan Duration, bool Permanent, PerkScriptable PerkToApply, bool IsHidden)
	    : StatusToApplyDurationBased(Caster, Target, FromCrit, Skill, ScriptOrigin, Duration, Permanent)
    {
        public PerkScriptable PerkToApply { get; set; } = PerkToApply;
        public bool IsHidden { get; set; } = IsHidden;
        
        public override StatusResult ApplyEffect() => PerkStatusScript.TryApply(this);
        protected override void ProcessModifiersInternal() { }
        
        [NotNull]
        public override string GetDescription() => StatusToApplyDescriptions.Get(this);
        [NotNull]
        public override string GetCompactDescription() => StatusToApplyDescriptions.GetCompact(this);
    }
}