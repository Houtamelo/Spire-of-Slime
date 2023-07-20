using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour.Modules;
using Core.Combat.Scripts.Interfaces;
using Core.Main_Database.Combat;
using Core.Utils.Collections;
using Core.Utils.Collections.Extensions;
using Core.Utils.Extensions;
using Core.Utils.Math;
using Core.Utils.Patterns;
using JetBrains.Annotations;
using ListPool;
using UnityEngine;
using Random = System.Random;

namespace Core.Save_Management.SaveObjects
{
    [DataContract]
    public class CharacterStats : IReadonlyCharacterStats, IDeepCloneable<CharacterStats>
    {
        [DataMember] public CleanString Key { get; init; }
        [DataMember] public Random Randomizer { get; init; }
        [DataMember] public int TotalExperience { get; set; }

        [DataMember] public SkillSet SkillSet { get; set; }
        public IReadOnlySkillSet GetSkillSet() => SkillSet;

        [DataMember] public int DamageLower { get; set; }
        [DataMember] public int DamageUpper { get; set; }

        [DataMember] public int Stamina { get; set; }
        [DataMember] public int Resilience { get; set; }
        
        [DataMember] public int Lust { get; set; }
        [DataMember] public int Temptation { get; set; }
        [DataMember] public int OrgasmLimit { get; set; }
        [DataMember] public int OrgasmCount { get; set; }
        [DataMember] public int Composure { get; set; }
        [DataMember] public int Corruption { get; set; }

        [DataMember] public int PoisonResistance { get; set; }
        [DataMember] public int PoisonApplyChance { get; set; }

        [DataMember] public int DebuffResistance { get; set; }
        [DataMember] public int DebuffApplyChance { get; set; }

        [DataMember] public int MoveResistance { get; set; }
        [DataMember] public int MoveApplyChance { get; set; }

        [DataMember] public int StunMitigation { get; set; }
        
        [DataMember] public int Speed { get; set; }
        [DataMember] public int Accuracy { get; set; }
        [DataMember] public int CriticalChance { get; set; }
        [DataMember] public int Dodge { get; set; }

        [DataMember] public Dictionary<PrimaryUpgrade, int> PrimaryUpgrades { get; init; }
        [DataMember] public Dictionary<SecondaryUpgrade, int> SecondaryUpgrades { get; init; }

        [DataMember] public int AvailablePerkPoints { get; set; }
        [DataMember] public int AvailablePrimaryPoints { get; set; }
        [DataMember] public int AvailableSecondaryPoints { get; set; }

        [DataMember] public Dictionary<int, PrimaryUpgrade[]> PrimaryUpgradeOptions { get; init; }
        [DataMember] public Dictionary<int, SecondaryUpgrade[]> SecondaryUpgradeOptions { get; init; }
        
        [DataMember] public Dictionary<Race, int> SexualExpByRace { get; init; }
        
        public int Level => (TotalExperience / ExperienceCalculator.ExperienceNeededForLevelUp);
        
        private Option<ICharacterScript> _cachedScript = Option<ICharacterScript>.None;

        public ICharacterScript GetScript()
        {
            if (_cachedScript.TrySome(out ICharacterScript script))
                return script;
            
            Option<CharacterScriptable> option = CharacterDatabase.GetCharacter(Key);
            if (option.IsSome)
            {
                _cachedScript = Option<ICharacterScript>.Some(option.Value);
                return _cachedScript.Value;
            }
            
            Debug.LogError($"Character {Key} not found in database, defaulting to Ethel.");
            return CharacterDatabase.DefaultEthel;
        }

        [MustUseReturnValue]
        public CustomValuePooledList<CleanString> GetUnlockedPerksAndSkills([NotNull] Save save)
        {
            CustomValuePooledList<CleanString> list = new(capacity: 32);
            CleanString perkPrefix = VariablesName.PerkPrefix(Key);
            CleanString skillPrefix = VariablesName.SkillPrefix(Key);
            
            foreach ((CleanString key, bool value) in save.Booleans)
            {
                if (value == true && (key.StartsWith(ref perkPrefix) || key.StartsWith(ref skillPrefix)))
                    list.Add(key);
            }

            return list;
        }

        [MustUseReturnValue]
        public CustomValuePooledList<CleanString> GetUnlockedPerks([NotNull] Save save)
        {
            CustomValuePooledList<CleanString> list = new(capacity: 32);
            CleanString perkPrefix = VariablesName.PerkPrefix(Key);
            
            foreach ((CleanString key, bool value) in save.Booleans)
            {
                if (value == true && key.StartsWith(ref perkPrefix))
                    list.Add(key);
            }

            return list;
        }
        
        [MustUseReturnValue]
        public CustomValuePooledList<CleanString> GetUnlockedSkills([NotNull] Save save)
        {
            CustomValuePooledList<CleanString> list = new(capacity: 32);
            CleanString skillPrefix = VariablesName.SkillPrefix(Key);
            
            foreach ((CleanString key, bool value) in save.Booleans)
            {
                if (value == true && key.StartsWith(ref skillPrefix))
                    list.Add(key);
            }

            return list;
        }

        [MustUseReturnValue]
        public CustomValuePooledList<CleanString> GetEnabledPerks([NotNull] Save save)
        {
            CustomValuePooledList<CleanString> list = new(capacity: 32);
            foreach (CleanString perkKey in GetUnlockedPerks(save))
            {
                CleanString activePerk = VariablesName.EnabledPerkName(characterKey: Key, perkKey);
                if (save.Booleans.TryGetValue(activePerk, out bool value) && value)
                    list.Add(perkKey);
            }
            
            return list;
        }

        public bool IsPerkUnlocked(CleanString key, [NotNull] Save save)
        {
            foreach (CleanString perk in GetUnlockedPerks(save))
            {
                if (perk == key)
                    return true;
            }

            return false;
        }
        
        public bool IsSkillUnlocked(CleanString key, [NotNull] Save save)
        {
            foreach (CleanString skill in GetUnlockedSkills(save))
            {
                if (skill == key)
                    return true;
            }

            return false;
        }

        public int GetUsedPrimaryPoints()
        {
            int usedPoints = 0;
            foreach (int upgradeCount in PrimaryUpgrades.Values)
                usedPoints += upgradeCount;

            return usedPoints;
        }

        public int GetUsedSecondaryPoints()
        {
            int usedPoints = 0;
            foreach (int upgradeCount in SecondaryUpgrades.Values)
                usedPoints += upgradeCount;

            return usedPoints;
        }

        public void SetValue(GeneralStat stat, int newValue)
        {
            
            switch (stat)
            {
                case GeneralStat.Stamina:           Stamina    = IStaminaModule.ClampMaxStamina(newValue); break;
                case GeneralStat.Resilience:        Resilience = IStaminaModule.ClampResilience(newValue); break;
                case GeneralStat.StunMitigation:    StunMitigation = IStunModule.ClampStunMitigation(newValue); break;
                case GeneralStat.Lust:              Lust        = ILustModule.ClampLust(newValue);                     break;
                case GeneralStat.OrgasmLimit:       OrgasmLimit = ILustModule.ClampOrgasmLimit(newValue);              break;
                case GeneralStat.OrgasmCount:       OrgasmCount = ILustModule.ClampOrgasmCount(newValue, OrgasmLimit); break;
                case GeneralStat.Composure:         Composure   = ILustModule.ClampComposure(newValue);                break;
                case GeneralStat.Temptation:        Temptation  = ILustModule.ClampTemptation(newValue);               break;
                case GeneralStat.Corruption:        Corruption  = ILustModule.ClampCorruption(newValue);               break;
                case GeneralStat.PoisonResistance:  PoisonResistance = IResistancesModule.ClampResistance(newValue); break;
                case GeneralStat.DebuffResistance:  DebuffResistance = IResistancesModule.ClampResistance(newValue); break;
                case GeneralStat.MoveResistance:    MoveResistance   = IResistancesModule.ClampResistance(newValue); break;
                case GeneralStat.PoisonApplyChance: PoisonApplyChance = IStatusApplierModule.ClampPoisonApplyChance(newValue); break;
                case GeneralStat.DebuffApplyChance: DebuffApplyChance = IStatusApplierModule.ClampDebuffApplyChance(newValue); break;
                case GeneralStat.MoveApplyChance:   MoveApplyChance   = IStatusApplierModule.ClampMoveApplyChance(newValue);   break;
                case GeneralStat.DamageLower:       (DamageLower, DamageUpper) = IStatsModule.ClampRawDamage(newValue, DamageUpper); break;
                case GeneralStat.DamageUpper:       (DamageLower, DamageUpper) = IStatsModule.ClampRawDamage(DamageLower, newValue); break;
                case GeneralStat.Speed:             Speed                      = IStatsModule.ClampSpeed(newValue);                       break;
                case GeneralStat.Accuracy:          Accuracy                   = IStatsModule.ClampAccuracy(newValue);                    break;
                case GeneralStat.CriticalChance:    CriticalChance             = IStatsModule.ClampCriticalChance(newValue);              break;
                case GeneralStat.Dodge:             Dodge                      = IStatsModule.ClampDodge(newValue);                       break;
                default: throw new ArgumentOutOfRangeException(nameof(stat), stat, message: null);
            }
        }

        public int GetValue(PrimaryUpgrade upgrade) => upgrade switch
        {
            PrimaryUpgrade.Accuracy   => IStatsModule.ClampAccuracy            (Accuracy + UpgradeHelper.GetUpgradeFull(PrimaryUpgrades[PrimaryUpgrade.Accuracy],   PrimaryUpgrade.Accuracy)),
            PrimaryUpgrade.Dodge      => IStatsModule.ClampDodge                  (Dodge + UpgradeHelper.GetUpgradeFull(PrimaryUpgrades[PrimaryUpgrade.Dodge],      PrimaryUpgrade.Dodge)),
            PrimaryUpgrade.Critical   => IStatsModule.ClampCriticalChance(CriticalChance + UpgradeHelper.GetUpgradeFull(PrimaryUpgrades[PrimaryUpgrade.Critical],   PrimaryUpgrade.Critical)),
            PrimaryUpgrade.Resilience => IStaminaModule.ClampResilience      (Resilience + UpgradeHelper.GetUpgradeFull(PrimaryUpgrades[PrimaryUpgrade.Resilience], PrimaryUpgrade.Resilience)),
            _ => throw new ArgumentOutOfRangeException(nameof(upgrade), upgrade, message: null)
        };

        public int GetValue(SecondaryUpgrade upgrade) => upgrade switch
        {
            SecondaryUpgrade.PoisonResistance  => IResistancesModule.ClampResistance          (PoisonResistance + UpgradeHelper.GetUpgradeFull(SecondaryUpgrades[SecondaryUpgrade.PoisonResistance],  SecondaryUpgrade.PoisonResistance)),
            SecondaryUpgrade.DebuffResistance  => IResistancesModule.ClampResistance          (DebuffResistance + UpgradeHelper.GetUpgradeFull(SecondaryUpgrades[SecondaryUpgrade.DebuffResistance],  SecondaryUpgrade.DebuffResistance)),
            SecondaryUpgrade.MoveResistance    => IResistancesModule.ClampResistance            (MoveResistance + UpgradeHelper.GetUpgradeFull(SecondaryUpgrades[SecondaryUpgrade.MoveResistance],    SecondaryUpgrade.MoveResistance)),
            SecondaryUpgrade.StunMitigation    => IStunModule.ClampStunMitigation               (StunMitigation + UpgradeHelper.GetUpgradeFull(SecondaryUpgrades[SecondaryUpgrade.StunMitigation],    SecondaryUpgrade.StunMitigation)),
            SecondaryUpgrade.Composure         => ILustModule.ClampComposure                         (Composure + UpgradeHelper.GetUpgradeFull(SecondaryUpgrades[SecondaryUpgrade.Composure],         SecondaryUpgrade.Composure)),
            SecondaryUpgrade.PoisonApplyChance => IStatusApplierModule.ClampPoisonApplyChance(PoisonApplyChance + UpgradeHelper.GetUpgradeFull(SecondaryUpgrades[SecondaryUpgrade.PoisonApplyChance], SecondaryUpgrade.PoisonApplyChance)),
            SecondaryUpgrade.DebuffApplyChance => IStatusApplierModule.ClampDebuffApplyChance(DebuffApplyChance + UpgradeHelper.GetUpgradeFull(SecondaryUpgrades[SecondaryUpgrade.DebuffApplyChance], SecondaryUpgrade.DebuffApplyChance)),
            SecondaryUpgrade.MoveApplyChance   => IStatusApplierModule.ClampMoveApplyChance    (MoveApplyChance + UpgradeHelper.GetUpgradeFull(SecondaryUpgrades[SecondaryUpgrade.MoveApplyChance],   SecondaryUpgrade.MoveApplyChance)),
            _ => throw new ArgumentOutOfRangeException(nameof(upgrade), upgrade, message: null)
        };

        public int GetValue(GeneralStat stat)
        {
            return stat switch
            {
                GeneralStat.Experience         => TotalExperience,
                GeneralStat.PrimaryPoints      => AvailablePrimaryPoints,
                GeneralStat.SecondaryPoints    => AvailableSecondaryPoints,
                GeneralStat.PerkPoints         => AvailablePerkPoints,
                GeneralStat.Speed              => IStatsModule.ClampSpeed(Speed),
                GeneralStat.Stamina            => IStaminaModule.ClampMaxStamina(Stamina),
                GeneralStat.DamageLower        => IStatsModule.ClampRawDamage(DamageLower, DamageUpper).lower,
                GeneralStat.DamageUpper        => IStatsModule.ClampRawDamage(DamageLower, DamageUpper).upper,
                GeneralStat.Lust               => ILustModule.ClampLust(Lust),
                GeneralStat.Temptation         => ILustModule.ClampTemptation(Temptation),
                GeneralStat.Corruption         => ILustModule.ClampCorruption(Corruption),
                GeneralStat.OrgasmLimit        => ILustModule.ClampOrgasmLimit(OrgasmLimit),
                GeneralStat.OrgasmCount        => ILustModule.ClampOrgasmCount(OrgasmCount, OrgasmLimit),
                GeneralStat.Resilience         => IStaminaModule.ClampResilience             (Resilience        + UpgradeHelper.GetUpgradeFull(PrimaryUpgrades[PrimaryUpgrade.Resilience], PrimaryUpgrade.Resilience)),
                GeneralStat.Accuracy           => IStatsModule.ClampAccuracy                 (Accuracy          + UpgradeHelper.GetUpgradeFull(PrimaryUpgrades[PrimaryUpgrade.Accuracy],   PrimaryUpgrade.Accuracy)),
                GeneralStat.CriticalChance     => IStatsModule.ClampCriticalChance           (CriticalChance    + UpgradeHelper.GetUpgradeFull(PrimaryUpgrades[PrimaryUpgrade.Critical],   PrimaryUpgrade.Critical)),
                GeneralStat.Dodge              => IStatsModule.ClampDodge                    (Dodge             + UpgradeHelper.GetUpgradeFull(PrimaryUpgrades[PrimaryUpgrade.Dodge],      PrimaryUpgrade.Dodge)),
                GeneralStat.StunMitigation     => IStunModule.ClampStunMitigation            (StunMitigation    + UpgradeHelper.GetUpgradeFull(SecondaryUpgrades[SecondaryUpgrade.StunMitigation],    SecondaryUpgrade.StunMitigation)),
                GeneralStat.Composure          => ILustModule.ClampComposure                 (Composure         + UpgradeHelper.GetUpgradeFull(SecondaryUpgrades[SecondaryUpgrade.Composure],         SecondaryUpgrade.Composure)),
                GeneralStat.PoisonResistance   => IResistancesModule.ClampResistance         (PoisonResistance  + UpgradeHelper.GetUpgradeFull(SecondaryUpgrades[SecondaryUpgrade.PoisonResistance],  SecondaryUpgrade.PoisonResistance)),
                GeneralStat.DebuffResistance   => IResistancesModule.ClampResistance         (DebuffResistance  + UpgradeHelper.GetUpgradeFull(SecondaryUpgrades[SecondaryUpgrade.DebuffResistance],  SecondaryUpgrade.DebuffResistance)),
                GeneralStat.MoveResistance     => IResistancesModule.ClampResistance         (MoveResistance    + UpgradeHelper.GetUpgradeFull(SecondaryUpgrades[SecondaryUpgrade.MoveResistance],    SecondaryUpgrade.MoveResistance)),
                GeneralStat.PoisonApplyChance  => IStatusApplierModule.ClampPoisonApplyChance(PoisonApplyChance + UpgradeHelper.GetUpgradeFull(SecondaryUpgrades[SecondaryUpgrade.PoisonApplyChance], SecondaryUpgrade.PoisonApplyChance)),
                GeneralStat.DebuffApplyChance  => IStatusApplierModule.ClampDebuffApplyChance(DebuffApplyChance + UpgradeHelper.GetUpgradeFull(SecondaryUpgrades[SecondaryUpgrade.DebuffApplyChance], SecondaryUpgrade.DebuffApplyChance)),
                GeneralStat.MoveApplyChance    => IStatusApplierModule.ClampMoveApplyChance  (MoveApplyChance   + UpgradeHelper.GetUpgradeFull(SecondaryUpgrades[SecondaryUpgrade.MoveApplyChance],   SecondaryUpgrade.MoveApplyChance)),
                GeneralStat.ArousalApplyChance => 0,
                _                              => throw new ArgumentOutOfRangeException(nameof(stat), stat, message:$"Unhandled stat: {stat}")
            };
        }

        public (int lower, int upper) GetDamage() => IStatsModule.ClampRawDamage(DamageLower, DamageUpper);

        public ReadOnlySpan<PrimaryUpgrade> GetPrimaryUpgradeOptions(int currentTier)     => UpgradeHelper.GetPrimaryUpgradeOptions  (currentTier, PrimaryUpgradeOptions,   Randomizer);
        public ReadOnlySpan<SecondaryUpgrade> GetSecondaryUpgradeOptions(int currentTier) => UpgradeHelper.GetSecondaryUpgradeOptions(currentTier, SecondaryUpgradeOptions, Randomizer);

        [NotNull]
        public CharacterStats DeepClone()
        {
            int clampedOrgasmLimit = ILustModule.ClampOrgasmLimit(OrgasmLimit);
            (int clampedLowerDamage, int clampedUpperDamage) = IStatsModule.ClampRawDamage(DamageLower, DamageUpper);
            
            return new CharacterStats
            {
                Key = Key,
                Randomizer = Randomizer.DeepClone(),
                SkillSet = SkillSet.DeepClone(),
                TotalExperience = TotalExperience,
                
                Stamina    = IStaminaModule.ClampMaxStamina(Stamina),
                Resilience = IStaminaModule.ClampResilience(Resilience),
                
                Lust        = ILustModule.ClampLust(Lust),
                Temptation  = ILustModule.ClampTemptation(Temptation),
                OrgasmCount = ILustModule.ClampOrgasmCount(OrgasmCount, clampedOrgasmLimit),
                OrgasmLimit = clampedOrgasmLimit,
                Composure   = ILustModule.ClampComposure(Composure),
                Corruption  = ILustModule.ClampCorruption(Corruption),

                PoisonResistance = IResistancesModule.ClampResistance(PoisonResistance),
                DebuffResistance = IResistancesModule.ClampResistance(DebuffResistance),
                MoveResistance   = IResistancesModule.ClampResistance(MoveResistance),
                
                PoisonApplyChance = IStatusApplierModule.ClampPoisonApplyChance(PoisonApplyChance),
                DebuffApplyChance = IStatusApplierModule.ClampDebuffApplyChance(DebuffApplyChance),
                MoveApplyChance   = IStatusApplierModule.ClampMoveApplyChance(MoveApplyChance),
                
                StunMitigation = IStunModule.ClampStunMitigation(StunMitigation),
                
                Speed          = IStatsModule.ClampSpeed(Speed),
                Accuracy       = IStatsModule.ClampAccuracy(Accuracy),
                CriticalChance = IStatsModule.ClampCriticalChance(CriticalChance),
                Dodge          = IStatsModule.ClampDodge(Dodge),
                
                DamageLower = clampedLowerDamage,
                DamageUpper = clampedUpperDamage,
                
                PrimaryUpgrades   = new Dictionary<PrimaryUpgrade, int>(PrimaryUpgrades),
                SecondaryUpgrades = new Dictionary<SecondaryUpgrade, int>(SecondaryUpgrades),
                
                AvailablePerkPoints      = AvailablePerkPoints,
                AvailablePrimaryPoints   = AvailablePrimaryPoints,
                AvailableSecondaryPoints = AvailableSecondaryPoints,
                
                PrimaryUpgradeOptions   = PrimaryUpgradeOptions.ToDictionary  (keySelector: pair => pair.Key, elementSelector: pair => pair.Value.ToArrayNonAlloc()),
                SecondaryUpgradeOptions = SecondaryUpgradeOptions.ToDictionary(keySelector: pair => pair.Key, elementSelector: pair => pair.Value.ToArrayNonAlloc()),
                
                SexualExpByRace = new Dictionary<Race, int>(SexualExpByRace),
            };
        }

        public bool IsDataValid(StringBuilder errors)
        {
            if (CharacterDatabase.GetCharacter(Key).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(CharacterStats), nameof(Key), ": ", Key.ToString(), " does not exist in database.");
                return false;
            }
            
            if (PrimaryUpgrades == null)
            {
                errors.AppendLine("Invalid ", nameof(CharacterStats), ". ", nameof(PrimaryUpgrades), "is null");
                return false;
            }
            
            if (SecondaryUpgrades == null)
            {
                errors.AppendLine("Invalid ", nameof(CharacterStats), ". ", nameof(SecondaryUpgrades), "is null");
                return false;
            }
            
            if (Randomizer == null)
            {
                errors.AppendLine("Invalid ", nameof(CharacterStats), ". ", nameof(Randomizer), "is null");
                return false;
            }
            
            if (PrimaryUpgradeOptions == null)
            {
                errors.AppendLine("Invalid ", nameof(CharacterStats), ". ", nameof(PrimaryUpgradeOptions), "is null");
                return false;
            }
            
            if (SecondaryUpgradeOptions == null)
            {
                errors.AppendLine("Invalid ", nameof(CharacterStats), ". ", nameof(SecondaryUpgradeOptions), "is null");
                return false;
            }
            
            if (SexualExpByRace == null)
            {
                errors.AppendLine("Invalid ", nameof(CharacterStats), ". ", nameof(SexualExpByRace), "is null");
                return false;
            }
            
            if (SkillSet == null)
            {
                errors.AppendLine("Invalid ", nameof(CharacterStats), ". ", nameof(SkillSet), "is null");
                return false;
            }

            return true;
        }
    }
}