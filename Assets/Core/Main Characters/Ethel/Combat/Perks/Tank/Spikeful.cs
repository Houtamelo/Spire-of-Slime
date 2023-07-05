using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.Interfaces;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using UnityEngine;

namespace Core.Main_Characters.Ethel.Combat.Perks.Tank
{
    public class Spikeful : PerkScriptable 
    {
        public override PerkInstance CreateInstance(CharacterStateMachine character)
        {
            SpikefulInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record SpikefulRecord(CleanString Key) : PerkRecord(Key)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (PerkDatabase.GetPerk(Key).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(SpikefulRecord), " data. ", nameof(Key), " with key: ", Key.ToString(), " does not exist in database.");
                return false;
            }
            
            return true;
        }

        public override PerkInstance CreateInstance(CharacterStateMachine owner, CharacterEnumerator allCharacters)
        {
            SpikefulInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class SpikefulInstance : PerkInstance, IBaseFloatAttributeModifier
    {
        public int Priority => 0;
        public string SharedId => nameof(SpikefulInstance);
        private const float MaxValue = 0.3f;
        private const float ResilienceMultiplier = 1;
        
        public SpikefulInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }
        
        public SpikefulInstance(CharacterStateMachine owner, SpikefulRecord record) : base(owner, record)
        {
        }
        
        protected override void OnSubscribe()
        {
            Owner.StatsModule.SubscribePower(this, allowDuplicates: false);
        }

        protected override void OnUnsubscribe()
        {
            Owner.StatsModule.UnsubscribePower(this);
        }

        public override PerkRecord GetRecord() => new SpikefulRecord(Key);

        public void Modify(ref float value, CharacterStateMachine self)
        {
            if (self.StaminaModule.IsNone)
                return;
            
            value += Mathf.Clamp(self.StaminaModule.Value.BaseResilience * ResilienceMultiplier, 0, MaxValue);
        }
    }
}