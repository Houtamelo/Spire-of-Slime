using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using JetBrains.Annotations;

namespace Core.Main_Characters.Ethel.Combat.Perks.Poison
{
    public class ConcentratedToxins : PerkScriptable
    {
        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine character)
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

        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine owner, DirectCharacterEnumerator allCharacters)
        {
            ConcentratedToxinsInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class ConcentratedToxinsInstance : PerkInstance
    {
        private const int PoisonApplyChanceModifier = 40;
        
        public ConcentratedToxinsInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }
        
        public ConcentratedToxinsInstance(CharacterStateMachine owner, [NotNull] ConcentratedToxinsRecord record) : base(owner, record)
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

        [NotNull]
        public override PerkRecord GetRecord() => new ConcentratedToxinsRecord(Key);
    }
}