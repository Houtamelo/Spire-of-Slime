using System;
using System.Collections.Generic;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Interfaces;
using Core.Utils.Collections;
using Core.Utils.Patterns;
using JetBrains.Annotations;
using ListPool;

namespace Core.Save_Management.SaveObjects
{
    public interface IReadonlyCharacterStats
    {
        CleanString Key { get; }
        ICharacterScript GetScript();

        int Speed { get; }
        int DamageLower { get; }
        int DamageUpper { get; }

        int Stamina { get; }
        int Resilience { get; }

        int Lust { get; }
        int Composure { get; }
        int OrgasmLimit { get; }
        int OrgasmCount { get; }
        int Temptation { get; }
        int Corruption { get; }

        int Accuracy { get; }
        int CriticalChance { get; }
        int Dodge { get; }

        int PoisonResistance { get; }
        int PoisonApplyChance { get; }

        int DebuffResistance { get; }
        int DebuffApplyChance { get; }

        int MoveResistance { get; }
        int MoveApplyChance { get; }

        int StunMitigation { get; }

        Dictionary<PrimaryUpgrade, int> PrimaryUpgrades { get; }
        Dictionary<SecondaryUpgrade, int> SecondaryUpgrades { get; }
        
        [MustUseReturnValue] CustomValuePooledList<CleanString> GetUnlockedPerksAndSkills(Save save);
        [MustUseReturnValue] CustomValuePooledList<CleanString> GetUnlockedPerks(Save save);
        [MustUseReturnValue] CustomValuePooledList<CleanString> GetEnabledPerks(Save save);
        
        bool IsPerkUnlocked(CleanString key, Save save);
        bool IsSkillUnlocked(CleanString skillKey, Save save);
        
        int AvailablePerkPoints { get; }
        int AvailablePrimaryPoints { get; }
        int AvailableSecondaryPoints { get; }
        
        Dictionary<int, PrimaryUpgrade[]> PrimaryUpgradeOptions { get; }
        Dictionary<int, SecondaryUpgrade[]> SecondaryUpgradeOptions { get; }
        
        Dictionary<Race, int> SexualExpByRace { get; }
        
        Random Randomizer { get; }
        IReadOnlySkillSet GetSkillSet();
        
        int TotalExperience { get; }
        int Level { get; }

        public void SetValue(GeneralStat stat, int newValue);
        
        int GetValue(GeneralStat stat);
        (int lower, int upper) GetDamage();
        int GetValue(PrimaryUpgrade upgrade);
        int GetValue(SecondaryUpgrade upgrade);

        int GetUsedPrimaryPoints();
        int GetUsedSecondaryPoints();
        
        ReadOnlySpan<PrimaryUpgrade> GetPrimaryUpgradeOptions(int currentTier);
        ReadOnlySpan<SecondaryUpgrade> GetSecondaryUpgradeOptions(int currentTier);
    }
}