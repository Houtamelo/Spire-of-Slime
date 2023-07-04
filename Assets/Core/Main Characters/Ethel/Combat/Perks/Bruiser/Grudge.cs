using System;
using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.Interfaces;
using Core.Combat.Scripts.Interfaces.Events;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Combat.Scripts.Skills.Action;
using ListPool;
using Main_Database.Combat;
using Save_Management;
using Utils.Extensions;

namespace Core.Main_Characters.Ethel.Combat.Perks.Bruiser
{
    public class Grudge : PerkScriptable
    {
        public override PerkInstance CreateInstance(CharacterStateMachine character)
        {
            GrudgeInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record GrudgeRecord(CleanString Key, float CurrentModifier) : PerkRecord(Key)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (PerkDatabase.GetPerk(Key).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(GrudgeRecord), " data. ", nameof(Key), " with key: ", Key.ToString(), " does not exist in database.");
                return false;
            }
            
            return true;
        }

        public override PerkInstance CreateInstance(CharacterStateMachine owner, CharacterEnumerator allCharacters)
        {
            GrudgeInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }

    public class GrudgeInstance : PerkInstance, IBaseFloatAttributeModifier, IActionCompletedListener
    {
        public string SharedId => nameof(GrudgeInstance);
        public int Priority => 0;

        private const float MaxModifier = 0.3f;
        private float _currentModifier;
        
        public GrudgeInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }
        
        public GrudgeInstance(CharacterStateMachine owner, GrudgeRecord record) : base(owner, record)
        {
            _currentModifier = record.CurrentModifier;
        }

        protected override void OnSubscribe()
        {
            Owner.Events.ActionCompletedListeners.Add(this);
            Owner.StatsModule.SubscribePower(this, allowDuplicates: false);
        }

        protected override void OnUnsubscribe()
        {
            Owner.Events.ActionCompletedListeners.Remove(this);
            Owner.StatsModule.UnsubscribePower(this);
        }

        public override PerkRecord GetRecord() => new GrudgeRecord(Key, _currentModifier);

        public void OnActionCompleted(ListPool<ActionResult> results)
        {
            Span<ActionResult> spanResults = results.AsSpan();
            bool anyValid = false;
            int targetCount = 0;
            int failCount = 0;
            for (int index = 0; index < results.Count; index++)
            {
                ref ActionResult result = ref spanResults[index];
                if (result.Target == Owner || result.Skill.AllowAllies)
                    continue;

                targetCount++;
                if (result.Missed)
                    failCount++;

                anyValid = true;
            }

            if (anyValid == false || targetCount == 0)
                return;
            
            _currentModifier = (float)failCount / targetCount * MaxModifier;
        }

        public void Modify(ref float value, CharacterStateMachine self)
        {
            value += _currentModifier;
        }
    }
}