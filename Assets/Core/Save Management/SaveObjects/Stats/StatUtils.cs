using System;
using System.Text;
using Core.Utils.Extensions;
using Core.Utils.Math;
using UnityEngine;

namespace Core.Save_Management.SaveObjects
{
    public static class StatUtils
    {
        private static readonly StringBuilder Builder = new();
        
        public static string Format(this GeneralStat stat, float value)
        {
            return stat switch
            {
                GeneralStat.DamageLower        => value.ToString("0"),
                GeneralStat.DamageUpper        => value.ToString("0"),
                GeneralStat.OrgasmLimit        => value.ToString("0"),
                GeneralStat.MoveResistance     => value.ToPercentageString(),
                GeneralStat.Stamina            => value.ToString("0"),
                GeneralStat.Lust               => value.ToString("0"),
                GeneralStat.Temptation         => value.ToPercentageString(),
                GeneralStat.Resilience         => value.ToPercentageString(),
                GeneralStat.PoisonResistance   => value.ToPercentageString(),
                GeneralStat.DebuffResistance   => value.ToPercentageString(),
                GeneralStat.StunRecoverySpeed  => value.ToPercentageString(),
                GeneralStat.Composure          => value.ToPercentageString(),
                GeneralStat.Speed              => value.ToPercentageString(),
                GeneralStat.Accuracy           => value.ToPercentageString(),
                GeneralStat.CriticalChance     => value.ToPercentageString(),
                GeneralStat.Dodge              => value.ToPercentageString(),
                GeneralStat.Experience         => value.ToString("0.00"),
                GeneralStat.PrimaryPoints      => value.ToString("0"),
                GeneralStat.SecondaryPoints    => value.ToString("0"),
                GeneralStat.PerkPoints         => value.ToString("0"),
                GeneralStat.OrgasmCount        => value.ToString("0"),
                GeneralStat.PoisonApplyChance  => value.ToPercentageString(),
                GeneralStat.DebuffApplyChance  => value.ToPercentageString(),
                GeneralStat.MoveApplyChance    => value.ToPercentageString(),
                GeneralStat.ArousalApplyChance => value.ToPercentageString(),
                GeneralStat.Corruption         => value.ToPercentageString(),
                _                              => throw new ArgumentOutOfRangeException(nameof(stat), stat, null)
            };
        }
        
        public static string AltFormat(this GeneralStat stat, float value)
        {
            return stat switch
            {
                GeneralStat.DamageLower        => value.ToString("0"),
                GeneralStat.DamageUpper        => value.ToString("0"),
                GeneralStat.OrgasmLimit        => value.ToString("0"),
                GeneralStat.MoveResistance     => value.ToPercentlessStringWithSymbol(digits: 1, decimalDigits: 0),
                GeneralStat.Stamina            => value.ToString("0"),
                GeneralStat.Lust               => value.ToString("0"),
                GeneralStat.Temptation         => Builder.Override(Mathf.RoundToInt(value * 100f).ToString("0"), " / 100").ToString(),
                GeneralStat.Resilience         => value.ToPercentlessStringWithSymbol(digits: 1, decimalDigits: 0),
                GeneralStat.PoisonResistance   => value.ToPercentlessStringWithSymbol(digits: 1, decimalDigits: 0),
                GeneralStat.DebuffResistance   => value.ToPercentlessStringWithSymbol(digits: 1, decimalDigits: 0),
                GeneralStat.StunRecoverySpeed  => value.ToPercentageString(),
                GeneralStat.Composure          => value.ToPercentlessStringWithSymbol(digits: 1, decimalDigits: 0),
                GeneralStat.Speed              => value.ToPercentageString(),
                GeneralStat.Accuracy           => value.ToPercentlessStringWithSymbol(digits: 1, decimalDigits: 0),
                GeneralStat.CriticalChance     => value.ToPercentlessStringWithSymbol(digits: 1, decimalDigits: 0),
                GeneralStat.Dodge              => value.ToPercentlessStringWithSymbol(digits: 1, decimalDigits: 0),
                GeneralStat.Experience         => value.ToString("0.00"),
                GeneralStat.PrimaryPoints      => value.ToString("0"),
                GeneralStat.SecondaryPoints    => value.ToString("0"),
                GeneralStat.PerkPoints         => value.ToString("0"),
                GeneralStat.OrgasmCount        => value.ToString("0"),
                GeneralStat.PoisonApplyChance  => value.ToPercentlessStringWithSymbol(digits: 1, decimalDigits: 0),
                GeneralStat.DebuffApplyChance  => value.ToPercentlessStringWithSymbol(digits: 1, decimalDigits: 0),
                GeneralStat.MoveApplyChance    => value.ToPercentlessStringWithSymbol(digits: 1, decimalDigits: 0),
                GeneralStat.ArousalApplyChance => value.ToPercentlessStringWithSymbol(digits: 1, decimalDigits: 0),
                GeneralStat.Corruption         => value.ToPercentageString(),
                _                              => throw new ArgumentOutOfRangeException(nameof(stat), stat, null)
            };
        }

        public static string Format(this PrimaryUpgrade upgrade, float value)
        {
            return upgrade switch
            {
                PrimaryUpgrade.Accuracy   => value.ToPercentageString(),
                PrimaryUpgrade.Dodge      => value.ToPercentageString(),
                PrimaryUpgrade.Critical   => value.ToPercentageString(),
                PrimaryUpgrade.Resilience => value.ToPercentageString(),
                _                         => throw new ArgumentOutOfRangeException(nameof(upgrade), upgrade, null)
            };
        }

        public static string Format(this SecondaryUpgrade upgrade, float value)
        {
            return upgrade switch
            {
                SecondaryUpgrade.Composure         => value.ToPercentageString(),
                SecondaryUpgrade.StunRecoverySpeed => value.ToPercentageString(),
                SecondaryUpgrade.MoveResistance    => value.ToPercentageString(),
                SecondaryUpgrade.DebuffResistance  => value.ToPercentageString(),
                SecondaryUpgrade.PoisonResistance  => value.ToPercentageString(),
                SecondaryUpgrade.PoisonApplyChance => value.ToPercentageString(),
                SecondaryUpgrade.DebuffApplyChance => value.ToPercentageString(),
                SecondaryUpgrade.MoveApplyChance   => value.ToPercentageString(),
                _                                  => throw new ArgumentOutOfRangeException(nameof(upgrade), upgrade, null)
            };
        }

        public static GeneralStat ToGeneral(this PrimaryUpgrade upgrade)
        {
            return upgrade switch
            {
                PrimaryUpgrade.Accuracy   => GeneralStat.Accuracy,
                PrimaryUpgrade.Dodge      => GeneralStat.Dodge,
                PrimaryUpgrade.Critical   => GeneralStat.CriticalChance,
                PrimaryUpgrade.Resilience => GeneralStat.Resilience,
                _                         => throw new ArgumentOutOfRangeException(nameof(upgrade), upgrade, null)
            };
        }
        
        public static GeneralStat ToGeneral(this SecondaryUpgrade upgrade)
        {
            return upgrade switch
            {
                SecondaryUpgrade.Composure         => GeneralStat.Composure,
                SecondaryUpgrade.StunRecoverySpeed => GeneralStat.StunRecoverySpeed,
                SecondaryUpgrade.MoveResistance    => GeneralStat.MoveResistance,
                SecondaryUpgrade.DebuffResistance  => GeneralStat.DebuffResistance,
                SecondaryUpgrade.PoisonResistance  => GeneralStat.PoisonResistance,
                SecondaryUpgrade.PoisonApplyChance => GeneralStat.PoisonApplyChance,
                SecondaryUpgrade.DebuffApplyChance => GeneralStat.DebuffApplyChance,
                SecondaryUpgrade.MoveApplyChance   => GeneralStat.MoveApplyChance,
                _                                  => throw new ArgumentOutOfRangeException(nameof(upgrade), upgrade, null)
            };
        }

        public static CleanString GetTooltipKey(this GeneralStat stat)
        {
            return stat switch
            {
                GeneralStat.Experience         => "stat_tooltip_experience",
                GeneralStat.Speed              => "stat_tooltip_speed",
                GeneralStat.Stamina            => "stat_tooltip_stamina",
                GeneralStat.Resilience         => "stat_tooltip_resilience",
                GeneralStat.DamageLower        => "stat_tooltip_basedamage",
                GeneralStat.DamageUpper        => "stat_tooltip_basedamage",
                GeneralStat.Lust               => "stat_tooltip_lust",
                GeneralStat.Temptation         => "stat_tooltip_temptation",
                GeneralStat.Composure          => "stat_tooltip_composure",
                GeneralStat.OrgasmLimit        => "stat_tooltip_orgasmlimit",
                GeneralStat.OrgasmCount        => "stat_tooltip_orgasmcount",
                GeneralStat.Accuracy           => "stat_tooltip_accuracy",
                GeneralStat.CriticalChance     => "stat_tooltip_criticalchance",
                GeneralStat.Dodge              => "stat_tooltip_dodge",
                GeneralStat.StunRecoverySpeed  => "stat_tooltip_stunrecoveryspeed",
                GeneralStat.PoisonResistance   => "stat_tooltip_poisonresistance",
                GeneralStat.PoisonApplyChance  => "stat_tooltip_poisonapplychance",
                GeneralStat.DebuffResistance   => "stat_tooltip_debuffresistance",
                GeneralStat.DebuffApplyChance  => "stat_tooltip_debuffapplychance",
                GeneralStat.MoveResistance     => "stat_tooltip_moveresistance",
                GeneralStat.MoveApplyChance    => "stat_tooltip_moveapplychance",
                GeneralStat.ArousalApplyChance => "stat_tooltip_arousalapplychance",
                GeneralStat.Corruption         => "stat_tooltip_corruption",
                GeneralStat.PrimaryPoints      => string.Empty,
                GeneralStat.SecondaryPoints    => string.Empty,
                GeneralStat.PerkPoints         => string.Empty,
                _                              => throw new ArgumentOutOfRangeException(nameof(stat), stat, null)
            };
        }
    }
}