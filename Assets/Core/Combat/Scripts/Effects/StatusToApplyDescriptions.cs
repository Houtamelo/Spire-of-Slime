using System.Text;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Effects.Types.Arousal;
using Core.Combat.Scripts.Effects.Types.BuffOrDebuff;
using Core.Combat.Scripts.Effects.Types.Grappled;
using Core.Combat.Scripts.Effects.Types.Guarded;
using Core.Combat.Scripts.Effects.Types.Heal;
using Core.Combat.Scripts.Effects.Types.Lust;
using Core.Combat.Scripts.Effects.Types.Marked;
using Core.Combat.Scripts.Effects.Types.Move;
using Core.Combat.Scripts.Effects.Types.OvertimeHeal;
using Core.Combat.Scripts.Effects.Types.Perk;
using Core.Combat.Scripts.Effects.Types.Poison;
using Core.Combat.Scripts.Effects.Types.Riposte;
using Core.Combat.Scripts.Effects.Types.Stun;
using Core.Combat.Scripts.Effects.Types.Summon;
using Core.Combat.Scripts.Effects.Types.Tempt;
using Core.Localization.Scripts;
using Core.Utils.Extensions;
using Core.Utils.Math;
using JetBrains.Annotations;
using KGySoft.CoreLibraries;
using UnityEngine;
using static Core.Combat.Scripts.ColorReferences;

namespace Core.Combat.Scripts.Effects
{
    public static class StatusToApplyDescriptions
    {
        private static readonly StringBuilder Builder = new();
        
        private static readonly LocalizedText
            ArousalTrans = new("status_toapply_description_arousal"),            // "Lust {0}pt/s, {1} => {2}"
            BuffTrans = new("status_toapply_description_buff"),                  // "Buff {0} by {1}, {2} => {3}"
            DebuffTrans = new("status_toapply_description_debuff"),              // "Debuff {0} by {1}, {2} => {3}"
            LustGrappledTrans = new("status_toapply_description_lust_grappled"), // "Grapple, increasing Lust {0}pt/s, {1}"
            GuardedTrans = new("status_toapply_description_guarded"),            // "Guard, {0}"
            HealTrans = new("status_toapply_description_heal"),                  // "Heal for {0} ({1} Power)"
            LustTrans = new("status_toapply_description_lust"),                  // "Increases Lust by {0}"
            MarkedTrans = new("status_toapply_description_marked"),              // "Mark, {0}"
            MoveTrans = new("status_toapply_description_move"),                  // "Move {0} | {1}"
            OvertimeHealTrans = new("status_toapply_description_overtime_heal"), // "Heal {0}pt/s, {1}"
            PerkTrans = new("status_toapply_description_perk"),                  // "Adds {0}, {1}\n<indent=10%>{2}</indent>"
            PoisonTrans = new("status_toapply_description_poison"),              // "Poison {0}pt/s, {1} => {2}"
            RiposteTrans = new("status_toapply_description_riposte"),            // "Riposte dealing {0} damage, {1} ({2} Power)"
            StunTrans = new("status_toapply_description_stun"),                  // "Stun with {0} Power"
            SummonTrans = new("status_toapply_description_summon"),              // "Summons {0} helping {1}"
            TemptTrans = new("status_toapply_description_tempt");                // "Tempts with {0} Power"
        
        [NotNull]
        public static string Get([NotNull] StatusToApply record)
        {
            StringBuilder builder = record switch
            {
                ArousalToApply a 
                    => Builder.Override(ArousalTrans.Translate().GetText(a.LustPerSecond.ToString("0"), a.GetDurationString(), a.ApplyChance.ToPercentageStringBase100()))
                              .Surround(ArousalRichText),
                BuffOrDebuffToApply { IsPositive: true } b 
                    => Builder.Override(BuffTrans.Translate().GetText(b.Stat.LowerCaseName().Translate().GetText(), b.Delta.WithSymbol(), b.GetDurationString(), b.ApplyChance.ToPercentageStringBase100()))
                              .Surround(BuffRichText),
                BuffOrDebuffToApply d 
                    => Builder.Override(DebuffTrans.Translate().GetText(d.Stat.LowerCaseName().Translate().GetText(), d.Delta.WithSymbol(), d.GetDurationString(), d.ApplyChance.ToPercentageStringBase100()))
                              .Surround(DebuffRichText),
                LustGrappledToApply l   
                    => Builder.Override(LustGrappledTrans.Translate().GetText(l.LustPerSecond.ToString(), l.GetDurationString()))
                              .Surround(LustRichText),
                GuardedToApply g        
                    => Builder.Override(GuardedTrans.Translate().GetText(g.GetDurationString()))
                              .Surround(GuardedRichText),
                HealToApply h 
                    => HealDescription(h),
                LustToApply l           
                    => Builder.Override(LustTrans.Translate().GetText((l.LustLower, l.LustUpper).ToLustRangeFormat()))
                              .Surround(LustRichText),
                MarkedToApply mar       
                    => Builder.Override(MarkedTrans.Translate().GetText(mar.GetDurationString()))
                              .Surround(MarkedRichText),
                MoveToApply mov         
                    => Builder.Override(MoveTrans.Translate().GetText(StatusUtils.MoveDeltaToString(mov.MoveDelta), mov.ApplyChance.ToPercentageStringBase100()))
                              .Surround(MoveRichText),
                OvertimeHealToApply o   
                    => Builder.Override(OvertimeHealTrans.Translate().GetText(o.HealPerSecond.ToString("0"), o.GetDurationString()))
                              .Surround(OvertimeHealRichText),
                PerkStatusToApply per
                    => Builder.Override(PerkTrans.Translate().GetText(per.PerkToApply.DisplayName, per.GetDurationString(), per.PerkToApply.Description)),
                PoisonToApply poi
                    => Builder.Override(PoisonTrans.Translate().GetText(poi.PoisonPerSecond.ToString("0"), poi.GetDurationString(), poi.ApplyChance.ToPercentageStringBase100()))
                              .Surround(PoisonRichText),
                RiposteToApply r        
                    => RiposteDescription(r),
                StunToApply stun
                    => Builder.Override(StunTrans.Translate().GetText(stun.StunPower.ToString("0")))
                              .Surround(StunRichText),
                SummonToApply sum       
                    => SummonDescription(sum),
                TemptToApply tempt 
                    => Builder.Override(TemptTrans.Translate().GetText(tempt.Power.ToString("0")))
                              .Surround(TemptationRichText),
                _ => LogWithType(record)
            };

            return builder.ToString();
        }

        [NotNull]
        private static StringBuilder HealDescription([NotNull] HealToApply h)
        {
            (int lower, int upper) heal = h.Caster.StatsModule.GetBaseDamageRaw();
            heal.lower = (heal.lower * h.Power) / 100;
            heal.upper = (heal.upper * h.Power) / 100;
            
            return Builder.Override(HealTrans.Translate().GetText(heal.ToHealRangeFormat(), h.Power.ToPercentageStringBase100()))
                          .Surround(HealRichText);
        }

        [NotNull]
        private static StringBuilder RiposteDescription([NotNull] RiposteToApply r)
        {
            int characterPower = r.Target.StatsModule.GetPower();
            (int lower, int upper) damage = r.Target.StatsModule.GetBaseDamageRaw();
            damage.lower = (damage.lower * r.Power * characterPower) / 10000;
            damage.upper = (damage.upper * r.Power * characterPower) / 10000;
            
            return Builder.Override(RiposteTrans.Translate().GetText(damage.ToDamageRangeFormat(), r.GetDurationString(), r.Power.ToPercentageStringBase100()))
                          .Surround(RiposteRichText);
        }

        private static StringBuilder LogWithType([NotNull] StatusToApply record)
        {
            Debug.LogWarning($"Missing description for {record.GetType()}");
            return Builder;
        }

        [NotNull]
        private static StringBuilder SummonDescription([NotNull] SummonToApply toApply)
        {
            string characterName = toApply.CharacterToSummon.CharacterName.Translate().GetText();
            if (string.IsNullOrEmpty(characterName))
                return Builder.Override(SummonTrans.Translate().GetText("a", Enum<Race>.ToString(toApply.CharacterToSummon.Race)));

            string article = SummonToApply.Vowels.Contains(characterName[0]) ? "an" : "a";
            return Builder.Override(SummonTrans.Translate().GetText(article, toApply.CharacterToSummon.CharacterName.Translate().GetText()));
        }
        
        private static readonly LocalizedText
            ArousalCompactTrans = new("status_compact_toapply_description_arousal"),            // "+Lust {0}pt/s {1} > {2}"
            BuffCompactTrans = new("status_compact_toapply_description_buff"),                  // "{0} {1} {2} > {3}"
            DebuffCompactTrans = new("status_compact_toapply_description_debuff"),              // "{0} {1} {2} > {3}"
            LustGrappledCompactTrans = new("status_compact_toapply_description_lust_grappled"), // "Grapple {0}Lust/s {1}"
            GuardedCompactTrans = new("status_compact_toapply_description_guarded"),            // "Guard {0}"
            HealCompactTrans = new("status_compact_toapply_description_heal"),                  // "Heal {0}"
            LustCompactTrans = new("status_compact_toapply_description_lust"),                  // "+Lust {0}"
            MarkedCompactTrans = new("status_compact_toapply_description_marked"),              // "Mark {0}"
            MoveCompactTrans = new("status_compact_toapply_description_move"),                  // "Move {0} | {1}"
            OvertimeHealCompactTrans = new("status_compact_toapply_description_overtime_heal"), // "Heal {0}pt/s {1}"
            PerkCompactTrans = new("status_compact_toapply_description_perk"),                  // "{0} {1}"
            PoisonCompactTrans = new("status_compact_toapply_description_poison"),              // "Poison {0}pt/s {1} > {2}"
            RiposteCompactTrans = new("status_compact_toapply_description_riposte"),            // "Riposte {0} {1}"
            StunCompactTrans = new("status_compact_toapply_description_stun"),                  // "Stun {0} Pow"
            SummonCompactTrans = new("status_compact_toapply_description_summon"),              // "Summon {0}"
            TemptCompactTrans = new("status_compact_toapply_description_tempt");                // "Tempt {0} Pow"
        
        [NotNull]
        public static string GetCompact([NotNull] StatusToApply record)
        {
            StringBuilder builder = record switch
            {
                ArousalToApply a 
                    => Builder.Override(ArousalCompactTrans.Translate().GetText(a.LustPerSecond.ToString("0"), a.GetCompactDurationString(), a.ApplyChance.ToPercentageStringBase100()))
                              .Surround(ArousalRichText),
                BuffOrDebuffToApply { IsPositive: true } b 
                    => Builder.Override(BuffCompactTrans.Translate().GetText(b.Stat.CompactLowerCaseName().Translate().GetText(), b.Delta.WithSymbol(), b.GetCompactDurationString(), b.ApplyChance.ToPercentageStringBase100()))
                              .Surround(BuffRichText),
                BuffOrDebuffToApply d 
                    => Builder.Override(DebuffCompactTrans.Translate().GetText(d.Stat.CompactLowerCaseName().Translate().GetText(), d.Delta.WithSymbol(), d.GetCompactDurationString(), d.ApplyChance.ToPercentageStringBase100()))
                              .Surround(DebuffRichText),
                LustGrappledToApply l   
                    => Builder.Override(LustGrappledCompactTrans.Translate().GetText(l.LustPerSecond.ToString(), l.GetCompactDurationString()))
                              .Surround(LustRichText),
                GuardedToApply g        
                    => Builder.Override(GuardedCompactTrans.Translate().GetText(g.GetCompactDurationString()))
                              .Surround(GuardedRichText),
                HealToApply h 
                    => CompactHealDescription(h),
                LustToApply l           
                    => Builder.Override(LustCompactTrans.Translate().GetText((l.LustLower, l.LustUpper).ToLustRangeFormat()))
                              .Surround(LustRichText),
                MarkedToApply mar       
                    => Builder.Override(MarkedCompactTrans.Translate().GetText(mar.GetCompactDurationString()))
                              .Surround(MarkedRichText),
                MoveToApply mov         
                    => Builder.Override(MoveCompactTrans.Translate().GetText(StatusUtils.MoveDeltaToString(mov.MoveDelta), mov.ApplyChance.ToPercentageStringBase100()))
                              .Surround(MoveRichText),
                OvertimeHealToApply o   
                    => Builder.Override(OvertimeHealCompactTrans.Translate().GetText(o.HealPerSecond.ToString("0"), o.GetCompactDurationString()))
                              .Surround(OvertimeHealRichText),
                PerkStatusToApply per   
                    => Builder.Override(PerkCompactTrans.Translate().GetText(per.PerkToApply.DisplayName, per.GetCompactDurationString())),
                PoisonToApply poi 
                    => Builder.Override(PoisonCompactTrans.Translate().GetText(poi.PoisonPerSecond.ToString("0"), poi.GetCompactDurationString(), poi.ApplyChance.ToPercentageStringBase100()))
                              .Surround(PoisonRichText),
                RiposteToApply r        
                    => CompactRiposteDescription(r),
                StunToApply stun
                    => Builder.Override(StunCompactTrans.Translate().GetText(stun.StunPower.ToString("0")))
                              .Surround(StunRichText),
                SummonToApply sum       
                    => CompactSummonDescription(sum),
                TemptToApply tempt 
                    => Builder.Override(TemptCompactTrans.Translate().GetText(tempt.Power.ToString("0")))
                              .Surround(TemptationRichText),
                _ => LogWithType(record)
            };

            return builder.ToString();
        }

        [NotNull]
        private static StringBuilder CompactHealDescription([NotNull] HealToApply h)
        {
            (int lower, int upper) damage = h.Caster.StatsModule.GetBaseDamageRaw();
            damage.lower = (damage.lower * h.Power) / 100;
            damage.upper = (damage.upper * h.Power) / 100;
            
            return Builder.Override(HealCompactTrans.Translate().GetText(damage.ToHealRangeFormat()))
                          .Surround(HealRichText);
        }

        [NotNull]
        private static StringBuilder CompactRiposteDescription([NotNull] RiposteToApply r)
        {
            int characterPower = r.Target.StatsModule.GetPower();
            (int lower, int upper) damage = r.Target.StatsModule.GetBaseDamageRaw();
            damage.lower = (damage.lower * r.Power * characterPower) / 10000;
            damage.upper = (damage.upper * r.Power * characterPower) / 10000;
            
            return Builder.Override(RiposteCompactTrans.Translate().GetText(damage.ToDamageRangeFormat(), r.GetDurationString()))
                          .Surround(RiposteRichText);
        }
        
        [NotNull]
        private static StringBuilder CompactSummonDescription([NotNull] SummonToApply toApply)
        {
            string characterName = toApply.CharacterToSummon.CharacterName.Translate().GetText();
            LocalizedText nameTranslation = characterName.IsNone() ? toApply.CharacterToSummon.Race.UpperCaseName() : toApply.CharacterToSummon.CharacterName;
            return Builder.Override(SummonCompactTrans.Translate().GetText(nameTranslation.Translate().GetText()));
        }
    }
    
}