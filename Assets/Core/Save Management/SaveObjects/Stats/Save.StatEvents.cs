using System;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour.Modules;
using Core.Main_Characters.Ethel.Combat;
using Core.Main_Characters.Nema.Combat;
using Core.Utils.Math;
using Core.Utils.Patterns;
using JetBrains.Annotations;
using UnityEngine;
using static Core.Utils.Patterns.Option<Core.Save_Management.SaveObjects.CharacterStats>;

namespace Core.Save_Management.SaveObjects
{
    public partial class Save
    {
        public void ChangeLust(CleanString key, int delta)
        {
            if (GetStats(key).AssertSome(out CharacterStats stats) == false)
                return;
            
            SetDirty();
            int newLust = stats.Lust + delta;
            SetLust(stats, ILustModule.ClampLust(newLust));
        }

        public void SetLust(CleanString key, int value)
        {
            if (GetStats(key).AssertSome(out CharacterStats stats))
                SetLust(stats, value);
        }

        private void SetLust([NotNull] CharacterStats stats, int newValue)
        {
            CleanString variableName = VariablesName.StatName(stats.Key, GeneralStat.Lust);
            int oldLust = stats.Lust;
            newValue = ILustModule.ClampLust(newValue);
            if (oldLust == newValue)
                return;

            SetDirty();
            stats.Lust = newValue;
            IntChanged?.Invoke(variableName, oldLust, newValue);
        }

        public void SetTemptation(CleanString key, int value)
        {
            if (GetStats(key).AssertSome(out CharacterStats stats))
                SetTemptation(stats, value);
        }
        
        private void SetTemptation([NotNull] CharacterStats stats, int newValue)
        {
            CleanString variableName = VariablesName.StatName(stats.Key, GeneralStat.Temptation);
            int oldTemptation = stats.Temptation;
            if (oldTemptation == newValue)
                return;

            SetDirty();
            stats.Temptation = newValue;
            IntChanged?.Invoke(variableName, oldTemptation, newValue);
        }
        
        public void ChangeOrgasmCount(CleanString key, int delta)
        {
            if (GetStats(key).AssertSome(out CharacterStats stats))
                ChangeOrgasmCount(stats, delta);
        }
        
        private void ChangeOrgasmCount([NotNull] CharacterStats stats, int delta)
        {
            int newOrgasm = stats.OrgasmCount + delta;
            SetOrgasmCount(stats, newOrgasm);
        }

        public void SetOrgasmCount(CleanString key, int orgasmCount)
        {
            if (GetStats(key).AssertSome(out CharacterStats stats))
                SetOrgasmCount(stats, orgasmCount);
        }
        
        private void SetOrgasmCount([NotNull] CharacterStats stats, int newValue)
        {
            CleanString variableName = VariablesName.StatName(stats.Key, GeneralStat.OrgasmCount);
            int oldOrgasmCount = stats.OrgasmCount;
            newValue = ILustModule.ClampOrgasmCount(newValue, stats.OrgasmLimit);
            if (oldOrgasmCount == newValue)
                return;

            SetDirty();
            stats.OrgasmCount = newValue;
            IntChanged?.Invoke(variableName, oldOrgasmCount, newValue);
        }
        
        public void ChangeCorruption(CleanString key, int delta)
        {
            if (GetStats(key).AssertSome(out CharacterStats stats))
                ChangeCorruption(stats, delta);
        }
        
        private void ChangeCorruption([NotNull] CharacterStats stats, int delta)
        {
            CleanString variableName = VariablesName.StatName(stats.Key, GeneralStat.Corruption);
            int oldCorruption = stats.Corruption;
            int newCorruption = stats.Corruption + delta;
            if (oldCorruption == newCorruption)
                return;

            SetDirty();
            stats.Corruption = newCorruption;
            IntChanged?.Invoke(variableName, oldCorruption, newCorruption);
        }

        public void IncrementSexualExp(CleanString statsKey, Race race, int amount)
        {
            if (GetStats(statsKey).AssertSome(out CharacterStats stats))
                IncrementSexualExp(stats, race, amount);
        }

        private void IncrementSexualExp([NotNull] IReadonlyCharacterStats stats, Race race, int amount)
        {
            CleanString variableName = VariablesName.SexualExpByRaceName(stats.Key, race);
            int newValue = stats.SexualExpByRace.TryGetValue(race, out int oldValue) ? oldValue + amount : amount;
            stats.SexualExpByRace[race] = newValue;

            SetDirty(); 
            IntChanged?.Invoke(variableName, oldValue, newValue);
        }
        
        public void SetSexualExp(CleanString statsKey, Race race, int newValue)
        {
            if (GetStats(statsKey).AssertSome(out CharacterStats stats))
                SetSexualExp(stats, race, newValue);
        }

        private void SetSexualExp([NotNull] IReadonlyCharacterStats stats, Race race, int newValue)
        {
            CleanString variableName = VariablesName.SexualExpByRaceName(stats.Key, race);
            stats.SexualExpByRace.TryGetValue(race, out int oldValue);
            stats.SexualExpByRace[race] = newValue;
            
            if (oldValue == newValue)
                return;

            SetDirty(); 
            IntChanged?.Invoke(variableName, oldValue, newValue);
        }

        public int GetSexualExp(CleanString key, Race race)
        {
            if (GetStats(key).AssertSome(out CharacterStats stats) && stats.SexualExpByRace.TryGetValue(race, out int value))
                return value;

            return 0;
        }
        
        public void IncreasePrimaryUpgrade(CleanString key, PrimaryUpgrade upgrade)
        {
            if (GetStats(key).AssertSome(out CharacterStats stats) == false)
                return;

            if (stats.AvailablePrimaryPoints <= 0)
            {
                Debug.LogWarning("Tried to increase primary upgrade but no points available");
                return;
            }

            SetDirty();

            GeneralStat generalStat = upgrade.ToGeneral();
            CleanString statVariable = VariablesName.StatName(key, generalStat);
            int oldStat = stats.GetValue(generalStat);

            CleanString upgradeVariable = VariablesName.AllocatedPrimaryUpgradeName(key, upgrade);
            int oldUpgrade = stats.PrimaryUpgrades[upgrade];
            int newUpgrade = oldUpgrade + 1;
            stats.PrimaryUpgrades[upgrade] = newUpgrade;

            CleanString primaryPointsVariable = VariablesName.StatName(key, GeneralStat.PrimaryPoints);
            int oldPoints = stats.AvailablePrimaryPoints;
            int newPoints = stats.AvailablePrimaryPoints - 1;
            stats.AvailablePrimaryPoints = newPoints;

            int newStat = stats.GetValue(generalStat);

            IntChanged?.Invoke(upgradeVariable,       oldUpgrade, newUpgrade);
            IntChanged?.Invoke(primaryPointsVariable, oldPoints,  newPoints);
            IntChanged?.Invoke(statVariable,          oldStat,    newStat);
        }

        public void IncreaseSecondaryUpgrade(CleanString key, SecondaryUpgrade upgrade)
        {
            if (GetStats(key).AssertSome(out CharacterStats stats) == false)
                return;

            if (stats.AvailableSecondaryPoints <= 0)
            {
                Debug.LogWarning("Tried to increase secondary upgrade but no points available");
                return;
            }

            SetDirty();
            GeneralStat generalStat = upgrade.ToGeneral();
            CleanString statVariable = VariablesName.StatName(key, generalStat);
            int oldStat = stats.GetValue(generalStat);

            CleanString upgradeVariable = VariablesName.AllocatedSecondaryUpgradeName(key, upgrade);
            int oldUpgrade = stats.SecondaryUpgrades[upgrade];
            int newUpgrade = oldUpgrade + 1;
            stats.SecondaryUpgrades[upgrade] = newUpgrade;

            CleanString secondaryPointsVariable = VariablesName.StatName(key, GeneralStat.SecondaryPoints);
            int oldPoints = stats.AvailableSecondaryPoints;
            int newPoints = stats.AvailableSecondaryPoints - 1;
            stats.AvailableSecondaryPoints = newPoints;
                
            int newStat = stats.GetValue(generalStat);
            IntChanged?.Invoke(upgradeVariable,         oldUpgrade, newUpgrade);
            IntChanged?.Invoke(secondaryPointsVariable, oldPoints,  newPoints);
            IntChanged?.Invoke(statVariable,            oldStat,    newStat);
        }
        
        public void AwardExperienceFromDefeatedEnemy(CleanString defeatedCharacterKey)
        {
            int experience = ExperienceCalculator.GetExperienceFromEnemy(defeatedCharacterKey, save: this);
            foreach (CharacterStats stats in GetAllCharacterStats())
                AwardExperience(stats, experience);
        }

        public void AwardExperienceRaw(int amount)
        {
            foreach (CharacterStats stats in GetAllCharacterStats())
                AwardExperience(stats, amount);
        }

        private bool AwardExperience([NotNull] CharacterStats stats, int amount) => SetExperience(stats, stats.TotalExperience + amount);

        public bool SetExperience(CleanString key, int value)
        {
            if (GetStats(key).AssertSome(out CharacterStats stats))
                return SetExperience(stats, value);
            
            return false;
        }

        private bool SetExperience([NotNull] CharacterStats stats, int value)
        {
            SetDirty();
            
            int oldExperience = stats.TotalExperience;
            stats.TotalExperience = value;

            CleanString experienceVariable = VariablesName.StatName(stats.Key, GeneralStat.Experience);
            CleanString primaryPointsVariable = VariablesName.StatName(stats.Key, GeneralStat.PrimaryPoints);
            CleanString secondaryPointsVariable = VariablesName.StatName(stats.Key, GeneralStat.SecondaryPoints);
            CleanString perkPointsVariable = VariablesName.StatName(stats.Key, GeneralStat.PerkPoints);

            IntChanged?.Invoke(experienceVariable, oldExperience, stats.TotalExperience);

            int oldLevel = oldExperience / ExperienceCalculator.ExperienceNeededForLevelUp;
            int newLevel = stats.TotalExperience / ExperienceCalculator.ExperienceNeededForLevelUp;
            if (oldLevel == newLevel)
                return false;
            
            if (oldLevel > newLevel)
            {
                Debug.Log($"Character lost levels, this probably is not intended: oldExperience: {oldExperience}, new experience: {value}");
                return false;
            }
            
            int levelDelta = newLevel - oldLevel;
            
            int newPrimaryPoints = ((newLevel + 1) / 2) - ((oldLevel + 1) / 2); // only give primary points on odd levels
            int oldPrimaryPoints = stats.AvailablePrimaryPoints;
            stats.AvailablePrimaryPoints += newPrimaryPoints;

            int oldSecondaryPoints = stats.AvailableSecondaryPoints;
            stats.AvailableSecondaryPoints = stats.AvailableSecondaryPoints + levelDelta;

            int oldPerkPoints = stats.AvailablePerkPoints;
            stats.AvailablePerkPoints = stats.AvailablePerkPoints + levelDelta;

            IntChanged?.Invoke(primaryPointsVariable,   oldPrimaryPoints,   stats.AvailablePrimaryPoints);
            IntChanged?.Invoke(secondaryPointsVariable, oldSecondaryPoints, stats.AvailableSecondaryPoints);
            IntChanged?.Invoke(perkPointsVariable,      oldPerkPoints,      stats.AvailablePerkPoints);
            return true;
        }

        public void AwardPrimaryPoint(CleanString key, int points)
        {
            if (GetStats(key).AssertSome(out CharacterStats stats) == false)
                return;
            
            SetDirty();
            int oldPoints = stats.AvailablePrimaryPoints;
            stats.AvailablePrimaryPoints += points;
            CleanString variableName = VariablesName.StatName(key, GeneralStat.PrimaryPoints);
            IntChanged?.Invoke(variableName, oldPoints, stats.AvailablePrimaryPoints);
        }

        public void AwardSecondaryPoint(CleanString key, int points)
        {
            if (GetStats(key).AssertSome(out CharacterStats stats) == false)
                return;
            
            SetDirty();
            int oldPoints = stats.AvailableSecondaryPoints;
            stats.AvailableSecondaryPoints += points;
            CleanString variableName = VariablesName.StatName(key, GeneralStat.SecondaryPoints);
            IntChanged?.Invoke(variableName, oldPoints, stats.AvailableSecondaryPoints);
        }

        public void AwardPerkPoint(CleanString key, int points)
        {
            if (GetStats(key).AssertSome(out CharacterStats stats) == false)
                return;
            
            SetDirty();
            int oldPoints = stats.AvailablePerkPoints;
            stats.AvailablePerkPoints += points;
            CleanString variableName = VariablesName.StatName(key, GeneralStat.PerkPoints);
            IntChanged?.Invoke(variableName, oldPoints, stats.AvailablePerkPoints);
        }

        public void SetStat(CleanString key, GeneralStat stat, int value)
        {
            if (GetStats(key).AssertSome(out CharacterStats stats))
                SetStat(stats, stat, value);
        }

        public void SetStat([NotNull] CharacterStats stats, GeneralStat stat, int value)
        {
            int oldValue = stats.GetValue(stat);
            int newValue = value;
            if (oldValue == newValue)
                return;

            SetDirty();
            stats.SetValue(stat, newValue);
            CleanString variableName = VariablesName.StatName(stats.Key, stat);
            IntChanged?.Invoke(variableName, oldValue, newValue);
        }

        public int GetStat(CleanString characterKey, GeneralStat stat) 
            => GetStats(characterKey).AssertSome(out CharacterStats stats) ? stats.GetValue(stat) : 0;

        private Option<CharacterStats> GetStats(CleanString key)
        {
            if (key == Ethel.GlobalKey)
                return Some(_ethelStats);
            if (key == Nema.GlobalKey)
                return Some(_nemaStats);
            
            return Option.None;
        }

        public Option<IReadonlyCharacterStats> GetReadOnlyStats(CleanString key)
        {
            if (key == Ethel.GlobalKey)
                return _ethelStats;
            if (key == Nema.GlobalKey)
                return _nemaStats;
            
            return Option.None;
        }

        private static readonly IReadonlyCharacterStats[] ReusableReadOnlyStatsArray = new IReadonlyCharacterStats[2];

        [Pure] public ReadOnlySpan<IReadonlyCharacterStats> GetAllReadOnlyCharacterStats()
        {
            ReusableReadOnlyStatsArray[0] = EthelStats;
            ReusableReadOnlyStatsArray[1] = NemaStats;
            return ReusableReadOnlyStatsArray.AsSpan();
        }
        
        private static readonly CharacterStats[] ReusableStatsArray = new CharacterStats[2];
        
        private ReadOnlySpan<CharacterStats> GetAllCharacterStats()
        {
            ReusableStatsArray[0] = _ethelStats;
            ReusableStatsArray[1] = _nemaStats;
            return ReusableStatsArray.AsSpan();
        }
    }
}