using System;
using System.Collections.Generic;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Interfaces;
using JetBrains.Annotations;
using ListPool;
using Utils.Patterns;

namespace Save_Management
{
    public interface IReadonlyCharacterStats
    {
        CleanString Key { get; }
        ICharacterScript GetScript();

        float Speed { get; }
        uint DamageLower { get; }
        uint DamageUpper { get; }

        uint Stamina { get; }
        float Resilience { get; }

        uint Lust { get; }
        float Composure { get; }
        uint OrgasmLimit { get; }
        uint OrgasmCount { get; }
        ClampedPercentage Temptation { get; }
        ClampedPercentage Corruption { get; }

        float Accuracy { get; }
        float CriticalChance { get; }
        float Dodge { get; }

        float PoisonResistance { get; }
        float PoisonApplyChance { get; }

        float DebuffResistance { get; }
        float DebuffApplyChance { get; }

        float MoveResistance { get; }
        float MoveApplyChance { get; }

        float StunRecoverySpeed { get; }

        Dictionary<PrimaryUpgrade, uint> PrimaryUpgrades { get; }
        Dictionary<SecondaryUpgrade, uint> SecondaryUpgrades { get; }
        
        [MustUseReturnValue] ValueListPool<CleanString> GetUnlockedPerksAndSkills(Save save);
        [MustUseReturnValue] ValueListPool<CleanString> GetUnlockedPerks(Save save);
        [MustUseReturnValue] ValueListPool<CleanString> GetEnabledPerks(Save save);
        
        bool IsPerkUnlocked(CleanString key, Save save);
        bool IsSkillUnlocked(CleanString skillKey, Save save);
        
        uint AvailablePerkPoints { get; }
        uint AvailablePrimaryPoints { get; }
        uint AvailableSecondaryPoints { get; }
        
        Dictionary<uint, List<PrimaryUpgrade>> PrimaryUpgradeOptions { get; }
        Dictionary<uint, List<SecondaryUpgrade>> SecondaryUpgradeOptions { get; }
        
        Dictionary<Race, uint> SexualExpByRace { get; }
        
        Random Randomizer { get; }
        IReadOnlySkillSet GetSkillSet();
        
        float Experience { get; }
        uint Level { get; }

        public void SetValue(GeneralStat stat, float newValue);
        float GetValue(GeneralStat stat);
        (uint lower, uint upper) GetDamage();
        float GetValue(PrimaryUpgrade upgrade);
        float GetValue(SecondaryUpgrade upgrade);

        uint GetUsedPrimaryPoints();
        uint GetUsedSecondaryPoints();
        
        List<PrimaryUpgrade> GetPrimaryUpgradeOptions(uint currentTier);
        List<SecondaryUpgrade> GetSecondaryUpgradeOptions(uint currentTier);
    }
}