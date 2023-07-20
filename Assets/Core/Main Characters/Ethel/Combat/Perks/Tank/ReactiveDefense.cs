using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.Interfaces;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Interfaces.Events;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Combat.Scripts.Skills.Action;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using JetBrains.Annotations;
using UnityEngine;

namespace Core.Main_Characters.Ethel.Combat.Perks.Tank
{
    public class ReactiveDefense : PerkScriptable
    {
        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine character)
        {
            ReactiveDefenseInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record ReactiveDefenseRecord(CleanString Key, int StackCount) : PerkRecord(Key)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (PerkDatabase.GetPerk(Key).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(ReactiveDefenseRecord), " data. ", nameof(Key), " with key: ", Key.ToString(), " does not exist in database.");
                return false;
            }
            
            return true;
        }

        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine owner, DirectCharacterEnumerator allCharacters)
        {
            ReactiveDefenseInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class ReactiveDefenseInstance : PerkInstance, IBaseAttributeModifier, ISelfAttackedListener
    {
        private const int ValuePerStack = 4;
        private const int MaxValue = 20;

        private int _stackCount;

        public ReactiveDefenseInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }

        public ReactiveDefenseInstance(CharacterStateMachine owner, [NotNull] ReactiveDefenseRecord record) : base(owner, record) => _stackCount = record.StackCount;

        protected override void OnSubscribe()
        {
            Owner.Events.SelfAttackedListeners.Add(this);
            
            if (Owner.StaminaModule.IsSome)
                Owner.StaminaModule.Value.SubscribeResilience(this, allowDuplicates: false);
        }

        protected override void OnUnsubscribe()
        {
            Owner.Events.SelfAttackedListeners.Remove(this);
            
            if (Owner.StaminaModule.IsSome)
                Owner.StaminaModule.Value.UnsubscribeResilience(this);
        }

        [NotNull]
        public override PerkRecord GetRecord() => new ReactiveDefenseRecord(Key, _stackCount);

        public void OnSelfAttacked(ref ActionResult result)
        {
            if (result.Hit && result.Skill.IsPositive == false &&
                Owner.StateEvaluator.PureEvaluate() is not CharacterState.Defeated and not CharacterState.Corpse and not CharacterState.Downed and not CharacterState.Grappled &&
                result.DamageDealt.TrySome(out int damage) && damage > 0)
                _stackCount++;
        }

        public void Modify(ref int value, CharacterStateMachine self)
        {
            value += Mathf.Clamp(_stackCount * ValuePerStack, 0, MaxValue);
        }

        [NotNull]
        public string SharedId => nameof(ReactiveDefenseInstance);
        public int Priority => 0;
    }
}