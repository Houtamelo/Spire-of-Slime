using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Behaviour.Modules;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using JetBrains.Annotations;

namespace Core.Main_Characters.Ethel.Combat.Perks.Tank
{
    public class Vanguard : PerkScriptable
    {
        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine character)
        {
            VanguardInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record VanguardRecord(CleanString Key) : PerkRecord(Key)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (PerkDatabase.GetPerk(Key).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(VanguardRecord), " data. ", nameof(Key), " with key: ", Key.ToString(), " does not exist in database.");
                return false;
            }
            
            return true;
        }

        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine owner, DirectCharacterEnumerator allCharacters)
        {
            VanguardInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }

    public class VanguardInstance : PerkInstance
    {
        private const int MoveResistanceBonus = 30;
        private const int StunMitigationBonus = 30;
        
        public VanguardInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }
        
        public VanguardInstance(CharacterStateMachine owner, [NotNull] VanguardRecord record) : base(owner, record)
        {
        }

        protected override void OnSubscribe()
        {
            if (CreatedFromLoad)
                return;

            Owner.ResistancesModule.BaseMoveResistance += MoveResistanceBonus;
            Owner.StunModule.BaseStunMitigation += StunMitigationBonus;
        }

        protected override void OnUnsubscribe()
        {
            Owner.ResistancesModule.BaseMoveResistance -= MoveResistanceBonus;
            Owner.StunModule.BaseStunMitigation -= StunMitigationBonus;
        }

        [NotNull]
        public override PerkRecord GetRecord() => new VanguardRecord(Key);
    }
}