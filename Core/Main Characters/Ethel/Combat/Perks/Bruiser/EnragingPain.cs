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
using Main_Database.Combat;
using Save_Management;
using UnityEngine;
using Utils.Extensions;

namespace Core.Main_Characters.Ethel.Combat.Perks.Bruiser
{
    public class EnragingPain : PerkScriptable
    {
        public override PerkInstance CreateInstance(CharacterStateMachine character)
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

        public override PerkInstance CreateInstance(CharacterStateMachine owner, CharacterEnumerator allCharacters)
        {
            EnragingPainInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }

    public class EnragingPainInstance : PerkInstance, IBaseFloatAttributeModifier, ISelfAttackedListener
    {
        public string SharedId => nameof(EnragingPainInstance);
        public int Priority => 0;
        private const float MaxValue = 0.3f;
        private const float ValuePerStack = 0.05f;

        private int _stackCount;
        
        public EnragingPainInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }
        
        public EnragingPainInstance(CharacterStateMachine owner, EnragingPainRecord record) : base(owner, record)
        {
            _stackCount = record.StackCount;
        }

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

        public override PerkRecord GetRecord() => new EnragingPainRecord(Key, _stackCount);

        public void OnSelfAttacked(ref ActionResult result)
        {
            if (result.Missed || result.Skill.AllowAllies ||
                Owner.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Corpse or CharacterState.Grappled or CharacterState.Downed ||
                result.DamageDealt.IsNone || result.DamageDealt.Value <= 0)
                return;
            
            _stackCount++;
        }

        public void Modify(ref float value, CharacterStateMachine self)
        {
            value += Mathf.Clamp(_stackCount * ValuePerStack, 0, MaxValue);
        }
    }
}