using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Behaviour.Modules;
using Core.Combat.Scripts.Effects.Interfaces;
using Core.Combat.Scripts.Interfaces;
using Core.Combat.Scripts.Interfaces.Events;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Combat.Scripts.Skills.Action;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using Core.Utils.Math;
using JetBrains.Annotations;
using Utils.Patterns;

namespace Core.Main_Characters.Nema.Combat.Perks.BattleMage
{
    public class Trust : PerkScriptable
    {
        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine character)
        {
            TrustInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record TrustRecord(CleanString Key, int Stacks, TSpan AccumulatedTime) : PerkRecord(Key)
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

        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine owner, DirectCharacterEnumerator allCharacters)
        {
            TrustInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class TrustInstance : PerkInstance, ITick, ISelfAttackedListener
    {
        private const int SpeedPerStack = 5;
        private const int CriticalChancePerStack = 4;
        private const int AccuracyPerStack = 2;

        private readonly Reference<int> _stackCounter;
        private readonly SpeedModifier _speedModifier;
        private readonly CriticalChanceModifier _criticalChanceModifier;
        private readonly AccuracyModifier _accuracyModifier;
        
        private TSpan _accumulatedTime;

        public TrustInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
            _stackCounter = new Reference<int>(0);
            _speedModifier = new SpeedModifier(_stackCounter);
            _criticalChanceModifier = new CriticalChanceModifier(_stackCounter);
            _accuracyModifier = new AccuracyModifier(_stackCounter);
        }
        
        public TrustInstance(CharacterStateMachine owner, [NotNull] TrustRecord record) : base(owner, record)
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

        [NotNull]
        public override PerkRecord GetRecord() => new TrustRecord(Key, _stackCounter, _accumulatedTime);

        public void OnSelfAttacked(ref ActionResult result)
        {
            if (result.Hit && result.DamageDealt.TrySome(out int damage) && damage > 0)
                _stackCounter.Value = 0;
        }

        public void Tick(TSpan timeStep)
        {
            _accumulatedTime += timeStep;
            if (_accumulatedTime.Seconds >= 1f)
            {
                _accumulatedTime.SubtractSeconds(1.0);
                _stackCounter.Value = _stackCounter + 1;
            }
        }

        private class SpeedModifier : IBaseAttributeModifier
        {
            public int Priority => 0;
            [NotNull]
            public string SharedId => nameof(TrustInstance);
            private readonly Reference<int> _stackCounter;

            public void Modify(ref int value, CharacterStateMachine self)
            {
                value += _stackCounter * SpeedPerStack;
            }
            
            public SpeedModifier(Reference<int> stackCounter) => _stackCounter = stackCounter;
        }

        private class CriticalChanceModifier : IBaseAttributeModifier
        {
            public int Priority => 0;
            [NotNull]
            public string SharedId => nameof(TrustInstance);
            private readonly Reference<int> _stackCounter;

            public void Modify(ref int value, CharacterStateMachine self)
            {
                value += _stackCounter * CriticalChancePerStack;
            }
            
            public CriticalChanceModifier(Reference<int> stackCounter) => _stackCounter = stackCounter;
        }

        private class AccuracyModifier : IBaseAttributeModifier
        {
            [NotNull]
            public string SharedId => nameof(TrustInstance);
            public int Priority => 0;
            private readonly Reference<int> _stackCounter;

            public void Modify(ref int value, CharacterStateMachine self)
            {
                value += _stackCounter * AccuracyPerStack;
            }
            
            public AccuracyModifier(Reference<int> stackCounter) => _stackCounter = stackCounter;
        }
    }
}