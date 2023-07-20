using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Enums;
using Core.Localization.Scripts;
using Core.Utils.Extensions;
using Core.Utils.Math;
using JetBrains.Annotations;
using KGySoft.CoreLibraries;
using UnityEngine;

namespace Core.Save_Management.SaveObjects
{
    public static class StatUtils
    {
        private static readonly StringBuilder Builder = new();
        
        [NotNull]
        public static string Format(this GeneralStat stat, int value)
        {
            return stat switch
            {
                GeneralStat.DamageLower        => value.ToString("0"),
                GeneralStat.DamageUpper        => value.ToString("0"),
                GeneralStat.OrgasmLimit        => value.ToString("0"),
                GeneralStat.MoveResistance     => value.WithSymbol(),
                GeneralStat.Stamina            => value.ToString("0"),
                GeneralStat.Lust               => value.ToString("0"),
                GeneralStat.Temptation         => value.ToPercentageStringBase100(),
                GeneralStat.Resilience         => value.ToPercentageStringBase100(),
                GeneralStat.PoisonResistance   => value.WithSymbol(),
                GeneralStat.DebuffResistance   => value.WithSymbol(),
                GeneralStat.StunMitigation     => value.WithSymbol(),
                GeneralStat.Composure          => value.WithSymbol(),
                GeneralStat.Speed              => value.ToString("0"),
                GeneralStat.Accuracy           => value.WithSymbol(),
                GeneralStat.CriticalChance     => value.WithSymbol(),
                GeneralStat.Dodge              => value.WithSymbol(),
                GeneralStat.Experience         => value.ToString("0"),
                GeneralStat.PrimaryPoints      => value.ToString("0"),
                GeneralStat.SecondaryPoints    => value.ToString("0"),
                GeneralStat.PerkPoints         => value.ToString("0"),
                GeneralStat.OrgasmCount        => value.ToString("0"),
                GeneralStat.PoisonApplyChance  => value.WithSymbol(),
                GeneralStat.DebuffApplyChance  => value.WithSymbol(),
                GeneralStat.MoveApplyChance    => value.WithSymbol(),
                GeneralStat.ArousalApplyChance => value.WithSymbol(),
                GeneralStat.Corruption         => value.ToPercentageStringBase100(),
                _                              => throw new ArgumentOutOfRangeException(nameof(stat), stat, null)
            };
        }
        
        [NotNull]
        public static string AltFormat(this GeneralStat stat, int value)
        {
            return stat switch
            {
                GeneralStat.DamageLower        => value.ToString("0"),
                GeneralStat.DamageUpper        => value.ToString("0"),
                GeneralStat.OrgasmLimit        => value.ToString("0"),
                GeneralStat.MoveResistance     => value.WithSymbol(),
                GeneralStat.Stamina            => value.ToString("0"),
                GeneralStat.Lust               => value.ToString("0"),
                GeneralStat.Temptation         => value.ToPercentageStringBase100(),
                GeneralStat.Resilience         => value.ToPercentageStringBase100(),
                GeneralStat.PoisonResistance   => value.WithSymbol(),
                GeneralStat.DebuffResistance   => value.WithSymbol(),
                GeneralStat.StunMitigation     => value.WithSymbol(),
                GeneralStat.Composure          => value.WithSymbol(),
                GeneralStat.Speed              => value.ToString("0"),
                GeneralStat.Accuracy           => value.WithSymbol(),
                GeneralStat.CriticalChance     => value.WithSymbol(),
                GeneralStat.Dodge              => value.WithSymbol(),
                GeneralStat.Experience         => value.ToString("0"),
                GeneralStat.PrimaryPoints      => value.ToString("0"),
                GeneralStat.SecondaryPoints    => value.ToString("0"),
                GeneralStat.PerkPoints         => value.ToString("0"),
                GeneralStat.OrgasmCount        => value.ToString("0"),
                GeneralStat.PoisonApplyChance  => value.WithSymbol(),
                GeneralStat.DebuffApplyChance  => value.WithSymbol(),
                GeneralStat.MoveApplyChance    => value.WithSymbol(),
                GeneralStat.ArousalApplyChance => value.WithSymbol(),
                GeneralStat.Corruption         => value.ToPercentageStringBase100(),
                _                              => throw new ArgumentOutOfRangeException(nameof(stat), stat, message: null)
            };
        }

        public static LocalizedText UpperCaseName(this PrimaryUpgrade upgrade) => upgrade.ToCombatStat().UpperCaseName();
        
        public static LocalizedText UpperCaseName(this SecondaryUpgrade upgrade) => upgrade.ToCombatStat().UpperCaseName();

        public static LocalizedText LowerCaseName(this PrimaryUpgrade upgrade) => upgrade.ToCombatStat().LowerCaseName();

        public static LocalizedText LowerCaseName(this SecondaryUpgrade upgrade) => upgrade.ToCombatStat().LowerCaseName();

        public static CombatStat ToCombatStat(this PrimaryUpgrade upgrade) =>
            upgrade switch
            {
                PrimaryUpgrade.Accuracy   => CombatStat.Accuracy,
                PrimaryUpgrade.Dodge      => CombatStat.Dodge,
                PrimaryUpgrade.Critical   => CombatStat.CriticalChance,
                PrimaryUpgrade.Resilience => CombatStat.Resilience,
                _                         => throw new ArgumentOutOfRangeException(nameof(upgrade), upgrade, message: null)
            };

        public static CombatStat ToCombatStat(this SecondaryUpgrade upgrade) =>
            upgrade switch
            {
                SecondaryUpgrade.StunMitigation    => CombatStat.StunMitigation,
                SecondaryUpgrade.MoveResistance    => CombatStat.MoveResistance,
                SecondaryUpgrade.DebuffResistance  => CombatStat.DebuffResistance,
                SecondaryUpgrade.PoisonResistance  => CombatStat.PoisonResistance,
                SecondaryUpgrade.Composure         => CombatStat.Composure,
                SecondaryUpgrade.PoisonApplyChance => CombatStat.PoisonApplyChance,
                SecondaryUpgrade.DebuffApplyChance => CombatStat.DebuffApplyChance,
                SecondaryUpgrade.MoveApplyChance   => CombatStat.MoveApplyChance,
                _                                  => throw new ArgumentOutOfRangeException(nameof(upgrade), upgrade, message: null)
            };

        [NotNull]
        public static string Format(this PrimaryUpgrade upgrade, int value)
        {
            return upgrade switch
            {
                PrimaryUpgrade.Accuracy   => value.WithSymbol(),
                PrimaryUpgrade.Dodge      => value.WithSymbol(),
                PrimaryUpgrade.Critical   => value.WithSymbol(),
                PrimaryUpgrade.Resilience => value.ToPercentageStringBase100(),
                _                         => throw new ArgumentOutOfRangeException(nameof(upgrade), upgrade, message: null)
            };
        }

        [NotNull]
        public static string Format(this SecondaryUpgrade upgrade, int value)
        {
            return upgrade switch
            {
                SecondaryUpgrade.Composure         => value.WithSymbol(),
                SecondaryUpgrade.StunMitigation    => value.WithSymbol(),
                SecondaryUpgrade.MoveResistance    => value.WithSymbol(),
                SecondaryUpgrade.DebuffResistance  => value.WithSymbol(),
                SecondaryUpgrade.PoisonResistance  => value.WithSymbol(),
                SecondaryUpgrade.PoisonApplyChance => value.WithSymbol(),
                SecondaryUpgrade.DebuffApplyChance => value.WithSymbol(),
                SecondaryUpgrade.MoveApplyChance   => value.WithSymbol(),
                _                                  => throw new ArgumentOutOfRangeException(nameof(upgrade), upgrade, message: null)
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
                SecondaryUpgrade.StunMitigation    => GeneralStat.StunMitigation,
                SecondaryUpgrade.MoveResistance    => GeneralStat.MoveResistance,
                SecondaryUpgrade.DebuffResistance  => GeneralStat.DebuffResistance,
                SecondaryUpgrade.PoisonResistance  => GeneralStat.PoisonResistance,
                SecondaryUpgrade.PoisonApplyChance => GeneralStat.PoisonApplyChance,
                SecondaryUpgrade.DebuffApplyChance => GeneralStat.DebuffApplyChance,
                SecondaryUpgrade.MoveApplyChance   => GeneralStat.MoveApplyChance,
                _                                  => throw new ArgumentOutOfRangeException(nameof(upgrade), upgrade, message: null)
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
                GeneralStat.StunMitigation     => "stat_tooltip_stunrecoveryspeed",
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
                _                              => throw new ArgumentOutOfRangeException(nameof(stat), stat, message: null)
            };
        }
    }
}