using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Core.Combat.Scripts;
using Core.Main_Characters.Ethel.Combat;
using Core.Main_Characters.Nema.Combat;
using Core.Utils.Extensions;
using Core.World_Map.Scripts;
using KGySoft.CoreLibraries;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace Core.Save_Management.SaveObjects
{
    public static class VariablesName
    {
        public delegate void StringSetter(Save save, CleanString value, CleanString variableName);
        public delegate void IntSetter(Save save, int value, CleanString variableName);
        public delegate void BoolSetter(Save save, bool value, CleanString variableName);
        public delegate CleanString StringGetter(Save save, CleanString variableName);
        public delegate int IntGetter(Save save, CleanString variableName);
        public delegate bool BoolGetter(Save save, CleanString variableName);
        
        private static readonly StringBuilder Builder = new();

        public static readonly ReadOnlyDictionary<CleanString, StringSetter> StringSetters;
        public static readonly ReadOnlyDictionary<CleanString, IntSetter> IntSetters;
        public static readonly ReadOnlyDictionary<CleanString, BoolSetter> BoolSetters;

        public static readonly ReadOnlyDictionary<CleanString, StringGetter> StringGetters;
        public static readonly ReadOnlyDictionary<CleanString, IntGetter> IntGetters;
        public static readonly ReadOnlyDictionary<CleanString, BoolGetter> BoolGetters;

        public static readonly CleanString Ethel_Lust = StatName(Ethel.GlobalKey,                     GeneralStat.Lust);
        public static readonly CleanString Ethel_Experience = StatName(Ethel.GlobalKey,               GeneralStat.Experience);
        public static readonly CleanString Ethel_AvailablePerkPoints = StatName(Ethel.GlobalKey,      GeneralStat.PerkPoints);
        public static readonly CleanString Ethel_AvailablePrimaryPoints = StatName(Ethel.GlobalKey,   GeneralStat.PrimaryPoints);
        public static readonly CleanString Ethel_AvailableSecondaryPoints = StatName(Ethel.GlobalKey, GeneralStat.SecondaryPoints);

        public static readonly CleanString Nema_Lust = StatName(Nema.GlobalKey,                     GeneralStat.Lust);
        public static readonly CleanString Nema_Experience = StatName(Nema.GlobalKey,               GeneralStat.Experience);
        public static readonly CleanString Nema_AvailablePerkPoints = StatName(Nema.GlobalKey,      GeneralStat.PerkPoints);
        public static readonly CleanString Nema_AvailablePrimaryPoints = StatName(Nema.GlobalKey,   GeneralStat.PrimaryPoints);
        public static readonly CleanString Nema_AvailableSecondaryPoints = StatName(Nema.GlobalKey, GeneralStat.SecondaryPoints);

        public static readonly CleanString EnemyMetPrefix = "enemy_met_";
        public static readonly CleanString Current_Location = "Current_Location";
        public static readonly CleanString Combat_Order = nameof(Save.CombatOrder);
        public static readonly CleanString Nema_Exhaustion = nameof(Save.NemaExhaustion);
        public static readonly CleanString Nema_ClearingMist = nameof(Save.IsNemaClearingMist);
        
        /// <param name="characterName"> INPUT IS CHARACTER NAME NOT KEY!! </param>
        private static CleanString EnemyMetName(CleanString characterName) => Builder.Override(EnemyMetPrefix.ToString(), characterName.ToString()).ToString();

        public static CleanString AssignedSkillName(CleanString characterKey, int index) => Builder.Override(characterKey.ToString(), "_skill_", index.ToString()).ToString();
        private static CleanString AssignSkillSlotlessName(CleanString characterKey) => Builder.Override(characterKey.ToString(),     "_assign_skill").ToString();
        private static CleanString UnassignSkillName(CleanString characterKey) => Builder.Override(characterKey.ToString(),           "_unassign_skill").ToString();

        static VariablesName()
        {
            StringSetters = new ReadOnlyDictionary<CleanString, StringSetter>(new Dictionary<CleanString, StringSetter>
            {
                [Current_Location] = (save, value, _) =>
                {
                    if (Enum.TryParse(value.ToString(), out LocationEnum location) == false)
                    {
                        Debug.LogWarning($"Failed to parse location while trying to set current location: {value.ToString()}");
                        return;
                    }

                    save.Location = location;
                },
                [Combat_Order] = (save, value, _) => save.SetCombatOrder(characterIds: value.ToString().Split('_').Select(s => (CleanString) s)),
                [AssignedSkillName(Ethel.GlobalKey, index: 0)] = (save, value, _) => save.OverrideSkill(Ethel.GlobalKey, value, slotIndex: 0),
                [AssignedSkillName(Ethel.GlobalKey, index: 1)] = (save, value, _) => save.OverrideSkill(Ethel.GlobalKey, value, slotIndex: 1),
                [AssignedSkillName(Ethel.GlobalKey, index: 2)] = (save, value, _) => save.OverrideSkill(Ethel.GlobalKey, value, slotIndex: 2),
                [AssignedSkillName(Ethel.GlobalKey, index: 3)] = (save, value, _) => save.OverrideSkill(Ethel.GlobalKey, value, slotIndex: 3),
                [AssignSkillSlotlessName(Ethel.GlobalKey)]     = (save, value, _) => save.AssignSkill(Ethel.GlobalKey, value),
                [UnassignSkillName(Ethel.GlobalKey)]           = (save, value, _) => save.UnassignSkill(Ethel.GlobalKey, value),
                [AssignedSkillName(Nema.GlobalKey, index: 0)]  = (save, value, _) => save.OverrideSkill(Nema.GlobalKey, value, slotIndex: 0),
                [AssignedSkillName(Nema.GlobalKey, index: 1)]  = (save, value, _) => save.OverrideSkill(Nema.GlobalKey, value, slotIndex: 1),
                [AssignedSkillName(Nema.GlobalKey, index: 2)]  = (save, value, _) => save.OverrideSkill(Nema.GlobalKey, value, slotIndex: 2),
                [AssignedSkillName(Nema.GlobalKey, index: 3)]  = (save, value, _) => save.OverrideSkill(Nema.GlobalKey, value, slotIndex: 3),
                [AssignSkillSlotlessName(Nema.GlobalKey)]      = (save, value, _) => save.AssignSkill(Nema.GlobalKey, value),
                [UnassignSkillName(Nema.GlobalKey)]            = (save, value, _) => save.UnassignSkill(Nema.GlobalKey, value),
            });
            
            Dictionary<CleanString, IntSetter> intSetters = new();
            foreach (GeneralStat stat in Enum<GeneralStat>.GetValues())
            {
                intSetters.Add(StatName(Ethel.GlobalKey, stat), (save, value, _) => save.SetStat(Ethel.GlobalKey, stat, value));
                intSetters.Add(StatName(Nema.GlobalKey,  stat), (save, value, _) => save.SetStat(Nema.GlobalKey,  stat, value));
            }

            foreach (Race race in Enum<Race>.GetValues())
            {
                intSetters.Add(SexualExpByRaceName(Ethel.GlobalKey, race), (save, value, _) => save.SetSexualExp(Ethel.GlobalKey, race, value));
                intSetters.Add(SexualExpByRaceName(Nema.GlobalKey,  race), (save, value, _) => save.SetSexualExp(Nema.GlobalKey,  race, value));
            }

            intSetters[Ethel_Experience] = (save, value, _) => save.SetExperience(Ethel.GlobalKey, value);
            intSetters[Ethel_Lust]       = (save, value, _) => save.SetLust(Ethel.GlobalKey, value);
            intSetters[Nema_Experience]  = (save, value, _) => save.SetExperience(Nema.GlobalKey,  value);
            intSetters[Nema_Lust]        = (save, value, _) => save.SetLust(Nema.GlobalKey,  value);
            intSetters[Nema_Exhaustion]  = (save, value, _) => save.SetNemaExhaustion(       value);
            
            IntSetters = new ReadOnlyDictionary<CleanString, IntSetter>(intSetters);

            BoolSetters = new ReadOnlyDictionary<CleanString, BoolSetter>(new Dictionary<CleanString, BoolSetter>
            {
                [Nema_ClearingMist] = (save, value, _) => save.SetNemaClearingMist(value)
            });

            StringGetters = new ReadOnlyDictionary<CleanString, StringGetter>(new Dictionary<CleanString, StringGetter>()
            {
                [Current_Location] = (save, _) => Enum<LocationEnum>.ToString(save.Location),
                [Combat_Order] = (save, _) => save.GetCombatOrderAsString().ToString(),
                [AssignedSkillName(Ethel.GlobalKey, index: 0)] = (save, _) => save.GetSkill(Ethel.GlobalKey, index: 0).SomeOrDefault().ToString(),
                [AssignedSkillName(Ethel.GlobalKey, index: 1)] = (save, _) => save.GetSkill(Ethel.GlobalKey, index: 1).SomeOrDefault().ToString(),
                [AssignedSkillName(Ethel.GlobalKey, index: 2)] = (save, _) => save.GetSkill(Ethel.GlobalKey, index: 2).SomeOrDefault().ToString(),
                [AssignedSkillName(Ethel.GlobalKey, index: 3)] = (save, _) => save.GetSkill(Ethel.GlobalKey, index: 3).SomeOrDefault().ToString(),
                [AssignedSkillName(Nema.GlobalKey,  index: 0)] = (save, _) => save.GetSkill(Nema.GlobalKey,  index: 0).SomeOrDefault().ToString(),
                [AssignedSkillName(Nema.GlobalKey,  index: 1)] = (save, _) => save.GetSkill(Nema.GlobalKey,  index: 1).SomeOrDefault().ToString(),
                [AssignedSkillName(Nema.GlobalKey,  index: 2)] = (save, _) => save.GetSkill(Nema.GlobalKey,  index: 2).SomeOrDefault().ToString(),
                [AssignedSkillName(Nema.GlobalKey,  index: 3)] = (save, _) => save.GetSkill(Nema.GlobalKey,  index: 3).SomeOrDefault().ToString(),
            });

            Dictionary<CleanString, IntGetter> intGetters = new();
            foreach (GeneralStat stat in Enum<GeneralStat>.GetValues())
            {
                intGetters.Add(StatName(Ethel.GlobalKey, stat), (save, _) => save.GetStat(Ethel.GlobalKey, stat));
                intGetters.Add(StatName(Nema.GlobalKey,  stat), (save, _) => save.GetStat(Nema.GlobalKey,  stat));
            }
            
            foreach (Race race in Enum<Race>.GetValues())
            {
                intGetters.Add(SexualExpByRaceName(Ethel.GlobalKey, race), (save, _) => save.GetSexualExp(Ethel.GlobalKey, race));
                intGetters.Add(SexualExpByRaceName(Nema.GlobalKey,  race), (save, _) => save.GetSexualExp(Nema.GlobalKey,  race));
            }

            intGetters.Add(Nema_Exhaustion, (save, _) => save.NemaExhaustion);
            
            IntGetters = new ReadOnlyDictionary<CleanString, IntGetter>(intGetters);
            
            Dictionary<CleanString, BoolGetter> locationGetters = new();
            foreach (LocationEnum location in Enum<LocationEnum>.GetValues())
            {
                CleanString locationName = Enum<LocationEnum>.ToString(location);
                locationGetters[locationName] = (save, _) => save.LocationsUnlocked.Contains(location);
            }

            BoolGetters = new ReadOnlyDictionary<CleanString, BoolGetter>(locationGetters);
        }
        
        public static CleanString StatName(CleanString key, GeneralStat stat) => Builder.Override(key.ToString(), '_', stat.GetName().ToString()).ToString();
        
        public static CleanString AllocatedPrimaryUpgradeName(CleanString key,   PrimaryUpgrade upgrade)   => Builder.Override(key.ToString(), "_allocated_", upgrade.GetName().ToString()).ToString();
        public static CleanString AllocatedSecondaryUpgradeName(CleanString key, SecondaryUpgrade upgrade) => Builder.Override(key.ToString(), "_allocated_", upgrade.GetName().ToString()).ToString();

        public static CleanString PerkPrefix(CleanString characterKey)  => Builder.Override("perk_",  characterKey.ToString(), '_').ToString();
        public static CleanString SkillPrefix(CleanString characterKey) => Builder.Override("skill_", characterKey.ToString(), '_').ToString();
        
        public static CleanString EnabledPerkPrefix() => "enabled_";
        public static CleanString EnabledPerkName(CleanString characterKey, CleanString perkKey) => Builder.Override(EnabledPerkPrefix().ToString(), PerkPrefix(characterKey).ToString(), perkKey.ToString()).ToString();

        private static CleanString GetName(this GeneralStat stat)         => Enum<GeneralStat>.ToString(stat);
        private static CleanString GetName(this PrimaryUpgrade upgrade)   => upgrade.ToGeneral().GetName();
        private static CleanString GetName(this SecondaryUpgrade upgrade) => upgrade.ToGeneral().GetName();
        
        public static CleanString SexualExpByRaceName(CleanString statsKey, Race race) => Builder.Override("sexual_exp_", statsKey.ToString(), '_' , Enum<Race>.ToString(race)).ToString();
    }
}