using System;
using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.Types.BuffOrDebuff;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using Core.Utils.Math;
using JetBrains.Annotations;

namespace Core.Main_Characters.Nema.Combat.Perks.Healer
{
    public class Alarmed : PerkScriptable
    {
        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine character)
        {
            AlarmedInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record AlarmedRecord(CleanString Key) : PerkRecord(Key)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (PerkDatabase.GetPerk(Key).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(AlarmedRecord), " data. ", nameof(Key), " with key: ", Key.ToString(), " does not exist in database.");
                return false;
            }
            
            return true;
        }

        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine owner, DirectCharacterEnumerator allCharacters)
        {
            AlarmedInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class AlarmedInstance : PerkInstance
    {
        private static readonly BuffOrDebuffScript Buff = new(Permanent: false, TSpan.FromSeconds(5.0), BaseApplyChance: 100, CombatStat.Dodge, BaseDelta: 50);

        private readonly Action _onCombatBegin;

        public AlarmedInstance(CharacterStateMachine owner, CleanString key) : base(owner, key) => _onCombatBegin = OnCombatBegin;

        public AlarmedInstance(CharacterStateMachine owner, [NotNull] AlarmedRecord record) : base(owner, record) => _onCombatBegin = OnCombatBegin;

        protected override void OnSubscribe()
        {
            if (Owner.Display.IsSome)
                Owner.Display.Value.CombatManager.OnCombatBegin += _onCombatBegin;
        }

        protected override void OnUnsubscribe()
        {
            if (Owner.Display.IsSome)
                Owner.Display.Value.CombatManager.OnCombatBegin -= _onCombatBegin;
        }

        [NotNull]
        public override PerkRecord GetRecord() => new AlarmedRecord(Key);

        private void OnCombatBegin()
        {
            BuffOrDebuffToApply toApply = (BuffOrDebuffToApply) Buff.GetStatusToApply(Owner, Owner, false, skill: null);
            BuffOrDebuffScript.ProcessModifiersAndTryApply(toApply);
        }
    }
}