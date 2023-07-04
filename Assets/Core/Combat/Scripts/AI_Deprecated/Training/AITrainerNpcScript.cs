/*using System;
using System.Collections.Generic;
using Core.Combat.Scripts.Animations;
using Core.Combat.Scripts.Barks;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Interfaces;
using Core.Combat.Scripts.Perks;
using Core.Combat.Scripts.Skills.Interfaces;
using Save_Management;
using UnityEngine;
using Utils.Patterns;

namespace Core.Combat.Scripts.AI.Training
{
    public class AITrainerNpcScript : ICharacterScript
    {
        public byte Size { get; }
        public GameObject RendererPrefab => null;
        public Race Race => default;
        public Sprite TimelineIcon => null;
        public Option<RuntimeAnimatorController> GetPortraitAnimation => Option<RuntimeAnimatorController>.None;
        public Option<Color> GetPortraitBackgroundColor => Option<Color>.None;
        public Option<Sprite> GetPortrait => Option<Sprite>.None;
        public Sprite LustPromptPortrait => null;
        public CleanString Key { get; }
        public int Stamina { get; }
        public int StaminaAmplitude { get; }
        public float Resilience { get; }
        public float PoisonResistance { get; }
        public float PoisonApplyChance { get; }
        public float DebuffResistance { get; }
        public float DebuffApplyChance { get; }
        public float MoveResistance { get; }
        public float MoveApplyChance { get; }
        public float Speed { get; }
        public float StunRecoverySpeed { get; }
        public float Accuracy { get; }
        public float Critical { get; }
        public float Dodge { get; }
        public string CharacterName { get; }
        public IReadOnlyList<ISkill> Skills { get; }
        public (int lower, int upper) Damage { get; }
        public bool IsControlledByPlayer => false;
        public bool IsGirl { get; }
        public float ExpMultiplier { get; }
        public ReadOnlySpan<IPerk> GetStartingPerks => ReadOnlySpan<IPerk>.Empty;
        //public float TemptationResistance { get; }
        public ClampedPercentage Temptation { get; }
        public float Composure { get; }
        public int OrgasmLimit { get; }
        public int OrgasmCount { get; }
        public int Lust { get; }
        public float DownedTime { get; }
        public float OrgasmDuration { get; }
        public float IdleGraphicalX { get; } = 0f;
        public Option<string> GetBark(BarkType barkType, CharacterStateMachine character) => Option<string>.None;

        public Option<(string parameter, float graphicalX)> DoesActiveSex(CharacterStateMachine other) 
            => IsGirl ? Option<(string parameter, float graphicalX)>.None : Option<(string parameter, float graphicalX)>.Some(default);
        public Option<float> GetSexGraphicalX(string parameter) => 0f;

        public IReadOnlyList<ISkill> GetAllPossibleSkills() => Skills;


        public bool BecomesCorpseOnDefeat(out CombatAnimation combatAnimation)
        {
            combatAnimation = default;
            return false;
        }

        private AITrainerNpcScript(byte size, string key, int stamina, int staminaAmplitude, float resilience, float poisonResistance, float debuffResistance, float moveResistance,
                          float speed, float stunRecoverySpeed, float accuracy, float critical, float dodge, string characterName, (int lower, int upper) damage,
                          float composure, int orgasmLimit, int lust, float downedTime, float orgasmDuration, IReadOnlyList<ISkill> skills, bool isGirl, float expMultiplier)
        {
            Size = size;
            Key = key;
            Stamina = stamina;
            StaminaAmplitude = staminaAmplitude;
            Resilience = resilience;
            PoisonResistance = poisonResistance;
            DebuffResistance = debuffResistance;
            MoveResistance = moveResistance;
            Speed = speed;
            StunRecoverySpeed = stunRecoverySpeed;
            Accuracy = accuracy;
            Critical = critical;
            Dodge = dodge;
            CharacterName = characterName;
            Damage = damage;
            Composure = composure;
            OrgasmLimit = orgasmLimit;
            Lust = lust;
            DownedTime = downedTime;
            OrgasmDuration = orgasmDuration;
            Skills = skills;
            IsGirl = isGirl;
            ExpMultiplier = expMultiplier;
        }
        
        public static AITrainerNpcScript GenerateRandom(System.Random random, int maxSize, float powerLevel)
        {
            powerLevel = Mathf.Clamp(powerLevel, 0.3f, 1f);
            byte size = (byte)random.Next(1, maxSize + 1);
            int stamina = random.Next(20, (int)(100 * powerLevel));
            int staminaAmplitude = random.Next(3, (int)(15 * powerLevel));
            float resilience = random.Next(-20, (int)(50 * powerLevel)) / 100f;
            float poisonResistance = random.Next(-40, (int)(120 * powerLevel)) / 100f;
            float debuffResistance = random.Next(-40, (int)(120 * powerLevel)) / 100f;
            float moveResistance = random.Next(-40, (int)(120 * powerLevel)) / 100f;
            float speed = random.Next(50, (int)(200 * powerLevel)) / 100f;
            float stunRecoverySpeed = random.Next(50, (int)(200 * powerLevel)) / 100f;
            float accuracy = random.Next(-10, (int)(30 * powerLevel)) / 100f;
            float critical = random.Next(0, (int)(20 * powerLevel)) / 100f;
            float dodge = random.Next(0, (int)(30 * powerLevel)) / 100f;
            string characterName = $"NPC-{powerLevel}";
            int lowerDamage = random.Next(3, (int)(20 * powerLevel));
            int upperDamage = random.Next(lowerDamage, lowerDamage + (int)(12 * powerLevel));
            //float temptationResistance = random.Next(-40, (int)(120 * powerLevel)) / 100f;
            float composure = random.Next(-20, (int)(50 * powerLevel)) / 100f;
            int orgasmLimit = random.Next(2, (int)(5 * powerLevel) + 1);
            int lust = random.Next(0, 100);
            float downedTime = random.Next(200, 500) / 100f;
            float orgasmDuration = random.Next(200, 500) / 100f;
            bool isGirl = random.NextDouble() > 0.66f;

            ISkill[] skills = new ISkill[4];
            for (int i = 0; i < skills.Length; i++)
            {
                int targetEffectsCount = random.Next(0, 3);
                int selfEffectsCount = random.Next(0, 3);
                skills[i] = AISkillScript.GenerateRandom(random, selfEffectsCount, targetEffectsCount);
            }

            return new AITrainerNpcScript(size, characterName, stamina, staminaAmplitude, resilience, poisonResistance, debuffResistance, moveResistance, speed, stunRecoverySpeed, accuracy,
                critical, dodge, characterName, (lowerDamage, upperDamage), composure, orgasmLimit, lust, downedTime, orgasmDuration, skills, isGirl, expMultiplier: 1f);
        }
    }
}*/