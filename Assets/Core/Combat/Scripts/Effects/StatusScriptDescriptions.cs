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
    public static class StatusScriptDescriptions
    {
        private static readonly StringBuilder Builder = new();

        private static readonly LocalizedText
            ArousalTrans = new("status_script_description_arousal"),            // "Lust {0}pt/s, {1} => {2}"
            BuffTrans = new("status_script_description_buff"),                  // "Buff {0} by {1}, {2} => {3}"
            DebuffTrans = new("status_script_description_debuff"),              // "Debuff {0} by {1}, {2} => {3}"
            LustGrappledTrans = new("status_script_description_lust_grappled"), // "Grapple, increasing Lust {0}pt/s, {1}"
            GuardedTrans = new("status_script_description_guarded"),            // "Guard, {0}"
            HealTrans = new("status_script_description_heal"),                  // "Heal with {0} Power"
            LustTrans = new("status_script_description_lust"),                  // "Increases Lust by {0}"
            MarkedTrans = new("status_script_description_marked"),              // "Mark, {0}"
            MoveTrans = new("status_script_description_move"),                  // "Move {0} | {1}"
            OvertimeHealTrans = new("status_script_description_overtime_heal"), // "Heal {0}pt/s, {1}"
            PerkTrans = new("status_script_description_perk"),                  // "Adds {0}, {1}\n<indent=10%>{2}</indent>"
            PoisonTrans = new("status_script_description_poison"),              // "Poison {0}pt/s, {1} => {2}"
            RiposteTrans = new("status_script_description_riposte"),            // "Riposte with {0} Power, {1}"
            StunTrans = new("status_script_description_stun"),                  // "Stun with {0} Power"
            SummonTrans = new("status_script_description_summon"),              // "Summons {0} helping {1}"
            TemptTrans = new("status_script_description_tempt");                // "Tempts with {0} Power"
        
        [NotNull]
        public static string Get([NotNull] IActualStatusScript script)
        {
            StringBuilder builder = script switch
            {
                ArousalScript a 
                    => Builder.Override(ArousalTrans.Translate().GetText(a.BaseLustPerSecond.ToString("0"), a.GetDurationString, a.BaseApplyChance.ToPercentageStringBase100()))
                              .Surround(ArousalRichText),
                BuffOrDebuffScript { IsPositive: true } b 
                    => Builder.Override(BuffTrans.Translate().GetText(b.Stat.LowerCaseName().Translate().GetText(), b.BaseDelta.WithSymbol(), b.GetDurationString, b.BaseApplyChance.ToPercentageStringBase100()))
                              .Surround(BuffRichText),
                BuffOrDebuffScript d 
                    => Builder.Override(DebuffTrans.Translate().GetText(d.Stat.LowerCaseName().Translate().GetText(), d.BaseDelta.WithSymbol(), d.GetDurationString, d.BaseApplyChance.ToPercentageStringBase100()))
                              .Surround(DebuffRichText),
                LustGrappledScript l   
                    => Builder.Override(LustGrappledTrans.Translate().GetText(l.BaseLustPerTime.ToString(), l.GetDurationString))
                              .Surround(LustRichText),
                GuardedScript g        
                    => Builder.Override(GuardedTrans.Translate().GetText(g.GetDurationString))
                              .Surround(GuardedRichText),
                HealScript h           
                    => Builder.Override(HealTrans.Translate().GetText(h.Power.ToPercentageStringBase100()))
                              .Surround(HealRichText),
                LustScript l           
                    => Builder.Override(LustTrans.Translate().GetText((l.LustLower, l.LustUpper).ToLustRangeFormat()))
                              .Surround(LustRichText),
                MarkedScript mar       
                    => Builder.Override(MarkedTrans.Translate().GetText(mar.GetDurationString))
                              .Surround(MarkedRichText),
                MoveScript mov         
                    => Builder.Override(MoveTrans.Translate().GetText(StatusUtils.MoveDeltaToString(mov.MoveDelta), mov.BaseApplyChance.ToPercentageStringBase100()))
                              .Surround(MoveRichText),
                OvertimeHealScript o   
                    => Builder.Override(OvertimeHealTrans.Translate().GetText(o.BaseHealPerTime.ToString("0"), o.GetDurationString))
                              .Surround(OvertimeHealRichText),
                PerkStatusScript per   
                    => Builder.Override(PerkTrans.Translate().GetText(per.PerkToApply.DisplayName, per.GetDurationString, per.PerkToApply.Description)),
                PoisonScript poi       
                    => Builder.Override(PoisonTrans.Translate().GetText(poi.BasePoisonPerTime.ToString("0"), poi.GetDurationString, poi.BaseApplyChance.ToPercentageStringBase100()))
                              .Surround(PoisonRichText),
                RiposteScript r        
                    => Builder.Override(RiposteTrans.Translate().GetText(r.BasePower.ToPercentageStringBase100(), r.GetDurationString))
                              .Surround(RiposteRichText),
                StunScript stun
                    => Builder.Override(StunTrans.Translate().GetText(stun.StunPower.ToString()))
                              .Surround(StunRichText),
                SummonScript sum       
                    => SummonDescription(sum),
                TemptScript tempt 
                    => Builder.Override(TemptTrans.Translate().GetText(tempt.Power.ToString()))
                              .Surround(TemptationRichText),
                _ => LogType(script)
            };

            return builder.ToString();
        }
        
        [NotNull]
        private static StringBuilder SummonDescription([NotNull] SummonScript script)
        {
            string characterName = script.CharacterToSummon.CharacterName.Translate().GetText();
            if (string.IsNullOrEmpty(characterName))
                return Builder.Override(SummonTrans.Translate().GetText("a", Enum<Race>.ToString(script.CharacterToSummon.Race)));

            string article = SummonToApply.Vowels.Contains(characterName[0]) ? "an" : "a";
            return Builder.Override(SummonTrans.Translate().GetText(article, script.CharacterToSummon.CharacterName.Translate().GetText()));
        }
        
        [NotNull]
        private static StringBuilder LogType([NotNull] this IActualStatusScript script)
        {
            Builder.Clear();
            Debug.LogWarning($"Missing description for {script.GetType()}");
            return Builder;
        }
    }
}