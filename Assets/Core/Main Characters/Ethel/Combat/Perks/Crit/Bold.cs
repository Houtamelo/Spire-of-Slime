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

namespace Core.Main_Characters.Ethel.Combat.Perks.Crit
{
    public class Bold : PerkScriptable
    {
        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine character)
        {
            BoldInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record BoldRecord(CleanString Key, bool Activated) : PerkRecord(Key)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (PerkDatabase.GetPerk(Key).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(BoldRecord), " data. ", nameof(Key), " with key: ", Key.ToString(), " does not exist in database.");
                return false;
            }
            
            return true;
        }

        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine owner, DirectCharacterEnumerator allCharacters)
        {
            BoldInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class BoldInstance : PerkInstance, IBaseAttributeModifier, IActionCompletedListener
    {
        private bool _activated;

        public BoldInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }

        public BoldInstance(CharacterStateMachine owner, [NotNull] BoldRecord record) : base(owner, record) => _activated = record.Activated;

        protected override void OnSubscribe()
        {
            Owner.Events.ActionCompletedListeners.Add(this);
            Owner.StatsModule.SubscribeCriticalChance(modifier: this, allowDuplicates: false);
        }

        protected override void OnUnsubscribe()
        {
            Owner.Events.ActionCompletedListeners.Remove(this);
            Owner.StatsModule.UnsubscribeCriticalChance(modifier: this);
        }

        [NotNull]
        public override PerkRecord GetRecord() => new BoldRecord(Key, _activated);

        public void OnActionCompleted(ListPool<ActionResult> results)
        {
            if (_activated)
                return;
            
            foreach (ActionResult result in results)
            {
                if (result.Caster == Owner)
                {
                    _activated = true;
                    break;
                }
            }
        }

        public void Modify(ref int value, CharacterStateMachine self)
        {
            if (_activated)
                return;
            
            value = 9999;
        }

        [NotNull]
        public string SharedId => nameof(BoldInstance);
        public int Priority => 999;
    }
}