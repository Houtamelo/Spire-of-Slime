using System;
using System.Collections.Generic;
using System.Linq;
using Core.Utils.Extensions;
using KGySoft.CoreLibraries;

namespace Core.Save_Management.SaveObjects
{
    public static class UpgradeHelper
    {
        private const int MinPrimaryOptions = 2, MaxPrimaryOptions = 3;
        private const int MinSecondaryOptions = 3, MaxSecondaryOptions = 5;

        public static float GetUpgradeIncrement(uint tier, PrimaryUpgrade upgradeType)
        {
            if (tier == 0)
                return 0;

            double scale = Math.Pow(0.75, Math.Sqrt(tier)) + 0.25;
            double value = scale * upgradeType switch
            {
                PrimaryUpgrade.Accuracy   => 0.08,
                PrimaryUpgrade.Critical   => 0.07,
                PrimaryUpgrade.Dodge      => 0.08,
                PrimaryUpgrade.Resilience => 0.08,
                _                         => throw new ArgumentOutOfRangeException(nameof(upgradeType), upgradeType, null)
            };
            
            value = Math.Round(value, 2);
            return (float) value;
        }
        
        public static float GetUpgradeFull(uint tier, PrimaryUpgrade upgradeType)
        {
            double sum = 0;
            for (uint i = 0; i <= tier; i++)
                sum += GetUpgradeIncrement(i, upgradeType);

            return (float) sum;
        }

        public static float GetUpgradeIncrement(uint tier, SecondaryUpgrade upgradeType)
        {
            if (tier == 0)
                return 0;
            
            double scale = Math.Pow(0.75, Math.Sqrt(tier)) + 0.25;
            double value = scale * upgradeType switch
            {
                SecondaryUpgrade.Composure         => 0.07,
                SecondaryUpgrade.DebuffResistance  => 0.12,
                SecondaryUpgrade.MoveResistance    => 0.12,
                SecondaryUpgrade.PoisonResistance  => 0.12,
                SecondaryUpgrade.StunRecoverySpeed => 0.12,
                SecondaryUpgrade.PoisonApplyChance => 0.1,
                SecondaryUpgrade.DebuffApplyChance => 0.1,
                SecondaryUpgrade.MoveApplyChance   => 0.1,
                _                                  => throw new ArgumentOutOfRangeException(nameof(upgradeType), upgradeType, null)
            };
            
            value = Math.Round(value, 2);
            return (float) value;
        }
        
        public static float GetUpgradeFull(uint tier, SecondaryUpgrade upgradeType)
        {
            double sum = 0;
            for (uint i = 0; i <= tier; i++)
                sum += GetUpgradeIncrement(i, upgradeType);
            
            return (float) sum;
        }
        
        public static List<PrimaryUpgrade> GetPrimaryUpgradeOptions(uint currentTier, Dictionary<uint, List<PrimaryUpgrade>> toFill, System.Random randomizer)
        {
            for (uint i = 0; i < currentTier; i++)
                GenerateUpgrade(i);

            return GenerateUpgrade(currentTier);

            List<PrimaryUpgrade> GenerateUpgrade(uint stat)
            {
                if (toFill.TryGetValue(stat, out List<PrimaryUpgrade> options))
                    return options;

                List<PrimaryUpgrade> possibles = Enum<PrimaryUpgrade>.GetValues().ToList();
                options = new List<PrimaryUpgrade>();
                uint optionCount = (uint)randomizer.Next(MinPrimaryOptions, MaxPrimaryOptions + 1);
                for (int i = 0; i < optionCount; i++)
                {
                    int index = randomizer.Next(possibles.Count);
                    options.Add(possibles.TakeAt(index));
                }

                toFill[stat] = options;
                return options;
            }
        }

        public static List<SecondaryUpgrade> GetSecondaryUpgradeOptions(uint currentStat, Dictionary<uint, List<SecondaryUpgrade>> toFill, System.Random randomizer)
        {
            for (uint i = 0; i < currentStat; i++)
                GenerateUpgrade(i);

            return GenerateUpgrade(currentStat);

            List<SecondaryUpgrade> GenerateUpgrade(uint stat)
            {
                if (toFill.TryGetValue(stat, out List<SecondaryUpgrade> options))
                    return options;

                List<SecondaryUpgrade> possibles = Enum.GetValues(typeof(SecondaryUpgrade)).Cast<SecondaryUpgrade>().ToList();
                options = new List<SecondaryUpgrade>();
                uint optionCount = (uint) randomizer.Next(MinSecondaryOptions, MaxSecondaryOptions + 1);
                for (int i = 0; i < optionCount; i++)
                {
                    int index = randomizer.Next(possibles.Count);
                    options.Add(possibles[index]);
                    possibles.RemoveAt(index);
                }

                toFill[stat] = options;
                return options;
            }
        }
    }
}