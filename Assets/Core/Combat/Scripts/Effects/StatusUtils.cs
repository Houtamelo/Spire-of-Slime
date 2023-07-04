using System;
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
using Utils.Patterns;

namespace Core.Combat.Scripts.Effects
{
    public static class StatusUtils
    {
        private const float Tolerance = 0.0001f;
        
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

        public static bool DoesRecordsHaveSameStats(StatusToApply one, StatusToApply two)
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

        private static bool CompareBuffOrDebuffRecords(BuffOrDebuffToApply buffOrDebuffOne, BuffOrDebuffToApply buffOrDebuffTwo) =>
            buffOrDebuffOne.IsPermanent == buffOrDebuffTwo.IsPermanent
            && buffOrDebuffOne.Stat == buffOrDebuffTwo.Stat
            && !(Math.Abs(buffOrDebuffOne.Duration - buffOrDebuffTwo.Duration) > Tolerance)
            && !(Math.Abs(buffOrDebuffOne.ApplyChance - buffOrDebuffTwo.ApplyChance) > Tolerance)
            && !(Math.Abs(buffOrDebuffOne.Delta - buffOrDebuffTwo.Delta) > Tolerance);

        private static bool ComparePoisonRecords(PoisonToApply poisonOne, PoisonToApply poisonTwo) =>
            poisonOne.IsPermanent == poisonTwo.IsPermanent
            && !(Math.Abs(poisonOne.Duration - poisonTwo.Duration) > Tolerance)
            && !(Math.Abs(poisonOne.ApplyChance - poisonTwo.ApplyChance) > Tolerance)
            && !(Math.Abs(poisonOne.PoisonPerTime - poisonTwo.PoisonPerTime) > Tolerance);

        private static bool CompareArousalRecords(ArousalToApply one, ArousalToApply two) =>
            one.IsPermanent == two.IsPermanent
            && !(Math.Abs(one.Duration - two.Duration) > Tolerance)
            && !(Math.Abs(one.ApplyChance - two.ApplyChance) > Tolerance)
            && one.LustPerTime == two.LustPerTime;

        private static bool CompareRiposteRecords(RiposteToApply one, RiposteToApply two) =>
            one.IsPermanent == two.IsPermanent
            && !(Math.Abs(one.Duration - two.Duration) > Tolerance)
            && Math.Abs(one.Power - two.Power) < Tolerance;

        private static bool CompareHealRecords(HealToApply one, HealToApply two) 
            => Math.Abs(one.Power - two.Power) < Tolerance;

        private static bool CompareMarkedRecords(MarkedToApply one, MarkedToApply two) =>
            one.IsPermanent == two.IsPermanent
            && !(Math.Abs(one.Duration - two.Duration) > Tolerance);
        
        private static bool CompareStunRecords(StunToApply one, StunToApply two) =>
            one.IsPermanent == two.IsPermanent
            && !(Math.Abs(one.Duration - two.Duration) > Tolerance);

        private static bool CompareGuardedRecords(GuardedToApply one, GuardedToApply two) =>
            one.IsPermanent == two.IsPermanent
            && !(Math.Abs(one.Duration - two.Duration) > Tolerance);

        private static bool CompareMoveRecords(MoveToApply one, MoveToApply two) =>
            Math.Abs(one.ApplyChance - two.ApplyChance) < Tolerance
            && one.MoveDelta == two.MoveDelta;
        
        private static bool CompareLustGrappledRecords(LustGrappledToApply one, LustGrappledToApply two) =>
            one.IsPermanent == two.IsPermanent
            && !(Math.Abs(one.Duration - two.Duration) > Tolerance)
            && one.LustPerTime == two.LustPerTime;
        
        private static bool ComparePerkRecords(PerkStatusToApply one, PerkStatusToApply two) =>
            one.IsPermanent == two.IsPermanent
            && !(Math.Abs(one.Duration - two.Duration) > Tolerance)
            && one.PerkToApply == two.PerkToApply
            && one.IsHidden == two.IsHidden;
        
        private static bool CompareLustRecords(LustToApply one, LustToApply two) =>
            Math.Abs(one.Multiplier - two.Multiplier) < Tolerance
            && one.LustLower == two.LustLower
            && one.LustUpper == two.LustUpper;

        private static bool CompareSummonRecords(SummonToApply one, SummonToApply two)
            => one.CharacterToSummon == two.CharacterToSummon;

        private static bool CompareTemptationRecords(TemptToApply one, TemptToApply two) => Math.Abs(one.Power - two.Power) < 0.0001f;

        public static void Subscribe(this BuffOrDebuff instance)
        {
            CharacterStateMachine owner = instance.Owner;
            switch (instance.Attribute)
            {
                case CombatStat.DebuffResistance:   owner.ResistancesModule.SubscribeDebuffResistance(instance, allowDuplicates: true);  break;
                case CombatStat.DebuffApplyChance:  owner.StatusApplierModule.SubscribeDebuffApplyChance(instance, allowDuplicates: true); break;
                case CombatStat.PoisonResistance:   owner.ResistancesModule.SubscribePoisonResistance(instance, allowDuplicates: true);  break;
                case CombatStat.PoisonApplyChance:  owner.StatusApplierModule.SubscribePoisonApplyChance(instance, allowDuplicates: true); break;
                case CombatStat.MoveResistance:     owner.ResistancesModule.SubscribeMoveResistance(instance, allowDuplicates: true); break;
                case CombatStat.MoveApplyChance:    owner.StatusApplierModule.SubscribeMoveApplyChance(instance, allowDuplicates: true); break;
                case CombatStat.ArousalApplyChance: owner.StatusApplierModule.SubscribeArousalApplyChance(instance, allowDuplicates: true); break;
                case CombatStat.StunSpeed:          owner.ResistancesModule.SubscribeStunRecoverySpeed(instance, allowDuplicates: true);   break;
                case CombatStat.Composure: 
                    if (owner.LustModule.IsSome)
                        owner.LustModule.Value.SubscribeComposure(instance, allowDuplicates: true); break;
                case CombatStat.Accuracy:         owner.StatsModule.SubscribeAccuracy(instance, allowDuplicates: true); break;
                case CombatStat.CriticalChance:   owner.StatsModule.SubscribeCriticalChance(instance, allowDuplicates: true);  break;
                case CombatStat.Dodge:            owner.StatsModule.SubscribeDodge(instance, allowDuplicates: true); break;
                case CombatStat.DamageMultiplier: owner.StatsModule.SubscribePower(instance, allowDuplicates: true); break;
                case CombatStat.Speed:            owner.StatsModule.SubscribeSpeed(instance, allowDuplicates: true); break;
                case CombatStat.Resilience:
                    if (owner.StaminaModule.IsSome)
                        owner.StaminaModule.Value.SubscribeResilience(instance, allowDuplicates: true); break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(instance.Attribute), instance.Attribute, null);
            }
        }
        
        public static void Unsubscribe(this BuffOrDebuff instance)
        {
            CharacterStateMachine owner = instance.Owner;
            switch (instance.Attribute)
            {
                case CombatStat.DebuffResistance:   owner.ResistancesModule.UnsubscribeDebuffResistance(instance);  break;
                case CombatStat.DebuffApplyChance:  owner.StatusApplierModule.UnsubscribeDebuffApplyChance(instance); break;
                case CombatStat.PoisonResistance:   owner.ResistancesModule.UnsubscribePoisonResistance(instance);  break;
                case CombatStat.PoisonApplyChance:  owner.StatusApplierModule.UnsubscribePoisonApplyChance(instance); break;
                case CombatStat.MoveResistance:     owner.ResistancesModule.UnsubscribeMoveResistance(instance); break;
                case CombatStat.MoveApplyChance:    owner.StatusApplierModule.UnsubscribeMoveApplyChance(instance); break;
                case CombatStat.ArousalApplyChance: owner.StatusApplierModule.UnsubscribeArousalApplyChance(instance); break;
                case CombatStat.StunSpeed:          owner.ResistancesModule.UnsubscribeStunRecoverySpeed(instance);   break;
                case CombatStat.Composure:
                    if (owner.LustModule.IsSome)
                        owner.LustModule.Value.UnsubscribeComposure(instance); break;
                case CombatStat.Accuracy:         owner.StatsModule.UnsubscribeAccuracy(instance); break;
                case CombatStat.CriticalChance:   owner.StatsModule.UnsubscribeCriticalChance(instance);  break;
                case CombatStat.Dodge:            owner.StatsModule.UnsubscribeDodge(instance); break;
                case CombatStat.DamageMultiplier: owner.StatsModule.UnsubscribePower(instance); break;
                case CombatStat.Speed:            owner.StatsModule.UnsubscribeSpeed(instance); break;
                case CombatStat.Resilience:
                    if (owner.StaminaModule.IsSome)
                        owner.StaminaModule.Value.UnsubscribeResilience(instance); break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(instance.Attribute), instance.Attribute, null);
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