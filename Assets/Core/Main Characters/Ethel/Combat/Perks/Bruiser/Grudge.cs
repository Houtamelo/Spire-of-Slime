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
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using JetBrains.Annotations;
using ListPool;

namespace Core.Main_Characters.Ethel.Combat.Perks.Bruiser
{
    public class Grudge : PerkScriptable
    {
        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine character)
        {
            GrudgeInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record GrudgeRecord(CleanString Key, int CurrentModifier) : PerkRecord(Key)
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

        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine owner, DirectCharacterEnumerator allCharacters)
        {
            GrudgeInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }

    public class GrudgeInstance : PerkInstance, IBaseAttributeModifier, IActionCompletedListener
    {
        private const int MaxModifier = 30;
        private int _currentModifier;

        public GrudgeInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }

        public GrudgeInstance(CharacterStateMachine owner, [NotNull] GrudgeRecord record) : base(owner, record) => _currentModifier = record.CurrentModifier;

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

        [NotNull]
        public override PerkRecord GetRecord() => new GrudgeRecord(Key, _currentModifier);

        public void OnActionCompleted([NotNull] ListPool<ActionResult> results)
        {
            Span<ActionResult> spanResults = results.AsSpan();
            bool anyValid = false;
            int targetCount = 0;
            int failCount = 0;
            for (int index = 0; index < results.Count; index++)
            {
                ref ActionResult result = ref spanResults[index];
                if (result.Target == Owner || result.Skill.IsPositive)
                    continue;

                targetCount++;
                if (result.Missed)
                    failCount++;

                anyValid = true;
            }

            if (anyValid == false || targetCount == 0)
                return;
            
            _currentModifier = (failCount * MaxModifier) / targetCount;
        }

        public void Modify(ref int value, CharacterStateMachine self)
        {
            value += _currentModifier;
        }

        [NotNull]
        public string SharedId => nameof(GrudgeInstance);
        public int Priority => 0;
    }
}