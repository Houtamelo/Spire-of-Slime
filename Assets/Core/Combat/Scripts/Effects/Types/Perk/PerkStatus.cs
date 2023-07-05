using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Effects.Types.Grappled;
using Core.Combat.Scripts.Perks;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using Core.Utils.Patterns;
using UnityEngine;
using Utils.Patterns;

namespace Core.Combat.Scripts.Effects.Types.Perk
{
    public record PerkStatusRecord(float Duration, bool IsPermanent, CleanString PerkKey, bool IsHidden) : StatusRecord(Duration, IsPermanent)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (PerkDatabase.GetPerk(PerkKey).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(PerkStatusRecord), " data. ", nameof(PerkKey), " with key: ", PerkKey.ToString(), " does not exist in database.");
                return false;
            }
            
            return true;
        }
    }

    public class PerkStatus : StatusInstance
    {
        public override bool IsPositive => true;

        public readonly CleanString PerkKey;
        public readonly bool IsHidden;
        private readonly PerkInstance _perkInstance;

        private PerkStatus(float duration, bool isPermanent, CharacterStateMachine owner, IPerk perkScript, bool isHidden) : base(duration, isPermanent, owner)
        {
            _perkInstance = perkScript.CreateInstance(owner);
            PerkKey = perkScript.Key;
            IsHidden = isHidden;
        }

        public static Option<StatusInstance> CreateInstance(float duration, bool isPermanent, CharacterStateMachine owner, CharacterStateMachine caster, IPerk perkScript, bool isHidden)
        {
            if (duration <= 0 && !isPermanent)
            {
                Debug.LogWarning($"Invalid parameters for {nameof(PerkStatus)}. Duration: {duration}, IsPermanent: {isPermanent}");
                return Option<StatusInstance>.None;
            }
            
            PerkStatus instance = new(duration, isPermanent, owner, perkScript, isHidden);
            owner.StatusModule.AddStatus(instance, caster);
            return Option<StatusInstance>.Some(instance);
        }

        private PerkStatus(PerkStatusRecord record, CharacterStateMachine owner, PerkInstance perkInstance) : base(record, owner)
        {
            PerkKey = perkInstance.Key;
            _perkInstance = perkInstance;
            IsHidden = record.IsHidden;
        }

        public static Option<StatusInstance> CreateInstance(PerkStatusRecord record, CharacterStateMachine owner, PerkInstance perkInstance)
        {
            Option<PerkScriptable> perk = PerkDatabase.GetPerk(record.PerkKey);
            if (perk.IsNone)
            {
                Debug.LogWarning($"Could not find perk with key {record.PerkKey} in {nameof(PerkDatabase)}");
                return Option<StatusInstance>.None;
            }
            
            PerkStatus instance = new(record, owner, perkInstance);
            owner.StatusModule.AddStatus(instance, owner);
            return Option<StatusInstance>.Some(instance);
        }
        
        public override void RequestDeactivation()
        {
            base.RequestDeactivation();
            Owner.PerksModule.Remove(_perkInstance);
        }
        
        public override StatusRecord GetRecord() => new PerkStatusRecord(Duration, IsPermanent, PerkKey, IsHidden);

        public override Option<string> GetDescription() => StatusInstanceDescriptions.Get(this);
        public override EffectType EffectType => IsHidden ? EffectType.HiddenPerk : EffectType.Perk;
        public const int GlobalId = LustGrappled.GlobalId + 1;
    }
}