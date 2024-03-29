﻿using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Behaviour.Modules;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using JetBrains.Annotations;

namespace Core.Main_Characters.Nema.Combat.Perks.AoE
{
    public class Grumpiness : PerkScriptable
    {
        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine character)
        {
            GrumpinessInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record GrumpinessRecord(CleanString Key) : PerkRecord(Key)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (PerkDatabase.GetPerk(Key).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(GrumpinessRecord), " data. ", nameof(Key), " with key: ", Key.ToString(), " does not exist in database.");
                return false;
            }
            
            return true;
        }

        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine owner, DirectCharacterEnumerator allCharacters)
        {
            GrumpinessInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class GrumpinessInstance : PerkInstance
    {
        private const int BaseDamageModifier = 2;
        private const int SpeedModifier = 10;
        private const int ResilienceModifier = -10;
        private const int ComposureModifier = -10;
        
        public GrumpinessInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }
        
        public GrumpinessInstance(CharacterStateMachine owner, [NotNull] GrumpinessRecord record) : base(owner, record)
        {
        }

        protected override void OnSubscribe()
        {
            if (CreatedFromLoad)
                return;
            
            IStatsModule statsModule = Owner.StatsModule;
            statsModule.BaseDamageLower += BaseDamageModifier;
            statsModule.BaseDamageUpper += BaseDamageModifier;
            statsModule.BaseSpeed += SpeedModifier;

            if (Owner.StaminaModule.TrySome(out IStaminaModule staminaModule))
                staminaModule.BaseResilience += ResilienceModifier;
            
            if (Owner.LustModule.TrySome(out ILustModule lustModule))
                lustModule.BaseComposure += ComposureModifier;
        }

        protected override void OnUnsubscribe()
        {
            IStatsModule statsModule = Owner.StatsModule;
            statsModule.BaseDamageLower -= BaseDamageModifier;
            statsModule.BaseDamageUpper -= BaseDamageModifier;
            statsModule.BaseSpeed -= SpeedModifier;

            if (Owner.StaminaModule.TrySome(out IStaminaModule staminaModule))
                staminaModule.BaseResilience -= ResilienceModifier;
            
            if (Owner.LustModule.TrySome(out ILustModule lustModule))
                lustModule.BaseComposure -= ComposureModifier;
        }

        [NotNull]
        public override PerkRecord GetRecord() => new GrumpinessRecord(Key);
    }
}