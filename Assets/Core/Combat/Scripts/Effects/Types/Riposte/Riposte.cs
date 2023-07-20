using System;
using System.Buffers;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Localization.Scripts;
using Core.Utils.Collections;
using Core.Utils.Math;
using JetBrains.Annotations;
using ListPool;
using UnityEngine;
using Core.Utils.Patterns;

// ReSharper disable UseArrayEmptyMethod

namespace Core.Combat.Scripts.Effects.Types.Riposte
{
    public class Riposte : StatusInstance
    {
        private const string Param_Riposte = "Riposte";
        private const float BaseDelay = 0.5f;
        public static float Delay => BaseDelay * IActionSequence.DurationMultiplier;

        public readonly int Power;
        public readonly ISkill Skill;

        private Riposte(TSpan duration, bool permanent, [NotNull] CharacterStateMachine owner, int power) : base(duration, permanent, owner)
        {
            Power = power;
            Skill = GenerateRiposteSkill(owner);
        }

        public static Option<StatusInstance> CreateInstance(TSpan duration, bool isPermanent, CharacterStateMachine owner, CharacterStateMachine caster, int ripostePower)
        {
            if (duration.Ticks <= 0 && isPermanent == false)
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                Debug.LogWarning($"Invalid parameters for {nameof(Riposte)}. Duration: {duration.Seconds.ToString()}, Permanent: {isPermanent.ToString()}");
                return Option.None;
            }
            
            Riposte instance = new(duration, isPermanent, owner, ripostePower);
            owner.StatusReceiverModule.AddStatus(instance, caster);
            return Option<StatusInstance>.Some(instance);
        }

        public Riposte([NotNull] RiposteRecord record, [NotNull] CharacterStateMachine owner) : base(record, owner)
        {
            Power = record.Power;
            Skill = GenerateRiposteSkill(owner);
        }

        [MustUseReturnValue]
        public CustomValuePooledList<ActionResult> Activate(CharacterStateMachine target)
        {
            SkillStruct skillStruct = SkillStruct.CreateInstance(Skill, Owner, target);
            skillStruct.ApplyCustomStats();
            Owner.SkillModule.ModifySkill(ref skillStruct);
            ref CustomValuePooledList<TargetProperties> targetProperties = ref skillStruct.TargetProperties;
            int count = targetProperties.Count;
            for (int index = 0; index < count; index++)
            {
                ref TargetProperties property = ref targetProperties[index];
                Option<int> power = property.Power;
                if (power.IsNone)
                {
                    Debug.LogWarning("Riposte skill has no damage modifier!");
                    continue;
                }
                
                property.Power = (power.Value * Power) / 100;
            }

            SkillCalculator.DoToCaster(ref skillStruct);
            CustomValuePooledList<ActionResult> results = new(count);
            for (int index = 0; index < count; index++)
            {
                ReadOnlyProperties property = targetProperties[index].ToReadOnly();
                results.Add(SkillCalculator.DoToTarget(ref skillStruct, in property, isRiposte: true));
            }

            skillStruct.Dispose();
            return results;
        }

        [NotNull]
        public override StatusRecord GetRecord() => new RiposteRecord(Duration, Permanent, Power);

        [NotNull]
        private ISkill GenerateRiposteSkill([NotNull] CharacterStateMachine owner) =>
            new TemporarySkill
            {
                Key = $"skill_riposte_{owner.Script.Key}",
                DisplayName = LocalizedText.Empty,
                FlavorText = LocalizedText.Empty,
                Charge = TSpan.Zero,
                Recovery = TSpan.Zero,
                Accuracy = 75,
                Power = 100,
                CriticalChance = 0,
                ResilienceReduction = Option<int>.None,
                CastingPositions = new PositionSetup(one: true, two: true, three: true, four: true),
                TargetPositions = new PositionSetup(one: true,  two: true, three: true, four: true),
                MultiTarget = false,
                IsPositive = false,
                IconBackground = null,
                IconBaseSprite = null,
                IconBaseFx = null,
                IconHighlightedSprite = null,
                IconHighlightedFx = null,
                AnimationParameter = Param_Riposte,
                CasterMovement = 0,
                CasterAnimationCurve = AnimationCurve.Linear(timeStart: 0f, valueStart: 0f, timeEnd: 1f, valueEnd: 1f),
                TargetMovement = 0,
                TargetAnimationCurve = AnimationCurve.Linear(timeStart: 0f, valueStart: 0f, timeEnd: 1f, valueEnd: 1f),
                targetEffects = Array.Empty<IBaseStatusScript>(),
                casterEffects = Array.Empty<IBaseStatusScript>(),
                customStats = Array.Empty<ICustomSkillStat>(),
                TargetType = TargetType.NotSelf,
                GetMaxUseCount = Option<int>.None,
                PaddingSettings = ActionPaddingSettings.Default()
            };

        public override EffectType EffectType => EffectType.Riposte;
        public override Option<string> GetDescription() => StatusInstanceDescriptions.Get(this);
        public override bool IsPositive => true;
        public const int GlobalId = Poison.Poison.GlobalId + 1;
    }
    
}