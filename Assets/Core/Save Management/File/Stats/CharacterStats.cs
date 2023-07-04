using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Interfaces;
using Core.Combat.Scripts.Interfaces.Modules;
using Data.Main_Characters.Ethel;
using JetBrains.Annotations;
using ListPool;
using Main_Database.Combat;
using UnityEngine;
using Utils.Extensions;
using Utils.Math;
using Utils.Patterns;
using Random = System.Random;

namespace Save_Management.Stats
{
    [DataContract]
    public class CharacterStats : IReadonlyCharacterStats, IDeepCloneable<CharacterStats>
    {
        [DataMember] public CleanString Key { get; init; }
        [DataMember] public Random Randomizer { get; init; }
        [DataMember] public float Experience { get; set; }

        [DataMember] public SkillSet SkillSet { get; set; }
        public IReadOnlySkillSet GetSkillSet() => SkillSet;

        [DataMember] public uint DamageLower { get; set; }
        [DataMember] public uint DamageUpper { get; set; }

        [DataMember] public uint Stamina { get; set; }
        [DataMember] public float Resilience { get; set; }
        
        [DataMember] public uint Lust { get; set; }
        [DataMember] public ClampedPercentage Temptation { get; set; }
        [DataMember] public uint OrgasmLimit { get; set; }
        [DataMember] public uint OrgasmCount { get; set; }
        [DataMember] public float Composure { get; set; }
        [DataMember] public ClampedPercentage Corruption { get; set; }

        [DataMember] public float PoisonResistance { get; set; }
        [DataMember] public float PoisonApplyChance { get; set; }

        [DataMember] public float DebuffResistance { get; set; }
        [DataMember] public float DebuffApplyChance { get; set; }

        [DataMember] public float MoveResistance { get; set; }
        [DataMember] public float MoveApplyChance { get; set; }

        [DataMember] public float StunRecoverySpeed { get; set; }
        
        [DataMember] public float Speed { get; set; }
        [DataMember] public float Accuracy { get; set; }
        [DataMember] public float CriticalChance { get; set; }
        [DataMember] public float Dodge { get; set; }

        [DataMember] public Dictionary<PrimaryUpgrade, uint> PrimaryUpgrades { get; init; }
        [DataMember] public Dictionary<SecondaryUpgrade, uint> SecondaryUpgrades { get; init; }

        [DataMember] public uint AvailablePerkPoints { get; set; }
        [DataMember] public uint AvailablePrimaryPoints { get; set; }
        [DataMember] public uint AvailableSecondaryPoints { get; set; }

        [DataMember] public Dictionary<uint, List<PrimaryUpgrade>> PrimaryUpgradeOptions { get; init; }
        [DataMember] public Dictionary<uint, List<SecondaryUpgrade>> SecondaryUpgradeOptions { get; init; }
        
        [DataMember] public Dictionary<Race, uint> SexualExpByRace { get; init; }
        
        public uint Level => (Experience / ExperienceCalculator.ExperienceNeededForLevelUp).FloorToUInt();
        
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
        public ValueListPool<CleanString> GetUnlockedPerksAndSkills(Save save)
        {
            ValueListPool<CleanString> list = new(32);
            CleanString perkPrefix = VariablesName.PerkPrefix(Key);
            CleanString skillPrefix = VariablesName.SkillPrefix(Key);
            
            foreach ((CleanString key, bool value) in save.Booleans)
                if (value == true && (key.StartsWith(ref perkPrefix) || key.StartsWith(ref skillPrefix)))
                    list.Add(key);
            
            return list;
        }

        [MustUseReturnValue]
        public ValueListPool<CleanString> GetUnlockedPerks(Save save)
        {
            ValueListPool<CleanString> list = new(32);
            CleanString perkPrefix = VariablesName.PerkPrefix(Key);
            
            foreach ((CleanString key, bool value) in save.Booleans)
                if (value == true && key.StartsWith(ref perkPrefix))
                    list.Add(key);
            
            return list;
        }
        
        [MustUseReturnValue]
        public ValueListPool<CleanString> GetUnlockedSkills(Save save)
        {
            ValueListPool<CleanString> list = new(32);
            CleanString skillPrefix = VariablesName.SkillPrefix(Key);
            
            foreach ((CleanString key, bool value) in save.Booleans)
                if (value == true && key.StartsWith(ref skillPrefix))
                    list.Add(key);
            
            return list;
        }

        [MustUseReturnValue]
        public ValueListPool<CleanString> GetEnabledPerks(Save save)
        {
            ValueListPool<CleanString> list = new(32);
            foreach (CleanString perkKey in GetUnlockedPerks(save))
            {
                CleanString activePerk = VariablesName.EnabledPerkName(characterKey: Key, perkKey);
                if (save.Booleans.TryGetValue(activePerk, out bool value) && value)
                    list.Add(perkKey);
            }
            
            return list;
        }

        public bool IsPerkUnlocked(CleanString key, Save save)
        {
            foreach (CleanString perk in GetUnlockedPerks(save))
                if (perk == key)
                    return true;

            return false;
        }
        
        public bool IsSkillUnlocked(CleanString key, Save save)
        {
            foreach (CleanString skill in GetUnlockedSkills(save))
                if (skill == key)
                    return true;

            return false;
        }

        public uint GetUsedPrimaryPoints()
        {
            uint usedPoints = 0;
            foreach (uint upgradeCount in PrimaryUpgrades.Values)
                usedPoints += upgradeCount;

            return usedPoints;
        }

        public uint GetUsedSecondaryPoints()
        {
            uint usedPoints = 0;
            foreach (uint upgradeCount in SecondaryUpgrades.Values)
                usedPoints += upgradeCount;

            return usedPoints;
        }

        public void SetValue(GeneralStat stat, float newValue)
        {
            switch (stat)
            {
                case GeneralStat.DamageLower:       (DamageLower, DamageUpper) = IStatsModule.ClampRoundedDamage(newValue.FloorToUInt(), DamageUpper); break;
                case GeneralStat.DamageUpper:       (DamageLower, DamageUpper) = IStatsModule.ClampRoundedDamage(DamageLower,            newValue.FloorToUInt()); break;
                case GeneralStat.Stamina:           Stamina = newValue.FloorToUInt(); break;
                case GeneralStat.Lust:              Lust = ILustModule.ClampLust(newValue.FloorToUInt()); break;
                case GeneralStat.OrgasmLimit:       OrgasmLimit = newValue.FloorToUInt(); break;
                case GeneralStat.OrgasmCount:       OrgasmCount = ILustModule.ClampOrgasmCount(newValue.FloorToUInt(), OrgasmLimit); break;
                case GeneralStat.Composure:         Composure = newValue; break;
                case GeneralStat.Speed:             Speed = newValue; break;
                case GeneralStat.Accuracy:          Accuracy = newValue; break;
                case GeneralStat.CriticalChance:    CriticalChance = newValue; break;
                case GeneralStat.Dodge:             Dodge = newValue; break;
                case GeneralStat.Resilience:        Resilience = newValue; break;
                case GeneralStat.PoisonResistance:  PoisonResistance = newValue; break;
                case GeneralStat.PoisonApplyChance: PoisonApplyChance = newValue; break;
                case GeneralStat.DebuffResistance:  DebuffResistance = newValue; break;
                case GeneralStat.DebuffApplyChance: DebuffApplyChance = newValue; break;
                case GeneralStat.MoveResistance:    MoveResistance = newValue; break;
                case GeneralStat.MoveApplyChance:   MoveApplyChance = newValue; break;
                case GeneralStat.StunRecoverySpeed: StunRecoverySpeed = newValue; break;
                case GeneralStat.Temptation:        Temptation = newValue; break;
                case GeneralStat.Corruption:        Corruption = newValue; break;
                default:                            throw new ArgumentOutOfRangeException(nameof(stat), stat, null);
            }
        }

        public float GetValue(PrimaryUpgrade upgrade) => upgrade switch
        {
            PrimaryUpgrade.Accuracy   => Accuracy + UpgradeHelper.GetUpgradeFull(PrimaryUpgrades[PrimaryUpgrade.Accuracy],       PrimaryUpgrade.Accuracy),
            PrimaryUpgrade.Dodge      => Dodge + UpgradeHelper.GetUpgradeFull(PrimaryUpgrades[PrimaryUpgrade.Dodge],             PrimaryUpgrade.Dodge),
            PrimaryUpgrade.Critical   => CriticalChance + UpgradeHelper.GetUpgradeFull(PrimaryUpgrades[PrimaryUpgrade.Critical], PrimaryUpgrade.Critical),
            PrimaryUpgrade.Resilience => Resilience + UpgradeHelper.GetUpgradeFull(PrimaryUpgrades[PrimaryUpgrade.Resilience],   PrimaryUpgrade.Resilience),
            _                         => throw new ArgumentException($"Invalid primary upgrade: {upgrade}")
        };

        public float GetValue(SecondaryUpgrade upgrade) => upgrade switch
        {
            SecondaryUpgrade.PoisonResistance  => PoisonResistance + UpgradeHelper.GetUpgradeFull(SecondaryUpgrades[SecondaryUpgrade.PoisonResistance],   SecondaryUpgrade.PoisonResistance),
            SecondaryUpgrade.DebuffResistance  => DebuffResistance + UpgradeHelper.GetUpgradeFull(SecondaryUpgrades[SecondaryUpgrade.DebuffResistance],   SecondaryUpgrade.DebuffResistance),
            SecondaryUpgrade.MoveResistance    => MoveResistance + UpgradeHelper.GetUpgradeFull(SecondaryUpgrades[SecondaryUpgrade.MoveResistance],       SecondaryUpgrade.MoveResistance),
            SecondaryUpgrade.StunRecoverySpeed => StunRecoverySpeed + UpgradeHelper.GetUpgradeFull(SecondaryUpgrades[SecondaryUpgrade.StunRecoverySpeed], SecondaryUpgrade.StunRecoverySpeed),
            SecondaryUpgrade.Composure         => Composure + UpgradeHelper.GetUpgradeFull(SecondaryUpgrades[SecondaryUpgrade.Composure],                 SecondaryUpgrade.Composure),
            SecondaryUpgrade.PoisonApplyChance => PoisonApplyChance + UpgradeHelper.GetUpgradeFull(SecondaryUpgrades[SecondaryUpgrade.PoisonApplyChance], SecondaryUpgrade.PoisonApplyChance),
            SecondaryUpgrade.DebuffApplyChance => DebuffApplyChance + UpgradeHelper.GetUpgradeFull(SecondaryUpgrades[SecondaryUpgrade.DebuffApplyChance], SecondaryUpgrade.DebuffApplyChance),
            SecondaryUpgrade.MoveApplyChance   => MoveApplyChance + UpgradeHelper.GetUpgradeFull(SecondaryUpgrades[SecondaryUpgrade.MoveApplyChance],     SecondaryUpgrade.MoveApplyChance),
            _                                  => throw new ArgumentException($"Invalid secondary upgrade: {upgrade}")
        };

        public float GetValue(GeneralStat stat)
        {
            return stat switch
            {
                GeneralStat.Experience         => Experience,
                GeneralStat.Speed              => Speed,
                GeneralStat.Stamina            => Stamina,
                GeneralStat.Resilience         => Resilience + UpgradeHelper.GetUpgradeFull(PrimaryUpgrades[PrimaryUpgrade.Resilience], PrimaryUpgrade.Resilience),
                GeneralStat.DamageLower        => DamageLower,
                GeneralStat.DamageUpper        => DamageUpper,
                GeneralStat.Lust               => Lust,
                GeneralStat.Temptation         => Temptation,
                GeneralStat.Corruption         => Corruption,
                GeneralStat.Composure          => Composure + UpgradeHelper.GetUpgradeFull(SecondaryUpgrades[SecondaryUpgrade.Composure], SecondaryUpgrade.Composure),
                GeneralStat.OrgasmLimit        => OrgasmLimit,
                GeneralStat.OrgasmCount        => OrgasmCount,
                GeneralStat.Accuracy           => Accuracy + UpgradeHelper.GetUpgradeFull(PrimaryUpgrades[PrimaryUpgrade.Accuracy],                       PrimaryUpgrade.Accuracy),
                GeneralStat.CriticalChance     => CriticalChance + UpgradeHelper.GetUpgradeFull(PrimaryUpgrades[PrimaryUpgrade.Critical],                 PrimaryUpgrade.Critical),
                GeneralStat.Dodge              => Dodge + UpgradeHelper.GetUpgradeFull(PrimaryUpgrades[PrimaryUpgrade.Dodge],                             PrimaryUpgrade.Dodge),
                GeneralStat.StunRecoverySpeed  => StunRecoverySpeed + UpgradeHelper.GetUpgradeFull(SecondaryUpgrades[SecondaryUpgrade.StunRecoverySpeed], SecondaryUpgrade.StunRecoverySpeed),
                GeneralStat.PoisonResistance   => PoisonResistance + UpgradeHelper.GetUpgradeFull(SecondaryUpgrades[SecondaryUpgrade.PoisonResistance],   SecondaryUpgrade.PoisonResistance),
                GeneralStat.PoisonApplyChance  => PoisonApplyChance + UpgradeHelper.GetUpgradeFull(SecondaryUpgrades[SecondaryUpgrade.PoisonApplyChance], SecondaryUpgrade.PoisonApplyChance),
                GeneralStat.DebuffResistance   => DebuffResistance + UpgradeHelper.GetUpgradeFull(SecondaryUpgrades[SecondaryUpgrade.DebuffResistance],   SecondaryUpgrade.DebuffResistance),
                GeneralStat.DebuffApplyChance  => DebuffApplyChance + UpgradeHelper.GetUpgradeFull(SecondaryUpgrades[SecondaryUpgrade.DebuffApplyChance], SecondaryUpgrade.DebuffApplyChance),
                GeneralStat.MoveResistance     => MoveResistance + UpgradeHelper.GetUpgradeFull(SecondaryUpgrades[SecondaryUpgrade.MoveResistance],       SecondaryUpgrade.MoveResistance),
                GeneralStat.MoveApplyChance    => MoveApplyChance + UpgradeHelper.GetUpgradeFull(SecondaryUpgrades[SecondaryUpgrade.MoveApplyChance],     SecondaryUpgrade.MoveApplyChance),
                GeneralStat.PrimaryPoints      => AvailablePrimaryPoints,
                GeneralStat.SecondaryPoints    => AvailableSecondaryPoints,
                GeneralStat.PerkPoints         => AvailablePerkPoints,
                GeneralStat.ArousalApplyChance => 0f,
                _                              => throw new ArgumentOutOfRangeException(nameof(stat), stat, message:$"Unhandled stat: {stat}")
            };
        }

        public (uint lower, uint upper) GetDamage() => (DamageLower, DamageUpper);

        public List<PrimaryUpgrade> GetPrimaryUpgradeOptions(uint currentTier) => UpgradeHelper.GetPrimaryUpgradeOptions(currentTier, PrimaryUpgradeOptions, Randomizer);
        public List<SecondaryUpgrade> GetSecondaryUpgradeOptions(uint currentTier) => UpgradeHelper.GetSecondaryUpgradeOptions(currentTier, SecondaryUpgradeOptions, Randomizer);

        public CharacterStats DeepClone()
        {
            return new CharacterStats
            {
                Key = Key,
                Randomizer = Randomizer.DeepClone(),
                SkillSet = SkillSet.DeepClone(),
                Experience = Experience,
                
                Stamina = Stamina,
                Resilience = Resilience,
                
                Lust = Lust,
                Temptation = Temptation,
                OrgasmCount = OrgasmCount,
                OrgasmLimit = OrgasmLimit,
                Composure = Composure,
                Corruption = Corruption,

                PoisonResistance = PoisonResistance,
                PoisonApplyChance = PoisonApplyChance,
                
                DebuffResistance = DebuffResistance,
                DebuffApplyChance = DebuffApplyChance,
                
                MoveResistance = MoveResistance,
                MoveApplyChance = MoveApplyChance,
                
                StunRecoverySpeed = StunRecoverySpeed,
                
                Speed = Speed,
                Accuracy = Accuracy,
                CriticalChance = CriticalChance,
                Dodge = Dodge,
                
                DamageLower = DamageLower,
                DamageUpper = DamageUpper,
                
                PrimaryUpgrades = new Dictionary<PrimaryUpgrade, uint>(PrimaryUpgrades),
                SecondaryUpgrades = new Dictionary<SecondaryUpgrade, uint>(SecondaryUpgrades),
                
                AvailablePerkPoints = AvailablePerkPoints,
                AvailablePrimaryPoints = AvailablePrimaryPoints,
                AvailableSecondaryPoints = AvailableSecondaryPoints,
                
                PrimaryUpgradeOptions = PrimaryUpgradeOptions.ToDictionary(pair => pair.Key, pair => new List<PrimaryUpgrade>(pair.Value)),
                SecondaryUpgradeOptions = SecondaryUpgradeOptions.ToDictionary(pair => pair.Key, pair => new List<SecondaryUpgrade>(pair.Value)),
                
                SexualExpByRace = new Dictionary<Race, uint>(SexualExpByRace),
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