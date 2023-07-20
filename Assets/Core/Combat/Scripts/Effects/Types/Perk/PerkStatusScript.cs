using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Behaviour.Modules;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Perks;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Utils.Math;
using Core.Utils.Patterns;
using JetBrains.Annotations;

namespace Core.Combat.Scripts.Effects.Types.Perk
{
    public record PerkStatusScript(bool Permanent, TSpan BaseDuration, PerkScriptable PerkToApply, bool IsHidden) : StatusScriptDurationBased(Permanent, BaseDuration)
    {
        public PerkScriptable PerkToApply { get; set; } = PerkToApply;
        public bool IsHidden { get; set; } = IsHidden;

        [NotNull]
        public override StatusToApply GetStatusToApply(CharacterStateMachine caster, CharacterStateMachine target, bool crit, [CanBeNull] ISkill skill = null) 
            => new PerkStatusToApply(caster, target, crit, skill, this, BaseDuration, Permanent, PerkToApply, IsHidden);

        public override StatusResult ApplyEffect(CharacterStateMachine caster, CharacterStateMachine target, bool crit, [CanBeNull] ISkill skill = null) 
            => TryApply(new PerkStatusToApply(caster, target, crit, skill, this, BaseDuration, Permanent, PerkToApply, IsHidden));

        public static StatusResult TryApply([NotNull] PerkStatusToApply record)
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
                foreach (StatusInstance status in record.Target.StatusReceiverModule.GetAll)
                {
                    if (status is not PerkStatus perkStatusInstance || perkStatusInstance.PerkKey != record.PerkToApply.Key || status.IsDeactivated)
                        continue;
                    
                    instance = status;
                    hasStatusAlready = true;
                    generatesInstance = true;
                    break;
                }

            switch (hasPerkAlready)
            {
                case true when hasStatusAlready:
                    instance.Duration = TSpan.ChoseMax(record.Duration, instance.Duration);
                    break;
                case false:
                {
                    Option<StatusInstance> option = PerkStatus.CreateInstance(record.Duration, record.Permanent, record.Target, record.Caster, record.PerkToApply, record.IsHidden);
                    if (option.IsSome)
                    {
                        instance = option.Value;
                        generatesInstance = true;
                        success = true;
                    }
                    else
                    {
                        // ReSharper disable once RedundantAssignment
                        success = false;
                        // ReSharper disable once RedundantAssignment
                        generatesInstance = false;
                    }

                    break;
                }
                default:
                    success = false;
                    // ReSharper disable once RedundantAssignment
                    generatesInstance = false;
                    break;
            }

            return new StatusResult(record.Caster, record.Target, success, instance, generatesInstance, record.IsHidden ? EffectType.HiddenPerk : EffectType.Perk);
        }

        public override float ComputePoints(ref SkillStruct skillStruct, [NotNull] CharacterStateMachine target)
        {
            if (target.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Corpse or CharacterState.Downed or CharacterState.Grappled)
                return 0f;
            
            float durationMultiplier = Permanent ? HeuristicConstants.PermanentMultiplier : BaseDuration.FloatSeconds * HeuristicConstants.DurationMultiplier;
            return durationMultiplier * HeuristicConstants.PerkMultiplier * (IsPositive ? 1f : -1f);
        }
        
        public override EffectType EffectType => IsHidden ? EffectType.HiddenPerk : EffectType.Perk;
        [NotNull]
        public override string Description => StatusScriptDescriptions.Get(this);
        
        public override bool IsPositive => PerkToApply.IsPositive;
        
        public override bool PlaysBarkAppliedOnCaster => false;
        public override bool PlaysBarkAppliedOnEnemy => false;
        public override bool PlaysBarkAppliedOnAlly => false;
    }
}