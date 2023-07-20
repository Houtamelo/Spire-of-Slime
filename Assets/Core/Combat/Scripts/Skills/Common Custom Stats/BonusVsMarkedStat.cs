using System.Diagnostics.Contracts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Effects.Types.Marked;
using Core.Localization.Scripts;
using Core.Utils.Collections;
using Core.Utils.Math;
using Core.Utils.Patterns;
using ListPool;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UIElements;
using Core.Utils.Extensions;

namespace Core.Combat.Scripts.Skills.Common_Custom_Stats
{
    [CreateAssetMenu(menuName = "Database/Combat/Skill/CustomStat/Bonus Vs Marked", fileName = "custom-stat_bonus-vs-marked")]
    public class BonusVsMarkedStat : CustomStatScriptable
    {
        private static readonly LocalizedText
            LabelTrans = new("customstat_bonusvsmarked_label"),
            DamageTrans = new("customstat_bonusvsmarked_damage"),
            CriticalTrans = new("customstat_bonusvsmarked_crit"),
            AccuracyTrans = new("customstat_bonusvsmarked_accuracy"),
            ResilienceReductionTrans = new("customstat_bonusvsmarked_resiliencereduction");
        
        [SerializeField, Range(-100, 200)]
        private int bonusPower;
        
        [SerializeField, Range(-100, 100)]
        private int bonusCriticalChance;
        
        [SerializeField, Range(-100, 100)]
        private int bonusAccuracy;
        
        [SerializeField, Range(-100, 100)]
        private int bonusResilienceReduction;

        public override void Apply(ref SkillStruct skillStruct)
        {
            ref CustomValuePooledList<TargetProperties> targetProperties = ref skillStruct.TargetProperties;
            int count = targetProperties.Count;
            for (int index = 0; index < count; index++)
            {
                ref TargetProperties targetProperty = ref targetProperties[index];
                CharacterStateMachine character = targetProperty.Target;
                bool hasMark = false;

                foreach (StatusInstance status in character.StatusReceiverModule.GetAll)
                {
                    if (status is not Marked { IsActive: true })
                        continue;
                    
                    hasMark = true;
                    break;
                }
                
                if (hasMark == false)
                    continue;
                
                if (targetProperty.Power.IsSome)
                    targetProperty.Power = targetProperty.Power.Value + bonusPower;
                
                if (targetProperty.AccuracyModifier.IsSome)
                    targetProperty.AccuracyModifier = targetProperty.AccuracyModifier.Value + bonusAccuracy;
                
                if (targetProperty.CriticalChanceModifier.IsSome)
                    targetProperty.CriticalChanceModifier = targetProperty.CriticalChanceModifier.Value + bonusCriticalChance;

                if (targetProperty.ResilienceReductionModifier.IsSome)
                    targetProperty.ResilienceReductionModifier = targetProperty.ResilienceReductionModifier.Value + bonusResilienceReduction;
            }
        }

        [Pure]
        public override Option<string> GetDescription()
        {
            if (bonusPower == 0 && bonusCriticalChance == 0 && bonusAccuracy == 0 && bonusResilienceReduction == 0)
                return Option.None;
            
            SharedStringBuilder.Clear();
            SharedStringBuilder.AppendLine(LabelTrans.Translate().GetText());
            if (bonusPower != 0)
                SharedStringBuilder.AppendLine(DamageTrans.Translate().GetText(), bonusPower.WithSymbol());
            
            if (bonusCriticalChance != 0)
                SharedStringBuilder.AppendLine(CriticalTrans.Translate().GetText(), bonusCriticalChance.WithSymbol());
            
            if (bonusAccuracy != 0)
                SharedStringBuilder.AppendLine(AccuracyTrans.Translate().GetText(), bonusAccuracy.WithSymbol());
            
            if (bonusResilienceReduction != 0)
                SharedStringBuilder.AppendLine(ResilienceReductionTrans.Translate().GetText(), bonusResilienceReduction.WithSymbol());
            
            return SharedStringBuilder.ToString();
        }
    }
}