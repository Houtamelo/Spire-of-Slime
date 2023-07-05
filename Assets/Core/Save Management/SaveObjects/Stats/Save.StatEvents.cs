using System;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Main_Characters.Nema.Combat;
using Core.Utils.Math;
using Core.Utils.Patterns;
using Data.Main_Characters.Ethel;
using JetBrains.Annotations;
using UnityEngine;
using Utils.Patterns;
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
            uint newLust = (uint)((int)stats.Lust + delta);
            SetLust(stats, ILustModule.ClampLust(newLust));
        }

        public void SetLust(CleanString key, uint value)
        {
            if (GetStats(key).AssertSome(out CharacterStats stats))
                SetLust(stats, value);
        }

        private void SetLust(CharacterStats stats, uint newValue)
        {
            CleanString variableName = VariablesName.StatName(stats.Key, GeneralStat.Lust);
            uint oldLust = stats.Lust;
            newValue = ILustModule.ClampLust(newValue);
            if (oldLust == newValue)
                return;

            SetDirty();
            stats.Lust = newValue;
            FloatChanged?.Invoke(variableName, oldLust, newValue);
        }

        public void SetTemptation(CleanString key, ClampedPercentage value)
        {
            if (GetStats(key).AssertSome(out CharacterStats stats))
                SetTemptation(stats, value);
        }
        
        private void SetTemptation(CharacterStats stats, ClampedPercentage newValue)
        {
            CleanString variableName = VariablesName.StatName(stats.Key, GeneralStat.Temptation);
            ClampedPercentage oldTemptation = stats.Temptation;
            if (oldTemptation == newValue)
                return;

            SetDirty();
            stats.Temptation = newValue;
            FloatChanged?.Invoke(variableName, oldTemptation, newValue);
        }

        public void SetOrgasmCount(CleanString key, uint orgasmCount)
        {
            if (GetStats(key).AssertSome(out CharacterStats stats))
                SetOrgasmCount(stats, orgasmCount);
        }
        
        private void SetOrgasmCount(CharacterStats stats, uint newValue)
        {
            CleanString variableName = VariablesName.StatName(stats.Key, GeneralStat.OrgasmCount);
            uint oldOrgasmCount = stats.OrgasmCount;
            newValue = ILustModule.ClampOrgasmCount(newValue, stats.OrgasmLimit);
            if (oldOrgasmCount == newValue)
                return;

            SetDirty();
            stats.OrgasmCount = newValue;
            FloatChanged?.Invoke(variableName, oldOrgasmCount, newValue);
        }
        
        public void ChangeCorruption(CleanString key, float delta)
        {
            if (GetStats(key).AssertSome(out CharacterStats stats))
                ChangeCorruption(stats, delta);
        }
        
        private void ChangeCorruption(CharacterStats stats, float delta)
        {
            CleanString variableName = VariablesName.StatName(stats.Key, GeneralStat.Corruption);
            ClampedPercentage oldCorruption = stats.Corruption;
            ClampedPercentage newCorruption = stats.Corruption + delta;
            if (oldCorruption == newCorruption)
                return;

            SetDirty();
            stats.Corruption = newCorruption;
            FloatChanged?.Invoke(variableName, oldCorruption, newCorruption);
        }

        public void IncrementSexualExp(CleanString statsKey, Race race, uint amount)
        {
            if (GetStats(statsKey).AssertSome(out CharacterStats stats))
                IncrementSexualExp(stats, race, amount);
        }

        private void IncrementSexualExp(IReadonlyCharacterStats stats, Race race, uint amount)
        {
            CleanString variableName = VariablesName.SexualExpByRaceName(stats.Key, race);
            uint newValue = stats.SexualExpByRace.TryGetValue(race, out uint oldValue) ? oldValue + amount : amount;
            stats.SexualExpByRace[race] = newValue;

            SetDirty(); 
            FloatChanged?.Invoke(variableName, oldValue, newValue);
        }
        
        public void SetSexualExp(CleanString statsKey, Race race, uint newValue)
        {
            if (GetStats(statsKey).AssertSome(out CharacterStats stats))
                SetSexualExp(stats, race, newValue);
        }

        private void SetSexualExp(IReadonlyCharacterStats stats, Race race, uint newValue)
        {
            CleanString variableName = VariablesName.SexualExpByRaceName(stats.Key, race);
            stats.SexualExpByRace.TryGetValue(race, out uint oldValue);
            stats.SexualExpByRace[race] = newValue;
            
            if (oldValue == newValue)
                return;

            SetDirty(); 
            FloatChanged?.Invoke(variableName, oldValue, newValue);
        }

        public uint GetSexualExp(CleanString key, Race race)
        {
            if (GetStats(key).AssertSome(out CharacterStats stats) && stats.SexualExpByRace.TryGetValue(race, out uint value))
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
            float oldStat = stats.GetValue(generalStat);

            CleanString upgradeVariable = VariablesName.AllocatedPrimaryUpgradeName(key, upgrade);
            uint oldUpgrade = stats.PrimaryUpgrades[upgrade];
            uint newUpgrade = oldUpgrade + 1;
            stats.PrimaryUpgrades[upgrade] = newUpgrade;

            CleanString primaryPointsVariable = VariablesName.StatName(key, GeneralStat.PrimaryPoints);
            uint oldPoints = stats.AvailablePrimaryPoints;
            uint newPoints = stats.AvailablePrimaryPoints - 1;
            stats.AvailablePrimaryPoints = newPoints;

            float newStat = stats.GetValue(generalStat);

            FloatChanged?.Invoke(upgradeVariable,       oldUpgrade, newUpgrade);
            FloatChanged?.Invoke(primaryPointsVariable, oldPoints,  newPoints);
            FloatChanged?.Invoke(statVariable,          oldStat,    newStat);
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
            float oldStat = stats.GetValue(generalStat);

            CleanString upgradeVariable = VariablesName.AllocatedSecondaryUpgradeName(key, upgrade);
            uint oldUpgrade = stats.SecondaryUpgrades[upgrade];
            uint newUpgrade = oldUpgrade + 1;
            stats.SecondaryUpgrades[upgrade] = newUpgrade;

            CleanString secondaryPointsVariable = VariablesName.StatName(key, GeneralStat.SecondaryPoints);
            uint oldPoints = stats.AvailableSecondaryPoints;
            uint newPoints = stats.AvailableSecondaryPoints - 1;
            stats.AvailableSecondaryPoints = newPoints;
                
            float newStat = stats.GetValue(generalStat);
            FloatChanged?.Invoke(upgradeVariable,         oldUpgrade, newUpgrade);
            FloatChanged?.Invoke(secondaryPointsVariable, oldPoints,  newPoints);
            FloatChanged?.Invoke(statVariable,            oldStat,    newStat);
        }
        
        public void AwardExperienceFromDefeatedEnemy(CleanString defeatedCharacterKey)
        {
            float experience = ExperienceCalculator.GetExperienceFromEnemy(defeatedCharacterKey, save: this);
            foreach (CharacterStats stats in GetAllCharacterStats())
                AwardExperience(stats, experience);
        }

        public void AwardExperienceRaw(float amount)
        {
            foreach (CharacterStats stats in GetAllCharacterStats())
                AwardExperience(stats, amount);
        }

        private bool AwardExperience(CharacterStats stats, float amount) => SetExperience(stats, stats.Experience + amount);

        public bool SetExperience(CleanString key, float value)
        {
            if (GetStats(key).AssertSome(out CharacterStats stats))
                return SetExperience(stats, value);
            
            return false;
        }

        private bool SetExperience(CharacterStats stats, float value)
        {
            SetDirty();
            
            float oldExperience = stats.Experience;
            stats.Experience = value;

            CleanString experienceVariable = VariablesName.StatName(stats.Key, GeneralStat.Experience);
            CleanString primaryPointsVariable = VariablesName.StatName(stats.Key, GeneralStat.PrimaryPoints);
            CleanString secondaryPointsVariable = VariablesName.StatName(stats.Key, GeneralStat.SecondaryPoints);
            CleanString perkPointsVariable = VariablesName.StatName(stats.Key, GeneralStat.PerkPoints);

            FloatChanged?.Invoke(experienceVariable, oldExperience, stats.Experience);

            uint oldLevel = (oldExperience / ExperienceCalculator.ExperienceNeededForLevelUp).FloorToUInt();
            uint newLevel = (stats.Experience / ExperienceCalculator.ExperienceNeededForLevelUp).FloorToUInt();
            if (oldLevel == newLevel)
                return false;
            
            if (oldLevel > newLevel)
            {
                Debug.Log($"Character lost levels, this probably is not intended: oldExperience: {oldExperience}, new experience: {value}");
                return false;
            }
            
            uint levelDelta = newLevel - oldLevel;
            
            uint newPrimaryPoints = ((newLevel + 1) / 2) - ((oldLevel + 1) / 2); // only give primary points on odd levels
            uint oldPrimaryPoints = stats.AvailablePrimaryPoints;
            stats.AvailablePrimaryPoints += newPrimaryPoints;

            uint oldSecondaryPoints = stats.AvailableSecondaryPoints;
            stats.AvailableSecondaryPoints += levelDelta;

            uint oldPerkPoints = stats.AvailablePerkPoints;
            stats.AvailablePerkPoints += levelDelta;

            FloatChanged?.Invoke(primaryPointsVariable,   oldPrimaryPoints,   stats.AvailablePrimaryPoints);
            FloatChanged?.Invoke(secondaryPointsVariable, oldSecondaryPoints, stats.AvailableSecondaryPoints);
            FloatChanged?.Invoke(perkPointsVariable,      oldPerkPoints,      stats.AvailablePerkPoints);
            return true;
        }

        public void AwardPrimaryPoint(CleanString key, uint points)
        {
            if (GetStats(key).AssertSome(out CharacterStats stats) == false)
                return;
            
            SetDirty();
            uint oldPoints = stats.AvailablePrimaryPoints;
            stats.AvailablePrimaryPoints += points;
            CleanString variableName = VariablesName.StatName(key, GeneralStat.PrimaryPoints);
            FloatChanged?.Invoke(variableName, oldPoints, stats.AvailablePrimaryPoints);
        }

        public void AwardSecondaryPoint(CleanString key, uint points)
        {
            if (GetStats(key).AssertSome(out CharacterStats stats) == false)
                return;
            
            SetDirty();
            uint oldPoints = stats.AvailableSecondaryPoints;
            stats.AvailableSecondaryPoints += points;
            CleanString variableName = VariablesName.StatName(key, GeneralStat.SecondaryPoints);
            FloatChanged?.Invoke(variableName, oldPoints, stats.AvailableSecondaryPoints);
        }

        public void AwardPerkPoint(CleanString key, uint points)
        {
            if (GetStats(key).AssertSome(out CharacterStats stats) == false)
                return;
            
            SetDirty();
            uint oldPoints = stats.AvailablePerkPoints;
            stats.AvailablePerkPoints += points;
            CleanString variableName = VariablesName.StatName(key, GeneralStat.PerkPoints);
            FloatChanged?.Invoke(variableName, oldPoints, stats.AvailablePerkPoints);
        }

        public void SetStat(CleanString key, GeneralStat stat, float value)
        {
            if (GetStats(key).AssertSome(out CharacterStats stats))
                SetStat(stats, stat, value);
        }

        public void SetStat(CharacterStats stats, GeneralStat stat, float value)
        {
            float oldValue = stats.GetValue(stat);
            float newValue = value;
            if (oldValue == newValue)
                return;

            SetDirty();
            stats.SetValue(stat, newValue);
            CleanString variableName = VariablesName.StatName(stats.Key, stat);
            FloatChanged?.Invoke(variableName, oldValue, newValue);
        }

        public float GetStat(CleanString characterKey, GeneralStat stat) => GetStats(characterKey).AssertSome(out CharacterStats stats) ? stats.GetValue(stat) : 0;

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

        [Pure]
        public ReadOnlySpan<IReadonlyCharacterStats> GetAllReadOnlyCharacterStats()
        {
            ReusableReadOnlyStatsArray[0] = EthelStats;
            ReusableReadOnlyStatsArray[1] = NemaStats;
            return (ReadOnlySpan<IReadonlyCharacterStats>)ReusableReadOnlyStatsArray;
        }
        
        private static readonly CharacterStats[] ReusableStatsArray = new CharacterStats[2];
        
        private ReadOnlySpan<CharacterStats> GetAllCharacterStats()
        {
            ReusableStatsArray[0] = _ethelStats;
            ReusableStatsArray[1] = _nemaStats;
            return (ReadOnlySpan<CharacterStats>)ReusableStatsArray;
        }
    }
}