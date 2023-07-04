using System;
using System.Collections.Generic;
using Core.Combat.Scripts.Behaviour;
using Save_Management;

namespace Core.Combat.Scripts.Perks
{
    public abstract class PerkInstance : IEquatable<PerkInstance>
    {
        public bool Equals(PerkInstance other) => EqualityComparer<PerkInstance>.Default.Equals(this, other);
        
        public readonly CleanString Key;
        public readonly CharacterStateMachine Owner;
        protected readonly bool CreatedFromLoad;
        private bool _subscribed;
        
        protected PerkInstance(CharacterStateMachine owner, CleanString key)
        {
            Owner = owner;
            Key = key;
        }

        protected PerkInstance(CharacterStateMachine owner, PerkRecord record)
        {
            Owner = owner;
            Key = record.Key;
            CreatedFromLoad = true;
        }

        protected abstract void OnSubscribe();
        protected abstract void OnUnsubscribe();
        
        public void Subscribe()
        {
            if (_subscribed)
                return;
            
            _subscribed = true;
            OnSubscribe();
        }
        
        public void Unsubscribe()
        {
            if (_subscribed == false)
                return;
            
            _subscribed = false;
            OnUnsubscribe();
        }

        public abstract PerkRecord GetRecord();
    }
}