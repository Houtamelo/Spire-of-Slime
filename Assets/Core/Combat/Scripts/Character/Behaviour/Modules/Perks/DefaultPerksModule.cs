using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Utils.Collections;
using Core.Utils.Collections.Extensions;
using Core.Utils.Extensions;
using JetBrains.Annotations;
using UnityEngine;

namespace Core.Combat.Scripts.Behaviour.Modules
{
    public record DefaultPerksModuleRecord(PerkRecord[] Perks) : PerksModuleRecord
    {
        [NotNull]
        public override IPerksModule Deserialize(CharacterStateMachine owner) => new DefaultPerksModule(owner);

        public override void AddSerializedPerks(CharacterStateMachine owner, DirectCharacterEnumerator allCharacters)
        {
            foreach (PerkRecord perkRecord in Perks)
                perkRecord.CreateInstance(owner, allCharacters);
        }

        public override bool IsDataValid(in StringBuilder errors, CharacterRecord[] allCharacters)
        {
            if (Perks == null)
            {
                errors.AppendLine("Invalid ", nameof(CharacterRecord), ". ", nameof(Perks), " is null");
                return false;
            }

            for (int index = 0; index < Perks.Length; index++)
            {
                PerkRecord perkRecord = Perks[index];

                if (perkRecord == null)
                {
                    errors.AppendLine($"Invalid {nameof(CharacterRecord)}. Perk at index {index} is null");
                    return false;
                }

                if (perkRecord.IsDataValid(errors, allCharacters) == false)
                    return false;
            }
            
            return true;
        }
    }
    
    public class DefaultPerksModule : IPerksModule
    {
        private readonly CharacterStateMachine _owner;

        /// <summary> Be careful while accessing this - always enumerate with Fixed </summary>
        private readonly HashSet<PerkInstance> _perks = new();
        public FixedEnumerator<PerkInstance> GetAll => _perks.FixedEnumerate();

        public DefaultPerksModule(CharacterStateMachine owner) => _owner = owner;

        public void Add([NotNull] PerkInstance perk)
        {
            _perks.Add(perk);
            perk.Subscribe();
        }
        
        public void Remove([NotNull] PerkInstance perk)
        {
            if (_perks.Remove(perk) == false)
                Debug.LogWarning($"Tried to remove perk {perk} from {_owner.Script.CharacterName} but it wasn't found", _owner.Display.SomeOrDefault());
            
            perk.Unsubscribe();
        }
        
        public void RemoveAll()
        {
            foreach (PerkInstance perk in GetAll)
                perk.Unsubscribe();
            
            _perks.Clear();
        }

        [NotNull]
        public PerksModuleRecord GetRecord()
        {
            PerkRecord[] perks = new PerkRecord[_perks.Count];
            int index = 0;
            foreach (PerkInstance perk in _perks)
            {
                perks[index] = perk.GetRecord();
                index++;
            }
            
            return new DefaultPerksModuleRecord(perks);
        }
    }
}