using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Behaviour.Modules;
using Core.Combat.Scripts.Effects.Interfaces;
using Core.Combat.Scripts.Effects.Types.BuffOrDebuff;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Managers;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using Core.Utils.Math;
using Core.Utils.Patterns;
using JetBrains.Annotations;

namespace Core.Main_Characters.Nema.Combat.Perks.AoE
{
    public class Regret : PerkScriptable
    {
        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine character)
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

        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine owner, DirectCharacterEnumerator allCharacters)
        {
            RegretInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class RegretInstance : PerkInstance, IBaseAttributeModifier
    {
        private const int DownedComposureModifier = 30;
        private const int OnKillComposureModifier = -15;

        private static readonly BuffOrDebuffScript Debuff = new(Permanent: false, TSpan.FromSeconds(4.0), BaseApplyChance: 100, CombatStat.Composure, OnKillComposureModifier);

        private readonly CharacterManager.DefeatedDelegate _onDefeat;

        public RegretInstance(CharacterStateMachine owner, CleanString key) : base(owner, key) => _onDefeat = OnKill;

        public RegretInstance(CharacterStateMachine owner, [NotNull] RegretRecord record) : base(owner, record) => _onDefeat = OnKill;

        private void OnKill(CharacterStateMachine defeated, Option<CharacterStateMachine> lastDamager)
        {
            if (lastDamager.IsNone || lastDamager.Value != Owner)
                return;

            BuffOrDebuffToApply effectStruct = (BuffOrDebuffToApply) Debuff.GetStatusToApply(caster: Owner, target: Owner, crit: false, skill: null);
            BuffOrDebuffScript.ProcessModifiersAndTryApply(effectStruct);
        }

        protected override void OnSubscribe()
        {
            if (Owner.Display.IsSome)
                Owner.Display.Value.CombatManager.Characters.DefeatedEvent += _onDefeat;
            
            if (Owner.LustModule.TrySome(out ILustModule lustModule))
                lustModule.SubscribeComposure(modifier: this, allowDuplicates: false);
        }

        protected override void OnUnsubscribe()
        {
            if (Owner.Display.IsSome)
                Owner.Display.Value.CombatManager.Characters.DefeatedEvent -= _onDefeat;
            
            if (Owner.LustModule.TrySome(out ILustModule lustModule))
                lustModule.UnsubscribeComposure(modifier: this);
        }

        [NotNull]
        public override PerkRecord GetRecord() => new RegretRecord(Key);

        public void Modify(ref int value, [NotNull] CharacterStateMachine self)
        {
            if (self.DownedModule.IsSome && self.DownedModule.Value.GetRemaining().Ticks > 0)
                value += DownedComposureModifier;
        }

        [NotNull]
        public string SharedId => nameof(RegretInstance);
        public int Priority => 0;
    }
}