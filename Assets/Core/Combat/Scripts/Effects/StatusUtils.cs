using System;
using System.Collections.Generic;
using System.Linq;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Behaviour.UI;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Effects.Types.Arousal;
using Core.Combat.Scripts.Effects.Types.BuffOrDebuff;
using Core.Combat.Scripts.Effects.Types.Grappled;
using Core.Combat.Scripts.Effects.Types.Guarded;
using Core.Combat.Scripts.Effects.Types.Heal;
using Core.Combat.Scripts.Effects.Types.Lust;
using Core.Combat.Scripts.Effects.Types.Marked;
using Core.Combat.Scripts.Effects.Types.Move;
using Core.Combat.Scripts.Effects.Types.Perk;
using Core.Combat.Scripts.Effects.Types.Poison;
using Core.Combat.Scripts.Effects.Types.Riposte;
using Core.Combat.Scripts.Effects.Types.Stun;
using Core.Combat.Scripts.Effects.Types.Summon;
using Core.Combat.Scripts.Effects.Types.Tempt;
using Core.Combat.Scripts.Enums;
using Core.Localization.Scripts;
using Core.Utils.Extensions;
using Core.Utils.Math;
using Core.Utils.Patterns;
using JetBrains.Annotations;
using KGySoft.CoreLibraries;

namespace Core.Combat.Scripts.Effects
{
    public static class StatusUtils
    {
        private static readonly LocalizedText PermanentDurationTrans =        new(key: "status_duration_string_permanent");                 // "Permanent"
        private static readonly LocalizedText CompactPermanentDurationTrans = new(key: "status_duration_string_compact_permanent"); // "permanent"
        
        public static string GetPermanentDurationString()        => PermanentDurationTrans.Translate().GetText();
        public static string GetCompactPermanentDurationString() => CompactPermanentDurationTrans.Translate().GetText();
        
        private static readonly LocalizedText SecondsDurationTrans =        new(key: "status_duration_string_seconds"); // "for {0}s"
        private static readonly LocalizedText CompactSecondsDurationTrans = new(key: "status_duration_string_compact_seconds"); // "for {0}s"
        
        public static string GetDurationString(TSpan duration)        => SecondsDurationTrans.Translate().GetText(duration.Seconds.ToString("0.00"));
        public static string GetCompactDurationString(TSpan duration) => CompactSecondsDurationTrans.Translate().GetText(duration.Seconds.ToString("0.0"));
        
        private static readonly Dictionary<EffectType, LocalizedText> EffectType_UpperCaseNames =
            Enum<EffectType>.GetValues().ToDictionary(keySelector: effectType => effectType, elementSelector: effectType => new LocalizedText("status_name_uppercase_" + effectType.ToStringNonAlloc().ToLowerInvariant().Trim()));
        
        public static LocalizedText UpperCaseName(this EffectType stat) => EffectType_UpperCaseNames[stat];
        
        [NotNull]
        public static string MoveDeltaToString(int delta)
        {
            return delta switch
            {
                >= 3  => "<<<",
                2     => "<<",
                1     => "<",
                0     => "-",
                -1    => ">",
                -2    => ">>",
                <= -3 => ">>>"
            };
        }

        public static bool DoesRecordsHaveSameStats([CanBeNull] StatusToApply one, [CanBeNull] StatusToApply two)
        {
            bool isOneNull = ReferenceEquals(one, null);
            bool isTwoNull = ReferenceEquals(two, null);
            if (isOneNull && isTwoNull)
                return true;

            if (isOneNull || isTwoNull)
                return false;
            
            if (one.GetType() != two.GetType())
                return false;

            return one.ScriptOrigin.EffectType switch
            {
                EffectType.Buff when one is BuffOrDebuffToApply buffOrDebuffOne && two is BuffOrDebuffToApply buffOrDebuffTwo 
                    => CompareBuffOrDebuffRecords(buffOrDebuffOne, buffOrDebuffTwo),
                EffectType.Debuff when one is BuffOrDebuffToApply buffOrDebuffOne && two is BuffOrDebuffToApply buffOrDebuffTwo
                    => CompareBuffOrDebuffRecords(buffOrDebuffOne, buffOrDebuffTwo),
                EffectType.Poison when one is PoisonToApply poisonOne && two is PoisonToApply poisonTwo      
                    => ComparePoisonRecords(poisonOne, poisonTwo),
                EffectType.Arousal when one is ArousalToApply arousalOne && two is ArousalToApply arousalTwo 
                    => CompareArousalRecords(arousalOne, arousalTwo),
                EffectType.Riposte when one is RiposteToApply riposteOne && two is RiposteToApply riposteTwo 
                    => CompareRiposteRecords(riposteOne, riposteTwo),
                EffectType.OvertimeHeal when one is HealToApply healOne && two is HealToApply healTwo        
                    => CompareHealRecords(healOne, healTwo),
                EffectType.Marked when one is MarkedToApply markedOne && two is MarkedToApply markedTwo     
                    => CompareMarkedRecords(markedOne, markedTwo),
                EffectType.Stun when one is StunToApply stunOne && two is StunToApply stunTwo               
                    => CompareStunRecords(stunOne, stunTwo),
                EffectType.Guarded when one is GuardedToApply guardedOne && two is GuardedToApply guardedTwo 
                    => CompareGuardedRecords(guardedOne, guardedTwo),
                EffectType.Move when one is MoveToApply moveOne && two is MoveToApply moveTwo               
                    => CompareMoveRecords(moveOne, moveTwo),
                EffectType.LustGrappled when one is LustGrappledToApply lustGrappledOne && two is LustGrappledToApply lustGrappledTwo 
                    => CompareLustGrappledRecords(lustGrappledOne, lustGrappledTwo),
                EffectType.Perk or EffectType.HiddenPerk when one is PerkStatusToApply perkOne && two is PerkStatusToApply perkTwo
                    => ComparePerkRecords(perkOne, perkTwo),
                EffectType.Heal when one is HealToApply healOne && two is HealToApply healTwo            
                    => CompareHealRecords(healOne, healTwo),
                EffectType.Lust when one is LustToApply lustOne && two is LustToApply lustTwo            
                    => CompareLustRecords(lustOne, lustTwo),
                EffectType.Summon when one is SummonToApply summonOne && two is SummonToApply summonTwo  
                    => CompareSummonRecords(summonOne, summonTwo),
                EffectType.Temptation when one is TemptToApply temptationOne && two is TemptToApply temptationTwo
                    => CompareTemptationRecords(temptationOne, temptationTwo),
                _ => false
            };
        }

        private static bool CompareBuffOrDebuffRecords([NotNull] BuffOrDebuffToApply buffOrDebuffOne, [NotNull] BuffOrDebuffToApply buffOrDebuffTwo) =>
            buffOrDebuffOne.Permanent == buffOrDebuffTwo.Permanent
            && buffOrDebuffOne.Stat == buffOrDebuffTwo.Stat
            && buffOrDebuffOne.Duration == buffOrDebuffTwo.Duration
            && buffOrDebuffOne.ApplyChance == buffOrDebuffTwo.ApplyChance
            && buffOrDebuffOne.Delta == buffOrDebuffTwo.Delta;

        private static bool ComparePoisonRecords([NotNull] PoisonToApply poisonOne, [NotNull] PoisonToApply poisonTwo) =>
            poisonOne.Permanent == poisonTwo.Permanent
            && poisonOne.Duration == poisonTwo.Duration
            && poisonOne.ApplyChance == poisonTwo.ApplyChance
            && poisonOne.PoisonPerSecond == poisonTwo.PoisonPerSecond;

        private static bool CompareArousalRecords([NotNull] ArousalToApply one, [NotNull] ArousalToApply two) =>
            one.Permanent == two.Permanent
            && one.Duration == two.Duration
            && one.ApplyChance == two.ApplyChance
            && one.LustPerSecond == two.LustPerSecond;

        private static bool CompareRiposteRecords([NotNull] RiposteToApply one, [NotNull] RiposteToApply two) =>
            one.Permanent == two.Permanent
            && one.Duration == two.Duration
            && one.Power == two.Power;

        private static bool CompareHealRecords([NotNull] HealToApply one, [NotNull] HealToApply two) =>
            one.Power == two.Power;

        private static bool CompareMarkedRecords([NotNull] MarkedToApply one, [NotNull] MarkedToApply two) =>
            one.Permanent == two.Permanent 
            && one.Duration == two.Duration;
        
        private static bool CompareStunRecords([NotNull] StunToApply one, [NotNull] StunToApply two) =>
            one.Permanent == two.Permanent
            && one.StunPower == two.StunPower;

        private static bool CompareGuardedRecords([NotNull] GuardedToApply one, [NotNull] GuardedToApply two) =>
            one.Permanent == two.Permanent
            && one.Duration == two.Duration;

        private static bool CompareMoveRecords([NotNull] MoveToApply one, [NotNull] MoveToApply two) =>
            one.ApplyChance == two.ApplyChance
            && one.MoveDelta == two.MoveDelta;
        
        private static bool CompareLustGrappledRecords([NotNull] LustGrappledToApply one, [NotNull] LustGrappledToApply two) =>
            one.Permanent == two.Permanent
            && one.Duration == two.Duration
            && one.LustPerSecond == two.LustPerSecond;
        
        private static bool ComparePerkRecords([NotNull] PerkStatusToApply one, [NotNull] PerkStatusToApply two) =>
            one.Permanent == two.Permanent
            && one.Duration == two.Duration
            && one.PerkToApply == two.PerkToApply
            && one.IsHidden == two.IsHidden;
        
        private static bool CompareLustRecords([NotNull] LustToApply one, [NotNull] LustToApply two) =>
            one.LustPower == two.LustPower
            && one.LustLower == two.LustLower
            && one.LustUpper == two.LustUpper;

        private static bool CompareSummonRecords([NotNull] SummonToApply one, [NotNull] SummonToApply two) =>
            one.CharacterToSummon == two.CharacterToSummon;

        private static bool CompareTemptationRecords([NotNull] TemptToApply one, [NotNull] TemptToApply two) =>
            one.Power == two.Power;

        public static void Subscribe([NotNull] this BuffOrDebuff instance)
        {
            CharacterStateMachine owner = instance.Owner;
            switch (instance.Attribute)
            {
                case CombatStat.DebuffResistance:   owner.ResistancesModule.SubscribeDebuffResistance(instance, allowDuplicates: true);     break;
                case CombatStat.PoisonResistance:   owner.ResistancesModule.SubscribePoisonResistance(instance, allowDuplicates: true);     break;
                case CombatStat.MoveResistance:     owner.ResistancesModule.SubscribeMoveResistance(instance, allowDuplicates: true);       break;
                case CombatStat.DebuffApplyChance:  owner.StatusApplierModule.SubscribeDebuffApplyChance(instance, allowDuplicates: true);  break;
                case CombatStat.PoisonApplyChance:  owner.StatusApplierModule.SubscribePoisonApplyChance(instance, allowDuplicates: true);  break;
                case CombatStat.MoveApplyChance:    owner.StatusApplierModule.SubscribeMoveApplyChance(instance, allowDuplicates: true);    break;
                case CombatStat.ArousalApplyChance: owner.StatusApplierModule.SubscribeArousalApplyChance(instance, allowDuplicates: true); break;
                case CombatStat.StunMitigation:     owner.StunModule.SubscribeStunMitigation(instance, allowDuplicates: true);              break;
                case CombatStat.Accuracy:           owner.StatsModule.SubscribeAccuracy(instance, allowDuplicates: true);                   break;
                case CombatStat.CriticalChance:     owner.StatsModule.SubscribeCriticalChance(instance, allowDuplicates: true);             break;
                case CombatStat.Dodge:              owner.StatsModule.SubscribeDodge(instance, allowDuplicates: true);                      break;
                case CombatStat.DamageMultiplier:   owner.StatsModule.SubscribePower(instance, allowDuplicates: true);                      break;
                case CombatStat.Speed:              owner.StatsModule.SubscribeSpeed(instance, allowDuplicates: true);                      break;
                case CombatStat.Composure: 
                    if (owner.LustModule.IsSome)
                        owner.LustModule.Value.SubscribeComposure(instance, allowDuplicates: true);     break;
                case CombatStat.Resilience:
                    if (owner.StaminaModule.IsSome)
                        owner.StaminaModule.Value.SubscribeResilience(instance, allowDuplicates: true); break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(instance.Attribute), instance.Attribute, message: null);
            }
        }
        
        public static void Unsubscribe([NotNull] this BuffOrDebuff instance)
        {
            CharacterStateMachine owner = instance.Owner;
            switch (instance.Attribute)
            {
                case CombatStat.DebuffResistance:   owner.ResistancesModule.UnsubscribeDebuffResistance(instance);     break;
                case CombatStat.PoisonResistance:   owner.ResistancesModule.UnsubscribePoisonResistance(instance);     break;
                case CombatStat.MoveResistance:     owner.ResistancesModule.UnsubscribeMoveResistance(instance);       break;
                case CombatStat.DebuffApplyChance:  owner.StatusApplierModule.UnsubscribeDebuffApplyChance(instance);  break;
                case CombatStat.PoisonApplyChance:  owner.StatusApplierModule.UnsubscribePoisonApplyChance(instance);  break;
                case CombatStat.MoveApplyChance:    owner.StatusApplierModule.UnsubscribeMoveApplyChance(instance);    break;
                case CombatStat.ArousalApplyChance: owner.StatusApplierModule.UnsubscribeArousalApplyChance(instance); break;
                case CombatStat.StunMitigation:     owner.StunModule.UnsubscribeStunMitigation(instance);              break;
                case CombatStat.Accuracy:           owner.StatsModule.UnsubscribeAccuracy(instance);                   break;
                case CombatStat.CriticalChance:     owner.StatsModule.UnsubscribeCriticalChance(instance);             break;
                case CombatStat.Dodge:              owner.StatsModule.UnsubscribeDodge(instance);                      break;
                case CombatStat.DamageMultiplier:   owner.StatsModule.UnsubscribePower(instance);                      break;
                case CombatStat.Speed:              owner.StatsModule.UnsubscribeSpeed(instance);                      break;
                case CombatStat.Resilience:
                    if (owner.StaminaModule.IsSome)
                        owner.StaminaModule.Value.UnsubscribeResilience(instance); break;
                case CombatStat.Composure:
                    if (owner.LustModule.IsSome)
                        owner.LustModule.Value.UnsubscribeComposure(instance);     break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(instance.Attribute), instance.Attribute, message: null);
            }
        }

        public static Option<PredictionIconsDisplay.IconType> GetPredictionIcon(this EffectType effectType) =>
            effectType switch
            {
                EffectType.Buff           => PredictionIconsDisplay.IconType.Buff,
                EffectType.Debuff         => PredictionIconsDisplay.IconType.Debuff,
                EffectType.Poison         => PredictionIconsDisplay.IconType.Poison,
                EffectType.Arousal        => PredictionIconsDisplay.IconType.Lust,
                EffectType.Riposte        => PredictionIconsDisplay.IconType.Buff,
                EffectType.OvertimeHeal   => PredictionIconsDisplay.IconType.Heal,
                EffectType.Marked         => PredictionIconsDisplay.IconType.Debuff,
                EffectType.Stun           => PredictionIconsDisplay.IconType.Debuff,
                EffectType.Guarded        => PredictionIconsDisplay.IconType.Buff,
                EffectType.Move           => PredictionIconsDisplay.IconType.Debuff,
                EffectType.LustGrappled   => PredictionIconsDisplay.IconType.Lust,
                EffectType.Perk           => PredictionIconsDisplay.IconType.Buff,
                EffectType.HiddenPerk     => PredictionIconsDisplay.IconType.Buff,
                EffectType.Heal           => PredictionIconsDisplay.IconType.Heal,
                EffectType.Lust           => PredictionIconsDisplay.IconType.Lust,
                EffectType.NemaExhaustion => Option.None,
                EffectType.Mist           => Option.None,
                EffectType.Summon         => PredictionIconsDisplay.IconType.Summon,
                EffectType.Temptation     => PredictionIconsDisplay.IconType.Lust,
                _                         => throw new ArgumentOutOfRangeException(nameof(effectType), effectType, null)
            };
    }
}