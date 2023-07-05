using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.Interfaces;
using Core.Combat.Scripts.Interfaces.Events;
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Combat.Scripts.Skills.Action;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using ListPool;

namespace Core.Main_Characters.Nema.Combat.Perks.AoE
{
    public class Hatred : PerkScriptable
    {
        public override PerkInstance CreateInstance(CharacterStateMachine character)
        {
            HatredInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record HatredRecord(CleanString Key, int Hits) : PerkRecord(Key)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (PerkDatabase.GetPerk(Key).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(HatredRecord), " data. ", nameof(Key), " with key: ", Key.ToString(), " does not exist in database.");
                return false;
            }
            
            return true;
        }

        public override PerkInstance CreateInstance(CharacterStateMachine owner, CharacterEnumerator allCharacters)
        {
            HatredInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class HatredInstance : PerkInstance, IBaseFloatAttributeModifier, ISelfAttackedListener, IActionCompletedListener
    {
        public string SharedId => nameof(HatredInstance);
        public int Priority => 0;
        private const float ResilienceModifier = 0.05f;
        private const float DamageModifierPerHit = 0.25f;
        private const int LustModifierPerHit = 3;
        
        private int _hits;
        
        public HatredInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }
        
        public HatredInstance(CharacterStateMachine owner, HatredRecord record) : base(owner, record)
        {
            _hits = record.Hits;
        }

        protected override void OnSubscribe()
        {
            IEventsHandler eventsHandler = Owner.Events;
            eventsHandler.ActionCompletedListeners.Add(this);
            eventsHandler.SelfAttackedListeners.Add(this);
            
            Owner.StatsModule.SubscribePower(this, allowDuplicates: false);
            if (CreatedFromLoad)
                return;
            
            if (Owner.StaminaModule.TrySome(out IStaminaModule staminaModule))
                staminaModule.BaseResilience += ResilienceModifier;
        }

        protected override void OnUnsubscribe()
        {
            IEventsHandler eventsHandler = Owner.Events;
            eventsHandler.ActionCompletedListeners.Remove(this);
            eventsHandler.SelfAttackedListeners.Remove(this);
            
            Owner.StatsModule.UnsubscribePower(this);
            
            if (Owner.StaminaModule.TrySome(out IStaminaModule staminaModule))
                staminaModule.BaseResilience -= ResilienceModifier;
        }

        public override PerkRecord GetRecord() => new HatredRecord(Key, _hits);

        public void OnSelfAttacked(ref ActionResult result)
        {
            if (result.Missed || Owner.LustModule.IsNone)
                return;

            Owner.LustModule.Value.ChangeLust(LustModifierPerHit);
            _hits++;
        }

        public void OnActionCompleted(ListPool<ActionResult> actionResults)
        {
            _hits = 0;
        }

        public void Modify(ref float value, CharacterStateMachine self)
        {
            value += _hits * DamageModifierPerHit;
        }
    }
}