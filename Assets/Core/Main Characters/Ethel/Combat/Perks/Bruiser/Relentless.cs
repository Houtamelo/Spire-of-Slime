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
using Core.Utils.Math;
using JetBrains.Annotations;
using ListPool;

namespace Core.Main_Characters.Ethel.Combat.Perks.Bruiser
{
    public class Relentless : PerkScriptable
    {
        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine character)
        {
            RelentlessInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record RelentlessRecord(CleanString Key, int StackCount) : PerkRecord(Key)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (PerkDatabase.GetPerk(Key).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(RelentlessRecord), " data. ", nameof(Key), " with key: ", Key.ToString(), " does not exist in database.");
                return false;
            }
            
            return true;
        }

        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine owner, DirectCharacterEnumerator allCharacters)
        {
            RelentlessInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class RelentlessInstance : PerkInstance, IBaseAttributeModifier, IActionCompletedListener
    {
        private const int HealMultiplier = 50;
        private const int StackMultiplier = -7;

        private int _stackCount;

        public RelentlessInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }

        public RelentlessInstance(CharacterStateMachine owner, [NotNull] RelentlessRecord record) : base(owner, record) => _stackCount = record.StackCount;

        protected override void OnSubscribe()
        {
            Owner.Events.ActionCompletedListeners.Add(this);
            Owner.StatsModule.SubscribeAccuracy(this, allowDuplicates: false);
        }

        protected override void OnUnsubscribe()
        {
            Owner.Events.ActionCompletedListeners.Remove(this);
            Owner.StatsModule.UnsubscribeAccuracy(this);
        }

        [NotNull]
        public override PerkRecord GetRecord() => new RelentlessRecord(Key, _stackCount);

        public void OnActionCompleted([NotNull] ListPool<ActionResult> results)
        {
            Span<ActionResult> spanResults = results.AsSpan();
            bool anyValid = false;
            bool anyFailed = false;
            int totalDamageDealt = 0;
            for (int index = 0; index < results.Count; index++)
            {
                ref ActionResult result = ref spanResults[index];
                if (result.Skill.IsPositive)
                    continue;

                if (result.Hit)
                {
                    if (result.DamageDealt.IsSome)
                        totalDamageDealt += result.DamageDealt.Value;
                    
                    _stackCount++;
                }
                else
                {
                    anyFailed = true;
                }

                anyValid = true;
            }

            if (anyValid == false)
                return;

            if (anyFailed)
                _stackCount = 0;
            else
                _stackCount++;

            if (Owner.StaminaModule.IsNone)
                return;
            
            int healAmount = (totalDamageDealt * HealMultiplier) / 100;
            Owner.StaminaModule.Value.DoHeal(healAmount, isOvertime: false);
        }

        public void Modify(ref int value, CharacterStateMachine self)
        {
            value += _stackCount * StackMultiplier;
        }

        [NotNull]
        public string SharedId => nameof(RelentlessInstance);
        public int Priority => 0;
    }
}