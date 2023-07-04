using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using JetBrains.Annotations;
using ListPool;
using NetFabric.Hyperlinq;
using UnityEngine;

// ReSharper disable UseArrayEmptyMethod

namespace Core.Combat.Scripts.Effects.Types.Riposte
{
    public record RiposteRecord(float Duration, bool IsPermanent, float Power) : StatusRecord(Duration, IsPermanent)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters) => true;
    }

    public class Riposte : StatusInstance
    {
        private const string Param_Riposte = "Riposte";
        private const float BaseDelay = 0.5f;
        public static float Delay => BaseDelay * IActionSequence.DurationMultiplier;

        public override bool IsPositive => true;

        public readonly float Power;
        public readonly ISkill Skill;

        private Riposte(float duration, bool isPermanent, CharacterStateMachine owner, float power) : base(duration: duration, isPermanent: isPermanent, owner: owner)
        {
            Power = power;
            Skill = GenerateRiposteSkill(owner);
        }

        public static Utils.Patterns.Option<StatusInstance> CreateInstance(float duration, bool isPermanent, CharacterStateMachine owner, CharacterStateMachine caster, float riposteMultiplier)
        {
            if (duration <= 0 && !isPermanent)
            {
                Debug.LogWarning($"Invalid parameters for {nameof(Riposte)}. Duration: {duration.ToString()}, IsPermanent: {isPermanent.ToString()}");
                return Utils.Patterns.Option<StatusInstance>.None;
            }
            
            Riposte instance = new(duration, isPermanent, owner, riposteMultiplier);
            owner.StatusModule.AddStatus(instance, caster);
            return Utils.Patterns.Option<StatusInstance>.Some(instance);
        }

        private Riposte(RiposteRecord record, CharacterStateMachine owner) : base(record, owner)
        {
            Power = record.Power;
            Skill = GenerateRiposteSkill(owner);
        }

        public static Utils.Patterns.Option<StatusInstance> CreateInstance(RiposteRecord record, CharacterStateMachine owner)
        {
            Riposte instance = new(record, owner);
            owner.StatusModule.AddStatus(instance, owner);
            return Utils.Patterns.Option<StatusInstance>.Some(instance);
        }

        [MustUseReturnValue]
        public Lease<ActionResult> Activate(CharacterStateMachine target)
        {
            SkillStruct skillStruct = SkillStruct.CreateInstance(Skill, Owner, target);
            skillStruct.ApplyCustomStats();
            Owner.SkillModule.ModifySkill(ref skillStruct);
            ref ValueListPool<TargetProperties> targetProperties = ref skillStruct.TargetProperties;
            int count = targetProperties.Count;
            for (int index = 0; index < count; index++)
            {
                ref TargetProperties property = ref targetProperties[index];
                Utils.Patterns.Option<float> damageModifierOption = property.DamageModifier;
                if (damageModifierOption.IsNone)
                {
                    Debug.LogWarning("Riposte skill has no damage modifier!");
                    continue;
                }
                
                property.DamageModifier = damageModifierOption.Value * Power;
#if UNITY_EDITOR
                Debug.Assert(property == targetProperties[index],                               $"Property not properly modified: {property} != {targetProperties[index]}");
                Debug.Assert(property.DamageModifier == targetProperties[index].DamageModifier, $"DamageModifier not properly modified: {property.DamageModifier} != {targetProperties[index].DamageModifier}");
#endif
            }

            SkillUtils.DoToCaster(ref skillStruct);
            Lease<ActionResult> results = ArrayPool<ActionResult>.Shared.Lease(count);
            for (int index = 0; index < count; index++)
            {
                ReadOnlyProperties property = targetProperties[index].ToReadOnly();
                results.Rented[index] = SkillUtils.DoToTarget(ref skillStruct, in property, isRiposte: true);
            }

            skillStruct.Dispose();
            return results;
        }


        public override StatusRecord GetRecord() => new RiposteRecord(Duration, IsPermanent, Power);

        private ISkill GenerateRiposteSkill(CharacterStateMachine owner)
        {
            return new TemporarySkill
            {
                Key = $"skill_riposte_{owner.Script.Key}",
                DisplayName = Param_Riposte,
                FlavorText = string.Empty,
                BaseCharge = 0,
                BaseRecovery = 0,
                BaseAccuracy = 0.75f,
                BaseDamageMultiplier = 1f,
                BaseCriticalChance = 0f,
                BaseResiliencePiercing = Utils.Patterns.Option<float>.None,
                CastingPositions = new PositionSetup(true, true, true, true),
                TargetPositions = new PositionSetup(true,  true, true, true),
                MultiTarget = false,
                AllowAllies = false,
                IconBackground = null,
                IconBaseSprite = null,
                IconBaseFx = null,
                IconHighlightedSprite = null,
                IconHighlightedFx = null,
                AnimationParameter = Param_Riposte,
                CasterMovement = 0,
                CasterAnimationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f),
                TargetMovement = 0,
                TargetAnimationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f),
                TargetEffects = Array.Empty<StatusScript>(),
                CasterEffects = Array.Empty<StatusScript>(),
                CustomStats = Array.Empty<ICustomSkillStat>(),
                TargetType = TargetType.NotSelf,
                GetMaxUseCount = Utils.Patterns.Option<uint>.None,
                PaddingSettings = ActionPaddingSettings.Default()
            };
        }


        public override EffectType EffectType => EffectType.Riposte;
        public override Utils.Patterns.Option<string> GetDescription() => StatusInstanceDescriptions.Get(this);
        public const int GlobalId = Poison.Poison.GlobalId + 1;
    }
    
}