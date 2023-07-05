using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Interfaces;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;

namespace Core.Main_Characters.Nema.Combat.Perks.Poison
{
    public class Acceptance : PerkScriptable
    {
        public override PerkInstance CreateInstance(CharacterStateMachine character)
        {
            AcceptanceInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record AcceptanceRecord(CleanString Key, float AccumulatedTime) : PerkRecord(Key)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (PerkDatabase.GetPerk(Key).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(AcceptanceRecord), " data. ", nameof(Key), " with key: ", Key.ToString(), " does not exist in database.");
                return false;
            }
            
            return true;
        }

        public override PerkInstance CreateInstance(CharacterStateMachine owner, CharacterEnumerator allCharacters)
        {
            AcceptanceInstance instance = new(owner, this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class AcceptanceInstance : PerkInstance, ITick
    {
        private const int LustModifierPerTime = -3;

        private float _accumulatedTime;
        
        public AcceptanceInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }

        public AcceptanceInstance(CharacterStateMachine owner, AcceptanceRecord record) : base(owner, record)
        {
            _accumulatedTime = record.AccumulatedTime;
        }

        protected override void OnSubscribe()
        {
            Owner.SubscribedTickers.Add(this);
        }

        protected override void OnUnsubscribe()
        {
            Owner.SubscribedTickers.Remove(this);
        }

        public override PerkRecord GetRecord() => new AcceptanceRecord(Key, _accumulatedTime);

        public void Tick(float timeStep)
        {
            foreach (StatusInstance status in Owner.StatusModule.GetAll)
            {
                if (status.EffectType != EffectType.Poison || status.IsDeactivated)
                    continue;
                
                _accumulatedTime += timeStep;
                if (_accumulatedTime >= 1f)
                {
                    _accumulatedTime -= 1f;
                    if (Owner.LustModule.IsSome)
                        Owner.LustModule.Value.ChangeLust(LustModifierPerTime);
                }

                return;
            }
        }
    }
}