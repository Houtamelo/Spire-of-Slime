using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Effects.Types.BuffOrDebuff;
using Core.Combat.Scripts.Effects.Types.Lust;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Interfaces;
using Data.Main_Characters.Nema;
using ListPool;
using Main_Database.Combat;
using Save_Management;
using Utils.Extensions;

namespace Core.Main_Characters.Nema.Combat.Perks.Healer
{
    public class Adoration : PerkScriptable 
    {
        public override PerkInstance CreateInstance(CharacterStateMachine character)
        {
            AdorationInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record AdorationRecord(CleanString Key) : PerkRecord(Key)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (PerkDatabase.GetPerk(Key).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(AdorationRecord), " data. ", nameof(Key), " with key: ", Key.ToString(), " does not exist in database.");
                return false;
            }
            
            return true;
        }

        public override PerkInstance CreateInstance(CharacterStateMachine owner, CharacterEnumerator allCharacters)
        {
            AdorationInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class AdorationInstance : PerkInstance, ISkillModifier
    {
        public string SharedId => nameof(AdorationInstance);
        public int Priority => 0;
        
        private const float BuffModifier = 0.1f;
        private const float BaseDuration = 4f;
        private const float BaseApplyChance = 1f;
        
        private static readonly LustScript LustScript = new(-4, -5);
        private static readonly BuffOrDebuffScript ResilienceScript = new(false, BaseDuration, BaseApplyChance, CombatStat.Resilience, BuffModifier);
        private static readonly BuffOrDebuffScript ComposureScript = new(false, BaseDuration, BaseApplyChance, CombatStat.Composure, BuffModifier);

        public AdorationInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }
        
        public AdorationInstance(CharacterStateMachine owner, AdorationRecord record) : base(owner, record)
        {
        }

        protected override void OnSubscribe()
        {
            Owner.SkillModule.SkillModifiers.Add(this);
        }

        protected override void OnUnsubscribe()
        {
            Owner.SkillModule.SkillModifiers.Remove(this);
        }

        public override PerkRecord GetRecord() => new AdorationRecord(Key);

        public void Modify(ref SkillStruct skillStruct)
        {
            CleanString key = skillStruct.Skill.Key;
            
            if (key != NemaSkills.Serenity.key && key != NemaSkills.Calm.key)
                return;
            
            ref ValueListPool<IActualStatusScript> targetEffects = ref skillStruct.TargetEffects;
            targetEffects.Add(LustScript);
            targetEffects.Add(ResilienceScript);
            targetEffects.Add(ComposureScript);
        }
    }
}