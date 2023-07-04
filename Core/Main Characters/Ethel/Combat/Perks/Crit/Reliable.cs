using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Main_Database.Combat;
using Save_Management;
using Utils.Extensions;

namespace Core.Main_Characters.Ethel.Combat.Perks.Crit
{
    public class Reliable : PerkScriptable
    {
        public override PerkInstance CreateInstance(CharacterStateMachine character)
        {
            ReliableInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record ReliableRecord(CleanString Key, float ConvertedCritChance) : PerkRecord(Key)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (PerkDatabase.GetPerk(Key).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(ReliableRecord), " data. ", nameof(Key), " with key: ", Key.ToString(), " does not exist in database.");
                return false;
            }
            
            return true;
        }

        public override PerkInstance CreateInstance(CharacterStateMachine owner, CharacterEnumerator allCharacters)
        {
            ReliableInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class ReliableInstance : PerkInstance
    {
        private float _convertedCritChance;

        public ReliableInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }
        
        public ReliableInstance(CharacterStateMachine owner, ReliableRecord record) : base(owner, record)
        {
            _convertedCritChance = record.ConvertedCritChance;
        }

        protected override void OnSubscribe()
        {
            if (CreatedFromLoad)
                return;
            
            IStatsModule statsModule = Owner.StatsModule;
            _convertedCritChance = statsModule.BaseCriticalChance;
            statsModule.BaseCriticalChance = 0f;
            statsModule.BasePower += _convertedCritChance;
        }

        protected override void OnUnsubscribe()
        {
            IStatsModule statsModule = Owner.StatsModule;
            statsModule.BaseCriticalChance += _convertedCritChance;
            statsModule.BasePower -= _convertedCritChance;
        }

        public override PerkRecord GetRecord() => new ReliableRecord(Key, _convertedCritChance);
    }
}