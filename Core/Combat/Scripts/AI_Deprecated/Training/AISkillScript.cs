/*using System;
using System.Collections.Generic;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using Save_Management;
using UnityEngine;
using Utils.Patterns;

namespace Core.Combat.Scripts.AI.Training
{
    public class AISkillScript : ISkill
    {
        public CleanString Key { get; }
        public string DisplayName => Key.ToString();
        public string FlavorText => "";
        public string GetEffectsText => "";
        public float BaseCharge { get; }
        public float BaseRecovery { get; }
        
        private readonly float _baseAccuracy;
        public Option<float> BaseAccuracy => _baseAccuracy;
        
        private readonly float _baseDamageMultiplier;
        public Option<float> BaseDamageMultiplier => _baseDamageMultiplier;
        
        private readonly float _baseCriticalChance;
        public Option<float> BaseCriticalChance => _baseCriticalChance;
        
        private float _baseResiliencePiercing;
        public Option<float> BaseResiliencePiercing => _baseResiliencePiercing;
        public bool CanCrit { get; }
        public PositionSetup CastingPositions { get; }
        public PositionSetup TargetPositions { get; }
        public bool MultiTarget { get; }
        public bool AllowAllies { get; }
        public Sprite IconBackground => null;
        public Sprite IconBaseSprite => null;
        public Sprite IconBaseFx => null;
        public Sprite IconHighlightedSprite => null;
        public Sprite IconHighlightedFx => null;
        public string AnimationParameter => default;
        public int AnimationId => default;
        public float CasterMovement => default;
        public AnimationCurve CasterAnimationCurve => AnimationCurve.Linear(0, 0, 1, 1);
        public float TargetMovement => default;
        public AnimationCurve TargetAnimationCurve => AnimationCurve.Linear(0, 0, 1, 1);
        public IReadOnlyList<IBaseStatusScript> TargetEffects { get; }
        public IReadOnlyList<IBaseStatusScript> CasterEffects { get; }
        public IReadOnlyList<ICustomSkillStat> CustomStats { get; } = Array.Empty<ICustomSkillStat>();
        public bool IsPositive { get; }
        public TargetType TargetType { get; }
        public Option<int> GetMaxUseCount => Option<int>.None;
        public ReadOnlyPaddingSettings GetPaddingSettings() => ActionPaddingSettings.Default();

        public AISkillScript(string key, bool isPositive, float baseCharge, float baseRecovery, float baseAccuracy, float baseDamageMultiplier, float baseCriticalChance,
                             bool canCrit, PositionSetup castingPositions, IBaseStatusScript[] targetEffects, IBaseStatusScript[] casterEffects, TargetType targetType, 
                             PositionSetup targetPositions, bool multiTarget, bool allowAllies)
        {
            Key = key;
            BaseCharge = baseCharge;
            BaseRecovery = baseRecovery;
            _baseAccuracy = baseAccuracy;
            _baseDamageMultiplier = baseDamageMultiplier;
            _baseCriticalChance = baseCriticalChance;
            CanCrit = canCrit;
            CastingPositions = castingPositions;
            TargetEffects = targetEffects;
            CasterEffects = casterEffects;
            IsPositive = isPositive;
            TargetType = targetType;
            TargetPositions = targetPositions;
            MultiTarget = multiTarget;
            AllowAllies = allowAllies;
        }
        
        public static AISkillScript GenerateRandom(System.Random random, int casterEffectCount, int targetEffectCount)
        {
            string key = $"Skill-{casterEffectCount.ToString()}-{targetEffectCount.ToString()}";
            bool isPositive = random.NextDouble() < 0.3333f;
            float baseCharge = ((float)random.NextDouble() * 2f) + 0.3f;
            float baseRecovery = ((float)random.NextDouble() * 1.5f) + 0.2f;
            float baseAccuracy = isPositive ? 1 : (float)random.NextDouble() * 0.5f + 0.5f;
            float baseDamageMultiplier = isPositive ? (float)random.NextDouble() * 0.2f : (float)random.NextDouble() * 1.3f + 0.2f;
            float baseCriticalChance = (float)random.NextDouble() * 0.2f;
            bool canCrit = random.NextDouble() > 0.5f;
            PositionSetup castingPositions = new();
            for (int i = 0; i < PositionSetup.Length; i++) 
                castingPositions[i] = random.NextDouble() > 0.5f;
            
            IBaseStatusScript[] targetEffects = new IBaseStatusScript[targetEffectCount];
            for (int i = 0; i < targetEffectCount; i++)
                targetEffects[i] = StatusGenerator.GenerateRandom(random, isPositive);
            
            IBaseStatusScript[] casterEffects = new IBaseStatusScript[casterEffectCount];
            for (int i = 0; i < casterEffectCount; i++)
                casterEffects[i] = StatusGenerator.GenerateRandom(random, isPositive);

            TargetType targetType;
            if (isPositive)
            {
                targetType = random.Next(0, 3) switch
                {
                    0 => TargetType.CanSelf,
                    1 => TargetType.NotSelf,
                    2 => TargetType.OnlySelf,
                    _ => throw new ArgumentException()
                };
            }
            else
            {
                targetType = TargetType.NotSelf;
            }
            
            return new AISkillScript(key, isPositive, baseCharge, baseRecovery, baseAccuracy, baseDamageMultiplier, baseCriticalChance, canCrit, castingPositions, targetEffects,
                casterEffects, targetType, default, default, default);
        }
    }
}*/