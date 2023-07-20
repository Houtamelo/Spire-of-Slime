using System;
using System.Collections.Generic;
using System.Linq;
using Core.Utils.Extensions;
using KGySoft.CoreLibraries;
using Core.Utils.Collections.Extensions;
using JetBrains.Annotations;

namespace Core.Save_Management.SaveObjects
{
    public static class UpgradeHelper
    {
        private const int MinPrimaryOptions = 2, MaxPrimaryOptions = 3;
        private const int MinSecondaryOptions = 3, MaxSecondaryOptions = 5;
        
        private static readonly PrimaryUpgrade[] AllPrimaryOptions = Enum<PrimaryUpgrade>.GetValues();
        private static readonly SecondaryUpgrade[] AllSecondaryOptions = Enum<SecondaryUpgrade>.GetValues();
        
        private static readonly List<PrimaryUpgrade> ReusablePrimaryOptions = new(AllPrimaryOptions.Length);
        private static readonly List<SecondaryUpgrade> ReusableSecondaryOptions = new(AllSecondaryOptions.Length);

        public static int GetUpgradeIncrement(int tier, PrimaryUpgrade upgradeType)
        {
            if (tier == 0)
                return 0;

            double scale = Math.Pow(0.75, Math.Sqrt(tier)) + 0.25;

            int multiplier = upgradeType switch
            {
                PrimaryUpgrade.Accuracy   => 8,
                PrimaryUpgrade.Critical   => 7,
                PrimaryUpgrade.Dodge      => 8,
                PrimaryUpgrade.Resilience => 8,
                _                         => throw new ArgumentOutOfRangeException(nameof(upgradeType), upgradeType, message: null)
            };
            
            return (int)(scale * multiplier);
        }
        
        public static int GetUpgradeFull(int tier, PrimaryUpgrade upgradeType)
        {
            int sum = 0;
            for (int i = 0; i <= tier; i++)
                sum += GetUpgradeIncrement(i, upgradeType);

            return sum;
        }

        public static int GetUpgradeIncrement(int tier, SecondaryUpgrade upgradeType)
        {
            if (tier == 0)
                return 0;
            
            double scale = Math.Pow(0.75, Math.Sqrt(tier)) + 0.25;

            int multiplier = upgradeType switch
            {
                SecondaryUpgrade.Composure         => 7,
                SecondaryUpgrade.DebuffResistance  => 12,
                SecondaryUpgrade.MoveResistance    => 12,
                SecondaryUpgrade.PoisonResistance  => 12,
                SecondaryUpgrade.StunMitigation    => 10,
                SecondaryUpgrade.PoisonApplyChance => 10,
                SecondaryUpgrade.DebuffApplyChance => 10,
                SecondaryUpgrade.MoveApplyChance   => 10,
                _                                  => throw new ArgumentOutOfRangeException(nameof(upgradeType), upgradeType, null)
            };

            return (int)(scale * multiplier);
        }
        
        public static int GetUpgradeFull(int tier, SecondaryUpgrade upgradeType)
        {
            int sum = 0;
            for (int i = 0; i <= tier; i++)
                sum += GetUpgradeIncrement(i, upgradeType);
            
            return sum;
        }
        
        public static PrimaryUpgrade[] GetPrimaryUpgradeOptions(int currentTier, [NotNull] Dictionary<int, PrimaryUpgrade[]> toFill, Random randomizer)
        {
            for (int i = 0; i < currentTier; i++)
                GenerateUpgrade(i);

            return GenerateUpgrade(currentTier);

            PrimaryUpgrade[] GenerateUpgrade(int stat)
            {
                if (toFill.TryGetValue(stat, out PrimaryUpgrade[] options))
                    return options;

                ReusablePrimaryOptions.Clear();
                ReusablePrimaryOptions.Add(AllPrimaryOptions);
                int optionCount = randomizer.Next(MinPrimaryOptions, MaxPrimaryOptions + 1);
                options = new PrimaryUpgrade[optionCount];
                for (int i = 0; i < optionCount; i++)
                {
                    int index = randomizer.Next(ReusablePrimaryOptions.Count);
                    options[i] = ReusablePrimaryOptions.TakeAt(index);
                }

                toFill[stat] = options;
                return options;
            }
        }

        public static SecondaryUpgrade[] GetSecondaryUpgradeOptions(int currentStat, [NotNull] Dictionary<int, SecondaryUpgrade[]> toFill, Random randomizer)
        {
            for (int i = 0; i < currentStat; i++)
                GenerateUpgrade(i);

            return GenerateUpgrade(currentStat);

            SecondaryUpgrade[] GenerateUpgrade(int stat)
            {
                if (toFill.TryGetValue(stat, out SecondaryUpgrade[] options))
                    return options;

                ReusableSecondaryOptions.Clear();
                ReusableSecondaryOptions.Add(AllSecondaryOptions);
                int optionCount = randomizer.Next(MinSecondaryOptions, MaxSecondaryOptions + 1);
                options = new SecondaryUpgrade[optionCount];

                for (int i = 0; i < optionCount; i++)
                {
                    int index = randomizer.Next(ReusableSecondaryOptions.Count);
                    options[i] = ReusableSecondaryOptions.TakeAt(index);
                }

                toFill[stat] = options;
                return options;
            }
        }
    }
}