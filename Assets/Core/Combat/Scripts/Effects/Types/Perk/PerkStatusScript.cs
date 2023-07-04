using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Perks;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using UnityEngine;
using Utils.Patterns;

namespace Core.Combat.Scripts.Effects.Types.Perk
{
    public record PerkStatusScript(bool Permanent, float BaseDuration, PerkScriptable PerkToApply, bool IsHidden) : StatusScriptDurationBased(Permanent, BaseDuration)
    {
        public PerkScriptable PerkToApply { get; set; } = PerkToApply;
        public bool IsHidden { get; set; } = IsHidden;

        public override bool IsPositive => PerkToApply.IsPositive;
        
        public override bool PlaysBarkAppliedOnCaster => false;
        public override bool PlaysBarkAppliedOnEnemy => false;
        public override bool PlaysBarkAppliedOnAlly => false;

        public override StatusToApply GetStatusToApply(CharacterStateMachine caster, CharacterStateMachine target, bool crit, ISkill skill = null) 
            => new PerkStatusToApply(caster, target, crit, skill, this, BaseDuration, Permanent, PerkToApply, IsHidden);

        public override StatusResult ApplyEffect(CharacterStateMachine caster, CharacterStateMachine target, bool crit, ISkill skill = null) 
            => TryApply(new PerkStatusToApply(caster, target, crit, skill, this, BaseDuration, Permanent, PerkToApply, IsHidden));
        
        public static StatusResult TryApply(PerkStatusToApply record)
        {
            bool success = false;
            bool generatesInstance = false;
            bool hasPerkAlready = false;
            foreach (PerkInstance perkInstance in record.Target.PerksModule.GetAll)
            {
                if (perkInstance.Key != record.PerkToApply.Key)
                    continue;
                
                hasPerkAlready = true;
                success = true;
                break;
            }

            bool hasStatusAlready = false;

            StatusInstance instance = null;
            if (hasPerkAlready)
            {
                foreach (StatusInstance status in record.Target.StatusModule.GetAll)
                {
                    if (status is not PerkStatus perkStatusInstance || perkStatusInstance.PerkKey != record.PerkToApply.Key || status.IsDeactivated)
                        continue;
                    
                    instance = status;
                    hasStatusAlready = true;
                    generatesInstance = true;
                    break;
                }
            }

            switch (hasPerkAlready)
            {
                case true when hasStatusAlready:
                    instance.Duration = Mathf.Max(record.Duration, instance.Duration);
                    break;
                case false:
                {
                    Option<StatusInstance> option = PerkStatus.CreateInstance(record.Duration, record.IsPermanent, record.Target, record.Caster, record.PerkToApply, record.IsHidden);
                    if (option.IsSome)
                    {
                        instance = option.Value;
                        generatesInstance = true;
                        success = true;
                    }
                    else
                    {
                        success = false;
                        generatesInstance = false;
                    }

                    break;
                }
                default:
                    success = false;
                    generatesInstance = false;
                    break;
            }

            return new StatusResult(record.Caster, record.Target, success, instance, generatesInstance, record.IsHidden ? EffectType.HiddenPerk : EffectType.Perk);
        }

        public override float ComputePoints(ref SkillStruct skillStruct, CharacterStateMachine target)
        {
            if (target.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Corpse or CharacterState.Downed or CharacterState.Grappled)
                return 0f;
            
            float durationMultiplier = Permanent ? HeuristicConstants.PermanentMultiplier : BaseDuration * HeuristicConstants.DurationMultiplier;
            return durationMultiplier * HeuristicConstants.PerkMultiplier * (IsPositive ? 1f : -1f);
        }

        public override EffectType EffectType => IsHidden ? EffectType.HiddenPerk : EffectType.Perk;
        public override string Description => StatusScriptDescriptions.Get(this);
    }
}