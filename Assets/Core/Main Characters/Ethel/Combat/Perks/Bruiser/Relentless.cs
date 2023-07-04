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
using Utils.Math;

namespace Core.Main_Characters.Ethel.Combat.Perks.Bruiser
{
    public class Relentless : PerkScriptable
    {
        public override PerkInstance CreateInstance(CharacterStateMachine character)
        {
            RelentlessInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record RelentlessRecord(CleanString Key, uint StackCount) : PerkRecord(Key)
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

        public override PerkInstance CreateInstance(CharacterStateMachine owner, CharacterEnumerator allCharacters)
        {
            RelentlessInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class RelentlessInstance : PerkInstance, IBaseFloatAttributeModifier, IActionCompletedListener
    {
        public string SharedId => nameof(RelentlessInstance);
        public int Priority => 0;
        private const float HealMultiplier = 0.5f;
        private const float StackMultiplier = -0.07f;
        
        private uint _stackCount;

        public RelentlessInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }
        
        public RelentlessInstance(CharacterStateMachine owner, RelentlessRecord record) : base(owner, record)
        {
            _stackCount = record.StackCount;
        }

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

        public override PerkRecord GetRecord() => new RelentlessRecord(Key, _stackCount);

        public void OnActionCompleted(ListPool<ActionResult> results)
        {
            Span<ActionResult> spanResults = results.AsSpan();
            bool anyValid = false;
            bool anyFailed = false;
            uint totalDamageDealt = 0;
            for (int index = 0; index < results.Count; index++)
            {
                ref ActionResult result = ref spanResults[index];
                if (result.Skill.AllowAllies)
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
            
            uint healAmount = (totalDamageDealt * HealMultiplier).CeilToUInt();
            Owner.StaminaModule.Value.DoHeal(healAmount, isOvertime: false);
        }

        public void Modify(ref float value, CharacterStateMachine self)
        {
            value += _stackCount * StackMultiplier;
        }
    }
}