﻿using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Behaviour.Modules;
using Core.Combat.Scripts.Effects;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Effects.Interfaces;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using JetBrains.Annotations;

namespace Core.Main_Characters.Nema.Combat.Perks.BattleMage
{
    public class Carefree : PerkScriptable
    {
        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine character)
        {
            CarefreeInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record CarefreeRecord(CleanString Key) : PerkRecord(Key)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (PerkDatabase.GetPerk(Key).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(CarefreeRecord), " data. ", nameof(Key), " with key: ", Key.ToString(), " does not exist in database.");
                return false;
            }
            
            return true;
        }

        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine owner, DirectCharacterEnumerator allCharacters)
        {
            CarefreeInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }

    public class CarefreeInstance : PerkInstance
    {
        private const int AccuracyModifier = 15;
        private const int DodgeModifier = 10;
        
        private static readonly AccuracyModifierEffect AccuracyModifierInstance = new();
        private static readonly DodgeModifierEffect DodgeModifierInstance = new();
        
        public CarefreeInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }
        
        public CarefreeInstance(CharacterStateMachine owner, [NotNull] CarefreeRecord record) : base(owner, record)
        {
        }

        protected override void OnSubscribe()
        {
            IStatsModule statsModule = Owner.StatsModule;
            statsModule.SubscribeAccuracy(AccuracyModifierInstance, allowDuplicates: false);
            statsModule.SubscribeDodge(DodgeModifierInstance, allowDuplicates: false);
        }

        protected override void OnUnsubscribe()
        {
            IStatsModule statsModule = Owner.StatsModule;
            statsModule.UnsubscribeAccuracy(AccuracyModifierInstance);
            statsModule.UnsubscribeDodge(DodgeModifierInstance);
        }

        [NotNull]
        public override PerkRecord GetRecord() => new CarefreeRecord(Key);

        private class AccuracyModifierEffect : IBaseAttributeModifier
        {
            [NotNull]
            public string SharedId => nameof(CarefreeInstance);
            public int Priority => 0;
            public void Modify(ref int value, [NotNull] CharacterStateMachine self)
            {
                foreach (StatusInstance status in self.StatusReceiverModule.GetAll)
                {
                    if (status.EffectType == EffectType.Poison && status.IsActive)
                        return;
                }

                value += AccuracyModifier;
            }
        }
        
        private class DodgeModifierEffect : IBaseAttributeModifier
        {
            [NotNull]
            public string SharedId => nameof(CarefreeInstance);
            public int Priority => 0;
            public void Modify(ref int value, [NotNull] CharacterStateMachine self)
            {
                foreach (StatusInstance status in self.StatusReceiverModule.GetAll)
                {
                    if (status.EffectType == EffectType.Poison && status.IsActive)
                        return;
                }

                value += DodgeModifier;
            }
        }
    }
}