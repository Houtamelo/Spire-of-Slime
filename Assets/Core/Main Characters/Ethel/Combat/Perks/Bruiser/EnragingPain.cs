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

namespace Core.Main_Characters.Ethel.Combat.Perks.Bruiser
{
    public class EnragingPain : PerkScriptable
    {
        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine character)
        {
            EnragingPainInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record EnragingPainRecord(CleanString Key, int StackCount) : PerkRecord(Key)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (PerkDatabase.GetPerk(Key).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(EnragingPainRecord), " data. ", nameof(Key), " with key: ", Key.ToString(), " does not exist in database.");
                return false;
            }
            
            return true;
        }

        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine owner, DirectCharacterEnumerator allCharacters)
        {
            EnragingPainInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }

    public class EnragingPainInstance : PerkInstance, IBaseAttributeModifier, ISelfAttackedListener
    {
        private const int MaxValue = 30;
        private const int ValuePerStack = 5;

        private int _stackCount;

        public EnragingPainInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }

        public EnragingPainInstance(CharacterStateMachine owner, [NotNull] EnragingPainRecord record) : base(owner, record) => _stackCount = record.StackCount;

        protected override void OnSubscribe()
        {
            Owner.Events.SelfAttackedListeners.Add(this);
            Owner.StatsModule.SubscribePower(this, allowDuplicates: false);
        }

        protected override void OnUnsubscribe()
        {
            Owner.Events.SelfAttackedListeners.Remove(this); 
            Owner.StatsModule.UnsubscribePower(this);
        }

        [NotNull]
        public override PerkRecord GetRecord() => new EnragingPainRecord(Key, _stackCount);

        public void OnSelfAttacked(ref ActionResult result)
        {
            if (result.Missed || result.Skill.IsPositive ||
                Owner.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Corpse or CharacterState.Grappled or CharacterState.Downed ||
                result.DamageDealt.IsNone || result.DamageDealt.Value <= 0)
                return;
            
            _stackCount++;
        }

        public void Modify(ref int value, CharacterStateMachine self)
        {
            value += Mathf.Clamp(_stackCount * ValuePerStack, min: 0, MaxValue);
        }

        [NotNull]
        public string SharedId => nameof(EnragingPainInstance);
        public int Priority => 0;
    }
}