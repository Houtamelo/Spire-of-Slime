using System.Diagnostics.Contracts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Effects.Types.Marked;
using Core.Utils.Math;
using Core.Utils.Patterns;
using ListPool;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils.Patterns;

namespace Core.Combat.Scripts.Skills.Common_Custom_Stats
{
    [CreateAssetMenu(menuName = "Database/Combat/Skill/CustomStat/Bonus Vs Marked", fileName = "custom-stat_bonus-vs-marked")]
    public class BonusVsMarkedStat : CustomStatScriptable
    {
        [SerializeField, PropertyRange(-100, 200), LabelText("Bonus Damage"), SuffixLabel("%")]
        private int serializedBonusDamage;
        private float BonusDamagePercentage => serializedBonusDamage / 100f;
        
        [SerializeField, PropertyRange(-100, 100), LabelText("Bonus Crit"), SuffixLabel("%")]
        private int serializedBonusCriticalChance;
        private float BonusCriticalChancePercentage => serializedBonusCriticalChance / 100f;
        
        [SerializeField, PropertyRange(-100, 100), LabelText("Bonus Accuracy"), SuffixLabel("%")]
        private int serializedBonusAccuracy;
        private float BonusAccuracyPercentage => serializedBonusAccuracy / 100f;
        
        [SerializeField, PropertyRange(-100, 100), LabelText("Resilience Piercing"), SuffixLabel("%")]
        private int serializedResiliencePiercing;
        private float ResiliencePiercing => serializedResiliencePiercing / 100f;

        public override void Apply(ref SkillStruct skillStruct)
        {
            ref ValueListPool<TargetProperties> targetProperties = ref skillStruct.TargetProperties;
            int count = targetProperties.Count;
            for (int index = 0; index < count; index++)
            {
                ref TargetProperties targetProperty = ref targetProperties[index];
                CharacterStateMachine character = targetProperty.Target;
                bool hasMark = false;

                foreach (StatusInstance status in character.StatusModule.GetAll)
                {
                    if (status is not Marked { IsActive: true })
                        continue;
                    
                    hasMark = true;
                    break;
                }
                
                if (hasMark == false)
                    continue;
                
                if (targetProperty.DamageModifier.IsSome)
                    targetProperty.DamageModifier = targetProperty.DamageModifier.Value + BonusDamagePercentage;
                
                if (targetProperty.AccuracyModifier.IsSome)
                    targetProperty.AccuracyModifier = targetProperty.AccuracyModifier.Value + BonusAccuracyPercentage;
                
                if (targetProperty.CriticalChanceModifier.IsSome)
                    targetProperty.CriticalChanceModifier = targetProperty.CriticalChanceModifier.Value + BonusCriticalChancePercentage;

                if (targetProperty.ResiliencePiercingModifier.IsSome)
                    targetProperty.ResiliencePiercingModifier = targetProperty.ResiliencePiercingModifier.Value + ResiliencePiercing;

                #region Assertion
#if UNITY_EDITOR
                Debug.Assert(targetProperty == targetProperties[index],    
                             $"{targetProperty} != {targetProperties[index]}");
                Debug.Assert(targetProperty == skillStruct.TargetProperties[index],
                             $"{targetProperty} != {skillStruct.TargetProperties[index]}");
                Debug.Assert(skillStruct.TargetProperties[index].DamageModifier == targetProperty.DamageModifier,
                             $"{skillStruct.TargetProperties[index].DamageModifier} != {targetProperty.DamageModifier}");
                Debug.Assert(skillStruct.TargetProperties[index].AccuracyModifier == targetProperty.AccuracyModifier,
                             $"{skillStruct.TargetProperties[index].AccuracyModifier} != {targetProperty.AccuracyModifier}");
                Debug.Assert(skillStruct.TargetProperties[index].CriticalChanceModifier == targetProperty.CriticalChanceModifier,
                             $"{skillStruct.TargetProperties[index].CriticalChanceModifier} != {targetProperty.CriticalChanceModifier}");
                Debug.Assert(skillStruct.TargetProperties[index].ResiliencePiercingModifier == targetProperty.ResiliencePiercingModifier,
                             $"{skillStruct.TargetProperties[index].ResiliencePiercingModifier} != {targetProperty.ResiliencePiercingModifier}");
#endif

                #endregion
            }
        }

        [Pure]
        public override Option<string> GetDescription()
        {
            if (serializedBonusDamage == 0 && serializedBonusCriticalChance == 0 && serializedBonusAccuracy == 0 && serializedResiliencePiercing == 0)
                return Option<string>.None;
            
            SharedStringBuilder.Clear();
            SharedStringBuilder.AppendLine("Bonus vs Marked: ");
            if (serializedBonusDamage != 0)
                SharedStringBuilder.AppendLine($"Damage: {BonusDamagePercentage.ToPercentageString()}");
            
            if (serializedBonusCriticalChance != 0)
                SharedStringBuilder.AppendLine($"Crit: {BonusCriticalChancePercentage.ToPercentageString()}");
            
            if (serializedBonusAccuracy != 0)
                SharedStringBuilder.AppendLine($"Accuracy: {BonusAccuracyPercentage.ToPercentageString()}");
            
            if (serializedResiliencePiercing != 0)
                SharedStringBuilder.AppendLine($"Ignores Resilience: {ResiliencePiercing.ToPercentageString()}");
            
            return SharedStringBuilder.ToString();
        }
    }
}