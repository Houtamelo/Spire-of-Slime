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

namespace Core.Main_Characters.Ethel.Combat.Perks.Crit
{
    public class Reliable : PerkScriptable
    {
        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine character)
        {
            ReliableInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record ReliableRecord(CleanString Key, int ConvertedCritChance) : PerkRecord(Key)
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

        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine owner, DirectCharacterEnumerator allCharacters)
        {
            ReliableInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class ReliableInstance : PerkInstance
    {
        private int _convertedCritChance;

        public ReliableInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }
        
        public ReliableInstance(CharacterStateMachine owner, [NotNull] ReliableRecord record) : base(owner, record) => _convertedCritChance = record.ConvertedCritChance;

        protected override void OnSubscribe()
        {
            if (CreatedFromLoad)
                return;
            
            IStatsModule statsModule = Owner.StatsModule;
            _convertedCritChance = statsModule.BaseCriticalChance;
            statsModule.BaseCriticalChance = 0;
            statsModule.BasePower += _convertedCritChance;
        }

        protected override void OnUnsubscribe()
        {
            IStatsModule statsModule = Owner.StatsModule;
            statsModule.BaseCriticalChance += _convertedCritChance;
            statsModule.BasePower -= _convertedCritChance;
        }

        [NotNull]
        public override PerkRecord GetRecord() => new ReliableRecord(Key, _convertedCritChance);
    }
}