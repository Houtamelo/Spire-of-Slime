using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Main_Database.Combat;
using Save_Management;
using Utils.Extensions;

namespace Core.Main_Characters.Nema.Combat.Perks.AoE
{
    public class Grumpiness : PerkScriptable
    {
        public override PerkInstance CreateInstance(CharacterStateMachine character)
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

        public override PerkInstance CreateInstance(CharacterStateMachine owner, CharacterEnumerator allCharacters)
        {
            GrumpinessInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class GrumpinessInstance : PerkInstance
    {
        private const int BaseDamageModifier = 2;
        private const float SpeedModifier = 0.1f;
        private const float ResilienceModifier = -0.1f;
        private const float ComposureModifier = -0.1f;
        
        public GrumpinessInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }
        
        public GrumpinessInstance(CharacterStateMachine owner, GrumpinessRecord record) : base(owner, record)
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

        public override PerkRecord GetRecord() => new GrumpinessRecord(Key);
    }
}