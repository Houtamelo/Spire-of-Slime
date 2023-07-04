using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Effects.Types.Stun;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Interfaces;
using ListPool;
using Main_Database.Combat;
using Save_Management;
using UnityEngine;
using Utils.Extensions;

namespace Core.Main_Characters.Ethel.Combat.Perks.Debuffer
{
    public class NoQuarters : PerkScriptable
    {
        public override PerkInstance CreateInstance(CharacterStateMachine character)
        {
            NoQuartersInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record NoQuartersRecord(CleanString Key) : PerkRecord(Key)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (PerkDatabase.GetPerk(Key).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(NoQuartersRecord), " data. ", nameof(Key), " with key: ", Key.ToString(), " does not exist in database.");
                return false;
            }
            
            return true;
        }

        public override PerkInstance CreateInstance(CharacterStateMachine owner, CharacterEnumerator allCharacters)
        {
            NoQuartersInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class NoQuartersInstance : PerkInstance, IStunModifier, ISkillModifier
    {
        private const float BonusDamagePerDebuffStack = 0.075f;
        private const float MaxDamage = 0.3f;
        private const float BonusDamageIfStunned = 0.1f;
        private const float JoltDurationModifier = 0.5f;
        public string SharedId => nameof(NoQuartersInstance);
        public int Priority => 0;
        
        public NoQuartersInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }
        
        public NoQuartersInstance(CharacterStateMachine owner, NoQuartersRecord record) : base(owner, record)
        {
        }

        protected override void OnSubscribe()
        {
            Owner.SkillModule.SkillModifiers.Add(this);
            Owner.StatusApplierModule.StunApplyModifiers.Add(this);
        }

        protected override void OnUnsubscribe()
        {
            Owner.SkillModule.SkillModifiers.Remove(this);
            Owner.StatusApplierModule.StunApplyModifiers.Remove(this);
        }

        public override PerkRecord GetRecord() => new NoQuartersRecord(Key);

        public void Modify(ref StunToApply effectStruct)
        {
            if (effectStruct.FromSkill == false || effectStruct.GetSkill().Key != EthelSkills.Jolt)
                return;

            effectStruct.Duration += JoltDurationModifier;
        }
        
        public void Modify(ref SkillStruct skillStruct)
        {
            ref ValueListPool<TargetProperties> targetProperties = ref skillStruct.TargetProperties;
            int count = targetProperties.Count;
            for (int i = 0; i < count; i++)
            {
                ref TargetProperties property = ref targetProperties[i];
                CharacterStateMachine target = property.Target;
                
                float extraDamage = 0;
                foreach (StatusInstance status in target.StatusModule.GetAll)
                    if (status.EffectType is EffectType.Debuff && status.IsActive)
                        extraDamage += BonusDamagePerDebuffStack;
                
                extraDamage = Mathf.Min(extraDamage, MaxDamage);
                
                if (property.DamageModifier.IsNone)
                    continue;

                float damageMultiplier = property.DamageModifier.Value;
                damageMultiplier += extraDamage;
                
                if (target.StunModule.GetRemaining() > 0)
                    damageMultiplier += BonusDamageIfStunned;
                
                property.DamageModifier = damageMultiplier;
#if UNITY_EDITOR
                Debug.Assert(property.DamageModifier == damageMultiplier, $"property.DamageModifier: {property.DamageModifier}, damageMultiplier: {damageMultiplier}");
                Debug.Assert(property == targetProperties[i], $"property: {property}, targetProperties[i]: {targetProperties[i]}");
                Debug.Assert(skillStruct.TargetProperties[i] == property, $"skillStruct.TargetProperties[i]: {skillStruct.TargetProperties[i]}, property: {property}");
#endif
            }
        }
    }
}