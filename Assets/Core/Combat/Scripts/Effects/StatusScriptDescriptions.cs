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
    public static class StatusScriptDescriptions
    {
        private static readonly StringBuilder Builder = new(); 
        
        public static string Get(IActualStatusScript script)
        {
            StringBuilder builder = script switch
            {
                ArousalScript a => Builder.Override("Lust ", a.BaseLustPerTime.ToString("0"), "pts, ", a.GetDurationString, " => ", a.BaseApplyChance.ToPercentageString()).Surround(ArousalRichText),
                BuffOrDebuffScript b => Builder.Override(b.IsPositive ? "Buff" : "Debuff", " ", b.Stat.LowerCaseName(), " by ", b.BaseDelta.ToPercentageWithSymbol(), ", ", b.GetDurationString, " => ", b.BaseApplyChance.ToPercentageString())
                    .Surround(b.IsPositive ? BuffRichText : DebuffRichText),
                LustGrappledScript l   => Builder.Override("Grapple, increasing Lust ", l.BaseLustPerTime.ToString(), "pts, ", l.GetDurationString).Surround(LustRichText),
                GuardedScript g        => Builder.Override("Guard, ",                   g.GetDurationString).Surround(GuardedRichText),
                HealScript h           => Builder.Override("Heal with ",                h.Power.ToPercentageString(), " Power").Surround(HealRichText),
                LustScript l           => Builder.Override("Lust ",                     l.LustLower.ToString("0"), " - ", l.LustUpper.ToString("0")).Surround(LustRichText),
                MarkedScript mar       => Builder.Override("Mark, ",                    mar.GetDurationString).Surround(MarkedRichText),
                MoveScript mov         => Builder.Override("Move ",                     StatusUtils.MoveDeltaToString(mov.MoveDelta), " | ", mov.BaseApplyChance.ToPercentageString()).Surround(MoveRichText),
                OvertimeHealScript o   => Builder.Override("Heal ",                     o.BaseHealPerTime.ToString("0"), "pts, ", o.GetDurationString).Surround(OvertimeHealRichText),
                PerkStatusScript per   => Builder.Override("Adds ",                     per.PerkToApply.DisplayName, ", ", per.GetDurationString, "\n    ", per.PerkToApply.Description),
                PoisonScript poi       => Builder.Override("Poison ",                   poi.BasePoisonPerTime.ToString("0"), "pts, ", poi.GetDurationString, " => ", poi.BaseApplyChance.ToPercentageString()).Surround(PoisonRichText),
                RiposteScript r        => Builder.Override("Riposte with ",             r.BasePower.ToPercentageString(), " Power, ", r.GetDurationString).Surround(RiposteRichText),
                StunScript stun        => Builder.Override("Stun, ",                    stun.GetDurationString).Surround(StunRichText),
                SummonScript sum       => GetEnglishCorrectSummonName(sum),
                TemptScript tempt => Builder.Override("Tempts with ", tempt.Power.ToPercentageString(), " Power").Surround(TemptationRichText),
                _                      => LogType(script)
            };

            return builder.ToString();
        }
        
        private static StringBuilder GetEnglishCorrectSummonName(SummonScript script)
        {
            string characterName = script.CharacterToSummon.CharacterName;
            if (string.IsNullOrEmpty(characterName))
                return Builder.Override("Summons a helping ", Enum<Race>.ToString(script.CharacterToSummon.Race));

            string article = SummonToApply.Vowels.Contains(characterName[0]) ? "an" : "a";
            return Builder.Override("Summons ", article, " helping ", script.CharacterToSummon.CharacterName);
        }
        
        private static StringBuilder LogType(this IActualStatusScript script)
        {
            Builder.Clear();
            Debug.LogWarning($"Missing description for {script.GetType()}");
            return Builder;
        }
    }
}