using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;

namespace Core.Main_Characters.Ethel.Combat.Perks.Tank
{
    public class Vanguard : PerkScriptable
    {
        public override PerkInstance CreateInstance(CharacterStateMachine character)
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

        public override PerkInstance CreateInstance(CharacterStateMachine owner, CharacterEnumerator allCharacters)
        {
            VanguardInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }

    public class VanguardInstance : PerkInstance
    {
        private const float MoveResistanceBonus = 0.3f;
        private const float StunRecoverySpeedBonus = 0.3f;
        
        public VanguardInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }
        
        public VanguardInstance(CharacterStateMachine owner, VanguardRecord record) : base(owner, record)
        {
        }

        protected override void OnSubscribe()
        {
            if (CreatedFromLoad)
                return;
            
            IResistancesModule resistancesModule = Owner.ResistancesModule;
            resistancesModule.BaseMoveResistance += MoveResistanceBonus;
            resistancesModule.BaseStunRecoverySpeed += StunRecoverySpeedBonus;
        }

        protected override void OnUnsubscribe()
        {
            IResistancesModule resistancesModule = Owner.ResistancesModule;
            resistancesModule.BaseMoveResistance -= MoveResistanceBonus;
            resistancesModule.BaseStunRecoverySpeed -= StunRecoverySpeedBonus;
        }

        public override PerkRecord GetRecord() => new VanguardRecord(Key);
    }
}