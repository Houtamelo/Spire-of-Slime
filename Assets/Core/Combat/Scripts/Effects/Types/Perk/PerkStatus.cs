using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Effects.Types.Grappled;
using Core.Combat.Scripts.Perks;
using Core.Save_Management.SaveObjects;
using Core.Utils.Math;
using Core.Utils.Patterns;
using JetBrains.Annotations;
using UnityEngine;

namespace Core.Combat.Scripts.Effects.Types.Perk
{
    public class PerkStatus : StatusInstance
    {
        public override bool IsPositive => true;

        public readonly CleanString PerkKey;
        public readonly bool IsHidden;
        private readonly PerkInstance _perkInstance;

        private PerkStatus(TSpan duration, bool isPermanent, CharacterStateMachine owner, [NotNull] IPerk perkScript, bool isHidden) : base(duration, isPermanent, owner)
        {
            _perkInstance = perkScript.CreateInstance(owner);
            PerkKey = perkScript.Key;
            IsHidden = isHidden;
        }

        public static Option<StatusInstance> CreateInstance(TSpan duration, bool isPermanent, CharacterStateMachine owner, CharacterStateMachine caster, IPerk perkScript, bool isHidden)
        {
            if (duration.Ticks <= 0 && !isPermanent)
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                Debug.LogWarning($"Invalid parameters for {nameof(PerkStatus)}. Duration: {duration}, Permanent: {isPermanent}");
                return Option<StatusInstance>.None;
            }
            
            PerkStatus instance = new(duration, isPermanent, owner, perkScript, isHidden);
            owner.StatusReceiverModule.AddStatus(instance, caster);
            return Option<StatusInstance>.Some(instance);
        }

        public PerkStatus([NotNull] PerkStatusRecord record, CharacterStateMachine owner, [NotNull] PerkInstance perkInstance) : base(record, owner)
        {
            PerkKey = perkInstance.Key;
            _perkInstance = perkInstance;
            IsHidden = record.IsHidden;
        }
        
        public override void RequestDeactivation()
        {
            base.RequestDeactivation();
            Owner.PerksModule.Remove(_perkInstance);
        }
        
        [NotNull]
        public override StatusRecord GetRecord() => new PerkStatusRecord(Duration, Permanent, PerkKey, IsHidden);

        public override Option<string> GetDescription() => StatusInstanceDescriptions.Get(this);
        public override EffectType EffectType => IsHidden ? EffectType.HiddenPerk : EffectType.Perk;
        public const int GlobalId = LustGrappled.GlobalId + 1;
    }
}