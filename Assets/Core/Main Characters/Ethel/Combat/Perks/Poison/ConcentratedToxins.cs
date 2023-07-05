using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;

namespace Core.Main_Characters.Ethel.Combat.Perks.Poison
{
    public class ConcentratedToxins : PerkScriptable
    {
        public override PerkInstance CreateInstance(CharacterStateMachine character)
        {
            ConcentratedToxinsInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record ConcentratedToxinsRecord(CleanString Key) : PerkRecord(Key)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (PerkDatabase.GetPerk(Key).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(ConcentratedToxinsRecord), " data. ", nameof(Key), " with key: ", Key.ToString(), " does not exist in database.");
                return false;
            }
            
            return true;
        }

        public override PerkInstance CreateInstance(CharacterStateMachine owner, CharacterEnumerator allCharacters)
        {
            ConcentratedToxinsInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class ConcentratedToxinsInstance : PerkInstance
    {
        private const float PoisonApplyChanceModifier = 0.4f;
        public ConcentratedToxinsInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }
        
        public ConcentratedToxinsInstance(CharacterStateMachine owner, ConcentratedToxinsRecord record) : base(owner, record)
        {
        }

        protected override void OnSubscribe()
        {
            if (CreatedFromLoad)
                return;
            
            Owner.StatusApplierModule.BasePoisonApplyChance += PoisonApplyChanceModifier;
        }

        protected override void OnUnsubscribe()
        {
            Owner.StatusApplierModule.BasePoisonApplyChance -= PoisonApplyChanceModifier;
        }

        public override PerkRecord GetRecord() => new ConcentratedToxinsRecord(Key);
    }
}