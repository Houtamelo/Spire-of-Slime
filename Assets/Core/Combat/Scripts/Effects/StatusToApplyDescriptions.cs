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
using Core.Utils.Extensions;
using Core.Utils.Math;
using KGySoft.CoreLibraries;
using UnityEngine;
using static Core.Combat.Scripts.ColorReferences;

namespace Core.Combat.Scripts.Effects
{
    public static class StatusToApplyDescriptions
    {
        private static readonly StringBuilder Builder = new();
        
        public static string Get(StatusToApply record)
        {
            StringBuilder builder = record switch
            {
                ArousalToApply a      => Builder.Override("Lust ", a.LustPerTime.ToString(), "pts, ", a.GetDurationString(), " => ", a.ApplyChance.ToPercentageString()).Surround(ArousalRichText),
                BuffOrDebuffToApply b => Builder.Override(b.IsPositive ? "Buff" : "Debuff", " ", b.Stat.LowerCaseName(), " ", b.Delta.ToPercentageWithSymbol(), ", ", b.GetDurationString(), " => ", b.ApplyChance.ToPercentageString())
                    .Surround(b.IsPositive ? BuffRichText : DebuffRichText),
                LustGrappledToApply l   => Builder.Override("Grapple, increasing Lust ", l.LustPerTime.ToString(), "pts, ", l.GetDurationString()).Surround(LustRichText),
                GuardedToApply g        => Builder.Override("Guard, ",                   g.GetDurationString()).Surround(GuardedRichText),
                HealToApply h           => HealDescription(h),
                LustToApply l           => Builder.Override("Lust ",   l.LustLower.ToString("0"), " - ", l.LustUpper.ToString("0")).Surround(LustRichText),
                MarkedToApply mar       => Builder.Override("Mark, ",  mar.GetDurationString()).Surround(MarkedRichText),
                MoveToApply mov         => Builder.Override("Move ",   StatusUtils.MoveDeltaToString(mov.MoveDelta), " | ",   mov.ApplyChance.ToPercentageString()).Surround(MoveRichText),
                OvertimeHealToApply o   => Builder.Override("Heal ",   o.HealPerTime.ToString(),                     "pts, ", o.GetDurationString()).Surround(OvertimeHealRichText),
                PerkStatusToApply per   => Builder.Override("Adds ",   per.PerkToApply.DisplayName,                  ", ",    per.GetDurationString(), "\n    ", per.PerkToApply.Description),
                PoisonToApply poi       => Builder.Override("Poison ", poi.PoisonPerTime.ToString(),                 "pts, ", poi.GetDurationString(), " => ",   poi.ApplyChance.ToPercentageString()).Surround(PoisonRichText),
                RiposteToApply r        => RiposteDescription(r),
                StunToApply stun        => Builder.Override("Stun, ", stun.GetDurationString()).Surround(StunRichText),
                SummonToApply sum       => GetEnglishCorrectSummonName(sum),
                TemptToApply tempt => Builder.Override("Tempt with ", tempt.Power.ToPercentageString()).Surround(TemptationRichText),
                _                       => record.LogWithType()
            };

            return builder.ToString();
        }

        private static StringBuilder HealDescription(HealToApply h)
        {
            (float lower, float upper) damage = h.Caster.StatsModule.GetDamageWithMultiplier();
            damage.lower *= h.Power;
            damage.upper *= h.Power;
            int lowerHeal = Mathf.CeilToInt(damage.lower);
            int upperHeal = Mathf.CeilToInt(damage.upper);
            return Builder.Override("Heal for ", lowerHeal.ToString(), " - ", upperHeal.ToString(), " (", h.Power.ToPercentageString(), " Power)").Surround(HealRichText);
        }

        private static StringBuilder RiposteDescription(RiposteToApply r)
        {
            (float lower, float upper) damage = r.Target.StatsModule.GetDamageWithMultiplier();
            damage.lower *= r.Power;
            damage.upper *= r.Power;
            
            int actualLower = Mathf.CeilToInt(damage.lower);
            int actualUpper = Mathf.CeilToInt(damage.upper);
            return Builder.Override("Riposte ", actualLower.ToString("0"), " - ", actualUpper.ToString("0"), ", ", r.GetDurationString(), " (", r.Power.ToPercentageString(), " Power)").Surround(RiposteRichText);
        }

        private static StringBuilder LogWithType(this StatusToApply record)
        {
            Debug.LogWarning($"Missing description for {record.GetType()}");
            return Builder;
        }

        private static StringBuilder GetEnglishCorrectSummonName(SummonToApply record)
        {
            string characterName = record.CharacterToSummon.CharacterName;
            if (string.IsNullOrEmpty(characterName))
                return Builder.Override("Summons a helping ", Enum<Race>.ToString(record.CharacterToSummon.Race));

            string article = SummonToApply.Vowels.Contains(characterName[0]) ? "an" : "a";
            return Builder.Override("Summons ", article, " helping ", record.CharacterToSummon.CharacterName);
        }
        
        public static string GetCompact(StatusToApply record)
        {
            StringBuilder builder = record switch
            {
                ArousalToApply a      => Builder.Override("Lust ", a.LustPerTime.ToString("0"), "pt, ", a.GetCompactDurationString(), " => ", a.ApplyChance.ToPercentageString()).Surround(ArousalRichText),
                BuffOrDebuffToApply b => Builder.Override(b.IsPositive ? "Buff" : "Debuff", " ", b.Stat.CompactLowerCaseName(), " ", b.Delta.ToPercentageString(), ", ", b.GetCompactDurationString(), " => ", b.ApplyChance.ToPercentageString())
                    .Surround(b.IsPositive ? BuffRichText : DebuffRichText),
                LustGrappledToApply l   => Builder.Override("Grapple ", l.LustPerTime.ToString("0"), "pt, ", l.GetCompactDurationString()).Surround(LustRichText),
                GuardedToApply g        => Builder.Override("Guard, ",  g.GetCompactDurationString()).Surround(GuardedRichText),
                HealToApply h           => CompactHealDescription(h),
                LustToApply l           => Builder.Override("Lust ",   l.LustLower.ToString("0"), " - ", l.LustUpper.ToString("0")).Surround(LustRichText),
                MarkedToApply mar       => Builder.Override("Mark, ",  mar.GetCompactDurationString()).Surround(MarkedRichText),
                MoveToApply mov         => Builder.Override("Move ",   StatusUtils.MoveDeltaToString(mov.MoveDelta), " | ",  mov.ApplyChance.ToPercentageString()).Surround(MoveRichText),
                OvertimeHealToApply o   => Builder.Override("Heal ",   o.HealPerTime.ToString("0"),                  "pt, ", o.GetCompactDurationString()).Surround(OvertimeHealRichText),
                PerkStatusToApply per   => Builder.Override("Adds ",   per.PerkToApply.DisplayName,                  ", ",   per.GetCompactDurationString()),
                PoisonToApply poi       => Builder.Override("Poison ", poi.PoisonPerTime.ToString("0"),              "pt, ", poi.GetCompactDurationString(), " => ", poi.ApplyChance.ToPercentageString()).Surround(PoisonRichText),
                RiposteToApply r        => CompactRiposteDescription(r),
                StunToApply stun        => Builder.Override("Stun, ",   stun.GetCompactDurationString()).Surround(StunRichText),
                SummonToApply sum       => Builder.Override("Summons ", Enum<Race>.ToString(sum.CharacterToSummon.Race)),
                TemptToApply tempt => Builder.Override("Tempt ",   tempt.Power.ToPercentageString()).Surround(TemptationRichText),
                _                       => record.LogWithType()
            };

            return builder.ToString();
        }

        private static StringBuilder CompactHealDescription(HealToApply h)
        {
            (float lower, float upper) damage = h.Caster.StatsModule.GetDamageWithMultiplier();
            damage.lower *= h.Power;
            damage.upper *= h.Power;
            int lowerHeal = Mathf.CeilToInt(damage.lower);
            int upperHeal = Mathf.CeilToInt(damage.upper);
            return Builder.Override("Heal ", lowerHeal.ToString("0"), " - ", upperHeal.ToString("0")).Surround(HealRichText);
        }

        private static StringBuilder CompactRiposteDescription(RiposteToApply r)
        {
            (float lower, float upper) damage = r.Target.StatsModule.GetDamageWithMultiplier();
            damage.lower *= r.Power;
            damage.upper *= r.Power;
            
            int actualLower = Mathf.CeilToInt(damage.lower);
            int actualUpper = Mathf.CeilToInt(damage.upper);
            return Builder.Override("Riposte ", actualLower.ToString("0"), " - ", actualUpper.ToString("0"), ", ", r.GetCompactDurationString()).Surround(RiposteRichText);
        }
    }
    
}