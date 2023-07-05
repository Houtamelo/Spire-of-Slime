using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.Interfaces;
using Core.Combat.Scripts.Effects.Types.BuffOrDebuff;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Combat.Scripts.Managers;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using Core.Utils.Patterns;
using Utils.Patterns;

namespace Core.Main_Characters.Nema.Combat.Perks.AoE
{
    public class Regret : PerkScriptable
    {
        public override PerkInstance CreateInstance(CharacterStateMachine character)
        {
            RegretInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record RegretRecord(CleanString Key) : PerkRecord(Key)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (PerkDatabase.GetPerk(Key).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(RegretRecord), " data. ", nameof(Key), " with key: ", Key.ToString(), " does not exist in database.");
                return false;
            }
            
            return true;
        }

        public override PerkInstance CreateInstance(CharacterStateMachine owner, CharacterEnumerator allCharacters)
        {
            RegretInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class RegretInstance : PerkInstance, IBaseFloatAttributeModifier
    {
        public string SharedId => nameof(RegretInstance);
        public int Priority => 0;
        private const float DownedComposureModifier = 0.3f;
        private const float OnKillComposureModifier = -0.15f;
        private const float Duration = 4f;
        
        private static readonly BuffOrDebuffScript Debuff = new(false, Duration, 1f, CombatStat.Composure, OnKillComposureModifier);

        private readonly CharacterManager.DefeatedDelegate _onDefeat;
        
        public RegretInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
            _onDefeat = OnKill;
        }
        
        public RegretInstance(CharacterStateMachine owner, RegretRecord record) : base(owner, record)
        {
            _onDefeat = OnKill;
        }

        private void OnKill(CharacterStateMachine defeated, Option<CharacterStateMachine> lastDamager)
        {
            if (lastDamager.IsNone || lastDamager.Value != Owner)
                return;

            BuffOrDebuffToApply effectStruct = (BuffOrDebuffToApply) Debuff.GetStatusToApply(Owner, Owner, false, null);
            BuffOrDebuffScript.ProcessModifiersAndTryApply(effectStruct);
        }

        protected override void OnSubscribe()
        {
            if (Owner.Display.IsSome)
                Owner.Display.Value.CombatManager.Characters.DefeatedEvent += _onDefeat;
            
            if (Owner.LustModule.TrySome(out ILustModule lustModule))
                lustModule.SubscribeComposure(this, allowDuplicates: false);
        }

        protected override void OnUnsubscribe()
        {
            if (Owner.Display.IsSome)
                Owner.Display.Value.CombatManager.Characters.DefeatedEvent -= _onDefeat;
            
            if (Owner.LustModule.TrySome(out ILustModule lustModule))
                lustModule.UnsubscribeComposure(this);
        }

        public override PerkRecord GetRecord() => new RegretRecord(Key);

        public void Modify(ref float value, CharacterStateMachine self)
        {
            if (self.DownedModule.IsSome && self.DownedModule.Value.GetRemaining() > 0)
                value += DownedComposureModifier;
        }
    }
}