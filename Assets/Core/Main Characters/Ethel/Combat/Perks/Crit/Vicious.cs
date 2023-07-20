using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.Interfaces;
using Core.Combat.Scripts.Interfaces.Events;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Combat.Scripts.Skills.Action;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using JetBrains.Annotations;
using ListPool;
using UnityEngine;

namespace Core.Main_Characters.Ethel.Combat.Perks.Crit
{
    public class Vicious : PerkScriptable
    {
        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine character)
        {
            ViciousInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record ViciousRecord(CleanString Key, int Counter) : PerkRecord(Key)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (PerkDatabase.GetPerk(Key).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(ViciousRecord), " data. ", nameof(Key), " with key: ", Key.ToString(), " does not exist in database.");
                return false;
            }
            
            return true;
        }

        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine owner, DirectCharacterEnumerator allCharacters)
        {
            ViciousInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }

    public class ViciousInstance : PerkInstance, IBaseAttributeModifier, IActionCompletedListener
    {
        private const int CritPerStack = 10;

        private int _counter;

        public ViciousInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }

        public ViciousInstance(CharacterStateMachine owner, [NotNull] ViciousRecord record) : base(owner, record) => _counter = record.Counter;

        protected override void OnSubscribe()
        {
            Owner.Events.ActionCompletedListeners.Add(this);
            Owner.StatsModule.SubscribeCriticalChance(this, allowDuplicates: false);
        }

        protected override void OnUnsubscribe()
        {
            Owner.Events.ActionCompletedListeners.Remove(this);
            Owner.StatsModule.UnsubscribeCriticalChance(this);
        }

        [NotNull]
        public override PerkRecord GetRecord() => new ViciousRecord(Key, _counter);

        public void OnActionCompleted([NotNull] ListPool<ActionResult> results)
        {
            foreach (ActionResult actionResult in results)
            {
                if (actionResult.Missed || actionResult.Caster != Owner)
                    continue;

                if (actionResult.Critical)
                    _counter = Mathf.Max(_counter - 1, 0);
                else if (actionResult.Target.PositionHandler.IsLeftSide != Owner.PositionHandler.IsLeftSide)
                    _counter++;
            }
        }

        public void Modify(ref int value, CharacterStateMachine self)
        {
            value += _counter * CritPerStack;
        }

        [NotNull]
        public string SharedId => nameof(ViciousInstance);
        public int Priority => 0;
    }
}