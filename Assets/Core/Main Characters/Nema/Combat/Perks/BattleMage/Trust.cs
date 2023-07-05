using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.Interfaces;
using Core.Combat.Scripts.Interfaces;
using Core.Combat.Scripts.Interfaces.Events;
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Combat.Scripts.Skills.Action;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using Utils.Patterns;

namespace Core.Main_Characters.Nema.Combat.Perks.BattleMage
{
    public class Trust : PerkScriptable
    {
        public override PerkInstance CreateInstance(CharacterStateMachine character)
        {
            TrustInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record TrustRecord(CleanString Key, int Stacks, float AccumulatedTime) : PerkRecord(Key)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (PerkDatabase.GetPerk(Key).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(TrustRecord), " data. ", nameof(Key), " with key: ", Key.ToString(), " does not exist in database.");
                return false;
            }
            
            return true;
        }

        public override PerkInstance CreateInstance(CharacterStateMachine owner, CharacterEnumerator allCharacters)
        {
            TrustInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class TrustInstance : PerkInstance, ITick, ISelfAttackedListener
    {
        private const float SpeedPerStack = 0.05f;
        private const float CriticalChancePerStack = 0.04f;
        private const float AccuracyPerStack = 0.02f;

        private readonly Reference<int> _stackCounter;
        private readonly SpeedModifier _speedModifier;
        private readonly CriticalChanceModifier _criticalChanceModifier;
        private readonly AccuracyModifier _accuracyModifier;
        private float _accumulatedTime;

        public TrustInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
            _stackCounter = new Reference<int>(0);
            _speedModifier = new SpeedModifier(_stackCounter);
            _criticalChanceModifier = new CriticalChanceModifier(_stackCounter);
            _accuracyModifier = new AccuracyModifier(_stackCounter);
        }
        
        public TrustInstance(CharacterStateMachine owner, TrustRecord record) : base(owner, record)
        {
            _stackCounter = new Reference<int>(record.Stacks);
            _speedModifier = new SpeedModifier(_stackCounter);
            _criticalChanceModifier = new CriticalChanceModifier(_stackCounter);
            _accuracyModifier = new AccuracyModifier(_stackCounter);
            _accumulatedTime = record.AccumulatedTime;
        }

        protected override void OnSubscribe()
        {
            Owner.Events.SelfAttackedListeners.Add(this);
            Owner.SubscribedTickers.Add(this);
            
            IStatsModule statsModule = Owner.StatsModule;
            statsModule.SubscribeSpeed(_speedModifier, allowDuplicates: false);
            statsModule.SubscribeCriticalChance(_criticalChanceModifier, allowDuplicates: false);
            statsModule.SubscribeAccuracy(_accuracyModifier, allowDuplicates: false);
        }

        protected override void OnUnsubscribe()
        {
            Owner.Events.SelfAttackedListeners.Remove(this);
            Owner.SubscribedTickers.Remove(this);
            
            IStatsModule statsModule = Owner.StatsModule;
            statsModule.UnsubscribeSpeed(_speedModifier);
            statsModule.UnsubscribeCriticalChance(_criticalChanceModifier);
            statsModule.UnsubscribeAccuracy(_accuracyModifier);
        }

        public override PerkRecord GetRecord() => new TrustRecord(Key, _stackCounter, _accumulatedTime);

        public void OnSelfAttacked(ref ActionResult result)
        {
            if (result.Hit && result.DamageDealt.TrySome(out uint damage) && damage > 0)
                _stackCounter.Value = 0;
        }

        public void Tick(float timeStep)
        {
            _accumulatedTime += timeStep;
            if (_accumulatedTime >= 1f)
            {
                _accumulatedTime -= 1f;
                _stackCounter.Value = _stackCounter + 1;
            }
        }

        private class SpeedModifier : IBaseFloatAttributeModifier
        {
            public int Priority => 0;
            public string SharedId => nameof(TrustInstance);
            private readonly Reference<int> _stackCounter;

            public void Modify(ref float value, CharacterStateMachine self)
            {
                value += _stackCounter * SpeedPerStack;
            }
            
            public SpeedModifier(Reference<int> stackCounter)
            {
                _stackCounter = stackCounter;
            }
        }

        private class CriticalChanceModifier : IBaseFloatAttributeModifier
        {
            public int Priority => 0;
            public string SharedId => nameof(TrustInstance);
            private readonly Reference<int> _stackCounter;

            public void Modify(ref float value, CharacterStateMachine self)
            {
                value += _stackCounter * CriticalChancePerStack;
            }
            
            public CriticalChanceModifier(Reference<int> stackCounter)
            {
                _stackCounter = stackCounter;
            }
        }

        private class AccuracyModifier : IBaseFloatAttributeModifier
        {
            public string SharedId => nameof(TrustInstance);
            public int Priority => 0;
            private readonly Reference<int> _stackCounter;

            public void Modify(ref float value, CharacterStateMachine self)
            {
                value += _stackCounter * AccuracyPerStack;
            }
            
            public AccuracyModifier(Reference<int> stackCounter)
            {
                _stackCounter = stackCounter;
            }
        }
    }
}