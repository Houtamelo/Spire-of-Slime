using System.Collections.Generic;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Combat.Scripts.Perks;
using Core.Utils.Collections;
using Core.Utils.Extensions;
using UnityEngine;

namespace Core.Combat.Scripts.DefaultModules
{
    public class DefaultPerksModule : IPerksModule
    {
        private readonly CharacterStateMachine _owner;

        /// <summary> Be careful while accessing this - always enumerate with Fixed </summary>
        private readonly HashSet<PerkInstance> _perks = new();
        public FixedEnumerable<PerkInstance> GetAll => _perks.FixedEnumerate();

        public DefaultPerksModule(CharacterStateMachine owner) => _owner = owner;

        public void Add(PerkInstance perk)
        {
            _perks.Add(perk);
            perk.Subscribe();
        }
        
        public void Remove(PerkInstance perk)
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
    }
}