using System;
using System.Collections.Generic;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Perks;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Utils.Patterns;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using Utils.Patterns;

namespace Core.Combat.Scripts
{
    [CreateAssetMenu(fileName = "NonGirl", menuName = "Database/Combat/NonGirl")]
    public class NonGirlScript : CharacterScriptable
    {
        [OdinSerialize, PropertyRange(1, 200)]
        private uint _stamina;
        public override uint Stamina => _stamina;
        
        [OdinSerialize] 
        private uint _staminaAmplitude;
        public override uint StaminaAmplitude => _staminaAmplitude;
        
        [OdinSerialize, SuffixLabel("%"), PropertyRange(-50, 200)]
        private int _resilience;
        public override float Resilience => _resilience / 100f;

        [OdinSerialize, SuffixLabel("%"), PropertyRange(-50, 200)]
        private int _debuffResistance;
        public override float DebuffResistance => _debuffResistance / 100f;
        
        [OdinSerialize, SuffixLabel("%"), PropertyRange(-50, 200)]
        private int _debuffApplyChance;
        public override float DebuffApplyChance => _debuffApplyChance / 100f;

        [OdinSerialize, SuffixLabel("%"), PropertyRange(-50, 200)]
        private int _moveResistance;
        public override float MoveResistance => _moveResistance / 100f;
        
        [OdinSerialize, SuffixLabel("%"), PropertyRange(-50, 200)]
        private int _moveApplyChance;
        public override float MoveApplyChance => _moveApplyChance / 100f;

        [OdinSerialize, SuffixLabel("%"), PropertyRange(-50, 200)]
        private int _poisonResistance;
        public override float PoisonResistance => _poisonResistance / 100f;

        [OdinSerialize, SuffixLabel("%"), PropertyRange(-50, 200)]
        private int _poisonApplyChance;
        public override float PoisonApplyChance => _poisonApplyChance / 100f;
        
        [OdinSerialize, SuffixLabel("%"), PropertyRange(-50, 200)]
        private int _arousalApplyChance;
        public override float ArousalApplyChance => _arousalApplyChance / 100f;

        [OdinSerialize, SuffixLabel("%"), PropertyRange(0, 200)]
        private int _speed = 1;
        public override float Speed => _speed / 100f;
        
        [OdinSerialize, SuffixLabel("%"), PropertyRange(0, 200)]
        private int _stunRecoverySpeed = 1;
        public override float StunRecoverySpeed => _stunRecoverySpeed / 100f;
        
        [OdinSerialize, SuffixLabel("%"), PropertyRange(-50, 200)]
        private int _accuracy;
        public override float Accuracy => _accuracy / 100f;
        
        [OdinSerialize, SuffixLabel("%"), PropertyRange(-50, 200)]
        private int _critical;
        public override float Critical => _critical / 100f;
        
        [OdinSerialize, SuffixLabel("%"), PropertyRange(-50, 200)]
        private int _dodge;
        public override float Dodge => _dodge / 100f;
        
        [OdinSerialize, SuffixLabel("%"), PropertyRange(0, 300)]
        private int _expMultiplier = 100;
        public override float ExpMultiplier => _expMultiplier / 100f;
        
        [OdinSerialize] private string _characterName;
        public override string CharacterName => _characterName;

        [SerializeField] 
        private SkillScriptable[] skills = new SkillScriptable[0];
        public override IReadOnlyList<ISkill> Skills => skills;

        [OdinSerialize, PropertyRange(0, 200)]
        private uint _lowerDamage;
        
        [OdinSerialize, PropertyRange(0, 200), ValidateInput(nameof(IsUpperDamageEqualOrBiggerToLower))]
        private uint _upperDamage;

        private bool IsUpperDamageEqualOrBiggerToLower() => _upperDamage >= _lowerDamage;

        [SerializeField, Required]
        private PerkScriptable[] perks;
        public override ReadOnlySpan<IPerk> GetStartingPerks => perks ?? ReadOnlySpan<IPerk>.Empty;

        public override (uint lower, uint upper) Damage => (_lowerDamage, _upperDamage);
        public override bool IsGirl => false;
        //public override float TemptationResistance => 0;

        public override uint Lust => 0;
        public override ClampedPercentage Temptation => 0;
        public override float Composure => 0;
        public override uint OrgasmLimit => 0;
        public override uint OrgasmCount => 0;

        [OdinSerialize, Required]
        private (CharacterScriptable character, string parameter, float graphicalX)[] _sexableCharacters = new (CharacterScriptable character, string parameter, float graphicalX)[0];

        public override Option<RuntimeAnimatorController> GetPortraitAnimation => Option<RuntimeAnimatorController>.None;

        public override Option<(string parameter, float graphicalX)> DoesActiveSex(CharacterStateMachine other)
        {
            foreach ((CharacterScriptable character, string parameter, float graphicalX) in _sexableCharacters)
                if (other.Script.Key == character.Key)
                    return Option<(string parameter, float graphicalX)>.Some((parameter, graphicalX));

            return Option.None;
        }

        public override Option<float> GetSexGraphicalX(string animationTrigger)
        {
            foreach ((_, string parameter, float graphicalX) in _sexableCharacters)
                if (parameter == animationTrigger)
                    return Option<float>.Some(graphicalX);
            
            return Option.None;
        }
    }
}