using System;
using System.Collections.Generic;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Behaviour.Modules;
using Core.Combat.Scripts.Perks;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Localization.Scripts;
using Core.Utils.Patterns;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Core.Combat.Scripts
{
    [CreateAssetMenu(fileName = "NonGirl", menuName = "Database/Combat/NonGirl")]
    public class NonGirlScript : CharacterScriptable
    {
        [SerializeField, Range(1, 200)]
        private int stamina;
        public override int Stamina => stamina;
        
        [SerializeField, Range(0, 50)]
        private int staminaAmplitude;
        public override int StaminaAmplitude => staminaAmplitude;
        
        [SerializeField, Range(IStaminaModule.MinResilience, IStaminaModule.MaxResilience)]
        private int resilience;
        public override int Resilience => resilience;

        [SerializeField, Range(IResistancesModule.MinResistance, IResistancesModule.MaxResistance)]
        private int debuffResistance;
        public override int DebuffResistance => debuffResistance;
        
        [SerializeField, Range(IStatusApplierModule.MinDebuffApplyChance, IStatusApplierModule.MaxDebuffApplyChance)]
        private int debuffApplyChance;
        public override int DebuffApplyChance => debuffApplyChance;

        [SerializeField, Range(IResistancesModule.MinResistance, IResistancesModule.MaxResistance)]
        private int moveResistance;
        public override int MoveResistance => moveResistance;
        
        [SerializeField, Range(IStatusApplierModule.MinMoveApplyChance, IStatusApplierModule.MaxMoveApplyChance)]
        private int moveApplyChance;
        public override int MoveApplyChance => moveApplyChance;

        [SerializeField, Range(IResistancesModule.MinResistance, IResistancesModule.MaxResistance)]
        private int poisonResistance;
        public override int PoisonResistance => poisonResistance;

        [SerializeField, Range(IStatusApplierModule.MinPoisonApplyChance, IStatusApplierModule.MaxPoisonApplyChance)]
        private int poisonApplyChance;
        public override int PoisonApplyChance => poisonApplyChance;
        
        [SerializeField, Range(IStatusApplierModule.MinArousalApplyChance, IStatusApplierModule.MaxArousalApplyChance)]
        private int arousalApplyChance;
        public override int ArousalApplyChance => arousalApplyChance;

        [SerializeField, Range(IStatsModule.MinSpeed, IStatsModule.MaxSpeed)]
        private int speed = 100;
        public override int Speed => speed;
        
        [SerializeField, Range(IStunModule.MinStunMitigation, IStunModule.MaxStunMitigation)]
        private int stunMitigation;
        public override int StunMitigation => stunMitigation;
        
        [SerializeField, Range(IStatsModule.MinAccuracy, IStatsModule.MaxAccuracy)]
        private int accuracy;
        public override int Accuracy => accuracy;
        
        [SerializeField, Range(IStatsModule.MinCriticalChance, IStatsModule.MaxCriticalChance)]
        private int criticalChance;
        public override int CriticalChance => criticalChance;
        
        [SerializeField, Range(IStatsModule.MinDodge, IStatsModule.MaxDodge)]
        private int dodge;
        public override int Dodge => dodge;
        
        [SerializeField, Range(0, 3)]
        private double expMultiplier = 1;
        public override double ExpMultiplier => expMultiplier;
        
        [SerializeField]
        private LocalizedText characterName;
        public override LocalizedText CharacterName => characterName;

        [SerializeField, Required] 
        private SkillScriptable[] skills = new SkillScriptable[0];
        public override ReadOnlySpan<ISkill> Skills => skills;

        [SerializeField, Range(0, 200)]
        private int lowerDamage;
        
        [SerializeField, Range(0, 200), ValidateInput(nameof(IsUpperDamageEqualOrBiggerToLower))]
        private int upperDamage;

        public override (int lower, int upper) Damage => (lowerDamage, upperDamage);
        
        private bool IsUpperDamageEqualOrBiggerToLower() => upperDamage >= lowerDamage;

        [SerializeField, Required]
        private PerkScriptable[] perks;
        public override ReadOnlySpan<IPerk> GetStartingPerks => perks ?? ReadOnlySpan<IPerk>.Empty;

        public override bool IsGirl => false;

        public override int Lust => 0;
        public override int Temptation => 0;
        public override int Composure => 0;
        public override int OrgasmLimit => 0;
        public override int OrgasmCount => 0;

        [OdinSerialize, Required]
        private (CharacterScriptable character, string parameter, float graphicalX)[] _sexableCharacters = new (CharacterScriptable character, string parameter, float graphicalX)[0];

        public override Option<RuntimeAnimatorController> GetPortraitAnimation => Option<RuntimeAnimatorController>.None;

        public override Option<(string parameter, float graphicalX)> DoesActiveSex(CharacterStateMachine other)
        {
            foreach ((CharacterScriptable character, string parameter, float graphicalX) in _sexableCharacters)
            {
                if (other.Script.Key == character.Key)
                    return Option<(string parameter, float graphicalX)>.Some((parameter, graphicalX));
            }

            return Option.None;
        }

        public override Option<float> GetSexGraphicalX(string animationTrigger)
        {
            foreach ((_, string parameter, float graphicalX) in _sexableCharacters)
            {
                if (parameter == animationTrigger)
                    return Option<float>.Some(graphicalX);
            }

            return Option.None;
        }
    }
}