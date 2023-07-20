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
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Collections;
using Core.Utils.Extensions;
using Core.Utils.Math;
using JetBrains.Annotations;
using ListPool;
using UnityEngine;

namespace Core.Main_Characters.Ethel.Combat.Perks.Debuffer
{
    public class NoQuarters : PerkScriptable
    {
        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine character)
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

        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine owner, DirectCharacterEnumerator allCharacters)
        {
            NoQuartersInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class NoQuartersInstance : PerkInstance, IStunModifier, ISkillModifier
    {
        private const int BonusDamagePerDebuffStack = 7;
        private const int MaxDamage = 28;
        private const int BonusDamageIfStunned = 10;
        private const int JoltStunPowerModifier = 50;

        public NoQuartersInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }

        public NoQuartersInstance(CharacterStateMachine owner, [NotNull] NoQuartersRecord record) : base(owner, record)
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

        [NotNull]
        public override PerkRecord GetRecord() => new NoQuartersRecord(Key);

        public void Modify([NotNull] ref StunToApply effectStruct)
        {
            if (effectStruct.FromSkill == false || effectStruct.GetSkill().Key != EthelSkills.Jolt)
                return;
            
            effectStruct.StunPower += JoltStunPowerModifier;
        }

        public void Modify(ref SkillStruct skillStruct)
        {
            ref CustomValuePooledList<TargetProperties> targetProperties = ref skillStruct.TargetProperties;
            int count = targetProperties.Count;
            for (int i = 0; i < count; i++)
            {
                ref TargetProperties property = ref targetProperties[i];
                CharacterStateMachine target = property.Target;
                
                int extraDamage = 0;
                foreach (StatusInstance status in target.StatusReceiverModule.GetAll)
                {
                    if (status.EffectType is EffectType.Debuff && status.IsActive)
                        extraDamage += BonusDamagePerDebuffStack;
                }

                extraDamage = Mathf.Min(extraDamage, MaxDamage);
                
                if (property.Power.IsNone)
                    continue;

                int power = property.Power.Value;
                power = power + extraDamage;
                
                if (target.StunModule.GetRemaining().Ticks > 0)
                    power += BonusDamageIfStunned;
                
                property.Power = power;
            }
        }

        [NotNull]
        public string SharedId => nameof(NoQuartersInstance);
        public int Priority => 0;
    }
}