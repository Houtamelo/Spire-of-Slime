using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Interfaces;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using JetBrains.Annotations;

namespace Core.Combat.Scripts.Effects.Types.Summon
{
    public record SummonToApply(ICharacterScript CharacterToSummon, CharacterStateMachine Caster, CharacterStateMachine Target, bool FromCrit, ISkill Skill, IBaseStatusScript ScriptOrigin)
        : StatusToApply(Caster, Target, FromCrit, Skill, ScriptOrigin)
    {
        public const string Vowels = "aeiouAEIOU";
        
        public ICharacterScript CharacterToSummon { get; set; } = CharacterToSummon;
        
        public override StatusResult ApplyEffect() => SummonScript.ProcessModifiersAndTryApply(this);
        protected override void ProcessModifiersInternal() => SummonScript.ProcessModifiers(this);
        
        [NotNull]
        public override string GetDescription() => StatusToApplyDescriptions.Get(this);
        [NotNull]
        public override string GetCompactDescription() => StatusToApplyDescriptions.GetCompact(this);
    }
}