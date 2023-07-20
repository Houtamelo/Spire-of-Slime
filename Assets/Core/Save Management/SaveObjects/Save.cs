using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Managers;
using Core.Main_Characters.Ethel.Combat;
using Core.Main_Characters.Nema.Combat;
using Core.Main_Database;
using Core.Main_Database.Combat;
using Core.Main_Database.Visual_Novel;
using Core.Utils.Collections;
using Core.Utils.Extensions;
using Core.Utils.Handlers;
using Core.Utils.Patterns;
using Core.World_Map.Scripts;
using Data.Main_Characters.Nema;
using JetBrains.Annotations;
using KGySoft.CoreLibraries;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Core.Save_Management.SaveObjects
{
    public delegate void BoolChanged(CleanString variableName, bool oldValue, bool newValue);
    public delegate void IntChanged(CleanString variableName, int oldValue, int newValue);
    public delegate void StringChanged(CleanString variableName, CleanString oldValue, CleanString newValue);
    
    public partial class Save
    {
        public static bool VALIDATE = true;
        
        private static VariableDatabase VariableDatabase => DatabaseManager.Instance.VariableDatabase;
        private static readonly StringBuilder Builder = new();

        public static System.Random Random => Current != null ? Current.GeneralRandomizer : new System.Random();

        public static readonly ValueHandler<Save> Handler = new();
        public static Save Current => Handler.Value;

        public static bool AssertInstance(out Save save)
        {
            if (Current != null)
            {
                save = Current;
                return true;
            }

            Debug.LogWarning("No save active.");
            save = null;
            return false;
        }

        public static event BoolChanged BoolChanged;
        public static event IntChanged IntChanged;
        public static event StringChanged StringChanged;

        public LocationEnum Location;
        public static event Action<LocationEnum> LocationChanged;

    #region NemaStatus
        public int NemaExhaustion { get; private set; }
        public ExhaustionEnum NemaExhaustionAsEnum => NemaExhaustion switch
        {
            >= NemaStatus.HighExhaustion   => ExhaustionEnum.High,
            >= NemaStatus.MediumExhaustion => ExhaustionEnum.Medium,
            >= NemaStatus.LowExhaustion    => ExhaustionEnum.Low,
            _                              => ExhaustionEnum.None
        };
        
        public static event Action<NemaStatus> NemaExhaustionChanged;

        public void CheckNemaCombatStatus()
        {
            NemaExhaustionChanged?.Invoke(GetFullNemaStatus());
        }

        [System.Diagnostics.Contracts.Pure]
        public NemaStatus GetFullNemaStatus()
        {
            (int, int) exhaustion = (NemaExhaustion, NemaExhaustion);
            (bool isInCombat, bool isStanding) combatStatus = GetNemaCombatStatus();
            (bool, bool) setToClearingMist = (IsNemaClearingMist, IsNemaClearingMist);
            (bool, bool) isInCombat = (combatStatus.isInCombat, combatStatus.isInCombat);
            (bool, bool) isStanding = (combatStatus.isStanding, combatStatus.isStanding);
            return new NemaStatus(exhaustion, setToClearingMist, isInCombat, isStanding);
        }
        
        [System.Diagnostics.Contracts.Pure]
        public (bool isInCombat, bool isStanding) GetNemaCombatStatus()
        {
            if (CombatManager.Instance.TrySome(out CombatManager combatManager) == false)
                return (isInCombat: false, isStanding: false);

            bool isInCombat = false;
            bool isStanding = false;
            
            foreach (CharacterStateMachine character in combatManager.Characters.FixedOnLeftSide) // No need to check right characters cause if Nema is there she won't be clearing mist anyway
            {
                if (character.Script is not Nema)
                    continue;

                isInCombat = true;
                isStanding = character.StateEvaluator.FullPureEvaluate() is not ({ Defeated: true } or { Corpse: true } or { Downed: true } or { Grappled: true });
                break;
            }

            return (isInCombat, isStanding);
        }
        
        public void SetNemaExhaustion(int newValue)
        {
            if (NemaExhaustion == newValue)
                return;
            
            SetDirty();
            int oldValue = NemaExhaustion;
            NemaExhaustion = newValue;
            (bool isInCombat, bool isStanding) combatStatus = GetNemaCombatStatus();
            (int, int) exhaustion = (oldValue, newValue);
            (bool, bool) isClearingMist = (IsNemaClearingMist, IsNemaClearingMist);
            (bool, bool) isInCombat = (combatStatus.isInCombat, combatStatus.isInCombat);
            (bool, bool) isStanding = (combatStatus.isStanding, combatStatus.isStanding);
            NemaStatus status = new(exhaustion, isClearingMist, isInCombat, isStanding);
            NemaExhaustionChanged?.Invoke(status);
            IntChanged?.Invoke(VariablesName.Nema_Exhaustion, oldValue, newValue);
        }

        public void ChangeNemaExhaustion(int delta)
        {
            SetDirty();
            int previousValue = NemaExhaustion;
            NemaExhaustion = NemaExhaustion + delta;
            if (previousValue == NemaExhaustion)
                return;

            (bool isInCombat, bool isStanding) combatStatus = GetNemaCombatStatus();
            (int, int) exhaustion = (previousValue, NemaExhaustion);
            (bool, bool) isClearingMist = (IsNemaClearingMist, IsNemaClearingMist);
            (bool, bool) isInCombat = (combatStatus.isInCombat, combatStatus.isInCombat);
            (bool, bool) isStanding = (combatStatus.isStanding, combatStatus.isStanding);
            NemaStatus status = new(exhaustion, isClearingMist, isInCombat, isStanding);
            NemaExhaustionChanged?.Invoke(status);
        }

        public void SetNemaClearingMist(bool newValue)
        {
            bool oldValue = IsNemaClearingMist;
            if (oldValue == newValue)
                return;
            
            SetDirty();
            IsNemaClearingMist = newValue;
            
            (bool isInCombat, bool isStanding) combatStatus = GetNemaCombatStatus();
            (int, int) exhaustion = (NemaExhaustion, NemaExhaustion);
            (bool, bool) isClearingMist = (oldValue, newValue);
            (bool, bool) isInCombat = (combatStatus.isInCombat, combatStatus.isInCombat);
            (bool, bool) isStanding = (combatStatus.isStanding, combatStatus.isStanding);
            NemaStatus status = new(exhaustion, isClearingMist, isInCombat, isStanding);
            NemaExhaustionChanged?.Invoke(status);
            BoolChanged?.Invoke(VariablesName.Nema_ClearingMist, oldValue, newValue);
        }
    #endregion

        public string Name { get; set; }
        public DateTime Date { get; set; }

        public Dictionary<CleanString, bool> Booleans;
        public Dictionary<CleanString, int> Ints;
        public Dictionary<CleanString, CleanString> Strings;

        private List<LocationEnum> _locationsUnlocked;
        public List<LocationEnum> LocationsUnlocked => _locationsUnlocked;

        private CharacterStats _ethelStats;
        public IReadonlyCharacterStats EthelStats => _ethelStats;

        private CharacterStats _nemaStats;
        public IReadonlyCharacterStats NemaStats => _nemaStats;

        public bool IsNemaClearingMist { get; private set; }

        public System.Random GeneralRandomizer { get; private set; }

        /// <summary> Sanitize before accessing. </summary>
        public List<CleanString> CombatOrder;

        private void SanitizeCombatOrder()
        {
            CombatOrder ??= new List<CleanString>();
            
            if (CombatOrder.Contains(Ethel.GlobalKey) == false)
                CombatOrder.Add(Ethel.GlobalKey);
            
            if (CombatOrder.Contains(Nema.GlobalKey) == false)
                CombatOrder.Add(Nema.GlobalKey);
        }

        /// <summary> Each element is a character's ID </summary>
        [System.Diagnostics.Contracts.Pure, NotNull]
        public IReadOnlyList<(CleanString key, bool bindToSave)> GetCombatOrderAsKeys()
        {
            SanitizeCombatOrder();
            return CombatOrder.Select(key => (key, bindToSave: true)).ToList();
        }

        [System.Diagnostics.Contracts.Pure, NotNull]
        public IReadOnlyList<(IReadonlyCharacterStats stats, bool bindToSave)> GetCombatOrderAsStats()
        {
            SanitizeCombatOrder();
            List<(IReadonlyCharacterStats stats, bool bindToSave)> characters = new(CombatOrder.Count);
            foreach (CleanString key in CombatOrder)
            {
                if (key == EthelStats.Key)
                    characters.Add((EthelStats, bindToSave: true));
                else if (key == NemaStats.Key)
                    characters.Add((NemaStats, bindToSave: true));
                else
                    Debug.LogWarning($"Character key: {key} is not valid");
            }
            
            return characters;
        }

        [System.Diagnostics.Contracts.Pure]
        public CleanString GetCombatOrderAsString()
        {
            SanitizeCombatOrder();
            Builder.Clear();
            Builder.Append(CombatOrder[0].ToString());
            for (int index = 1; index < CombatOrder.Count; index++)
            {
                CleanString characterId = CombatOrder[index];
                Builder.Append("_");
                Builder.Append(characterId.ToString());
            }
            
            return Builder.ToString();
        }

        public void SetCombatOrder([NotNull] IEnumerable<CleanString> characterIds)
        {
            SetDirty();
            SanitizeCombatOrder();
            CleanString oldValue = GetCombatOrderAsString();
            CombatOrder.Clear();
            CombatOrder.AddRange(characterIds);
            SanitizeCombatOrder();
            CleanString newValue = GetCombatOrderAsString();
            if (oldValue == newValue)
                return;

            StringChanged?.Invoke(VariablesName.Combat_Order, oldValue.ToString(), newValue.ToString());
        }

        public Ethel EthelScript => CharacterDatabase.DefaultEthel;
        public Nema NemaScript => CharacterDatabase.DefaultNema;
        
        [IgnoreDataMember]
        private int _hashCount;
        public int HashCount => _hashCount;

        public void SetDirty() => _hashCount++;

        public void SetLocation(LocationEnum location)
        {
            CleanString oldValue = GetVariable<string>(VariablesName.Current_Location);
            CleanString newValue = Enum<LocationEnum>.ToString(location);
            if (oldValue == newValue)
                return;
         
            SetDirty();
            Location = location;
            LocationChanged?.Invoke(location);
            StringChanged?.Invoke(VariablesName.Current_Location, oldValue, newValue);
        }
        
        private Save() {}

        private Save(string name)
        {
            SetDirty();
            
            Name = name;
            Date = DateTime.Now;

            Booleans = new Dictionary<CleanString, bool>();
            foreach (CleanString variableName in SaveUtils.GetTrueBooleansOnNewGame())
                Booleans[variableName] = true;

            foreach ((CleanString key, bool value) in VariableDatabase.DefaultBools)
                Booleans[key] = value;

            Ints = new Dictionary<CleanString, int>();
            
            foreach ((CleanString key, int value) in VariableDatabase.DefaultInts)
                Ints[key] = value;
            
            Strings = new Dictionary<CleanString, CleanString>();
            
            foreach ((CleanString key, CleanString value) in VariableDatabase.DefaultStrings)
                Strings[key] = value;

            _locationsUnlocked = new List<LocationEnum> { LocationEnum.Chapel };
            
            int seed = (int) (DateTime.Now.Millisecond * UnityEngine.Random.Range(1f, 1337f));
            GeneralRandomizer = new System.Random(seed);

            int ethelSeed = (int) Math.Pow(seed, 0.69);
            System.Random ethelRandomizer = new(ethelSeed);

            _ethelStats = new CharacterStats
            {
                Key = Ethel.GlobalKey,
                Stamina = 30,
                Accuracy = 0,
                AvailablePrimaryPoints = 0,
                AvailableSecondaryPoints = 0,
                CriticalChance = 0,
                DamageLower = 8,
                DamageUpper = 12,
                DebuffResistance = 0,
                MoveResistance = 10,
                StunMitigation = 10,
                Composure = 0,
                Speed = 100,
                Dodge = 10,
                Lust = 0,
                Resilience = 0,
                PoisonResistance = 0,
                OrgasmLimit = 3,
                SkillSet = new SkillSet(EthelSkills.Clash, EthelSkills.Sever, EthelSkills.Jolt, EthelSkills.Safeguard),
                Randomizer = ethelRandomizer,
                PrimaryUpgrades = new Dictionary<PrimaryUpgrade, int>(),
                SecondaryUpgrades = new Dictionary<SecondaryUpgrade, int>(),
                PrimaryUpgradeOptions = new Dictionary<int, PrimaryUpgrade[]>(),
                SecondaryUpgradeOptions = new Dictionary<int, SecondaryUpgrade[]>(),
                SexualExpByRace = new Dictionary<Race, int>(),
            };

            int nemaSeed = (int) Math.Pow(seed, 4.20);
            System.Random nemaRandomizer = new(nemaSeed);

            _nemaStats = new CharacterStats
            {
                Key = Nema.GlobalKey,
                Stamina =  20,
                Accuracy =  0,
                AvailablePrimaryPoints =  0,
                AvailableSecondaryPoints =  0,
                CriticalChance =  0,
                DamageLower =  6,
                DamageUpper =  10,
                DebuffResistance =  0,
                MoveResistance =  0,
                StunMitigation =  0,
                Composure =  0,
                Speed =  100,
                Dodge =  5,
                Lust =  0,
                Resilience =  0,
                PoisonResistance =  10,
                OrgasmLimit =  3,
                SkillSet =  new SkillSet(NemaSkills.Gawky.key, NemaSkills.Calm.key, string.Empty, string.Empty),
                Randomizer = nemaRandomizer,
                PrimaryUpgrades = new Dictionary<PrimaryUpgrade, int>(),
                SecondaryUpgrades = new Dictionary<SecondaryUpgrade, int>(),
                PrimaryUpgradeOptions = new Dictionary<int, PrimaryUpgrade[]>(),
                SecondaryUpgradeOptions = new Dictionary<int, SecondaryUpgrade[]>(),
                SexualExpByRace = new Dictionary<Race, int>(),
            };

            CombatOrder = new List<CleanString> { _ethelStats.Key, _nemaStats.Key };

            IsNemaClearingMist = true;
        }

        public static void StartNewGame(string name)
        {
            Option<SaveFilesManager> savesManager = SaveFilesManager.Instance;
            if (savesManager.IsNone)
            {
                Debug.LogError("Trying to start a new game with no save files manager");
                return;
            }
            
            Save save = new(name);
            Handler.SetValue(save);
        }
        
#if UNITY_EDITOR
        public static void StartSaveAsTesting()
        {
            Save save = new("test");
            Handler.SetValue(save);
        }
#endif

        public static void Deactivate()
        {
            Handler.SetValue(null);
        }

        [ShowInInspector]
        private readonly ListStack<SaveRecord> _recentRecords = new();
        public IList<SaveRecord> RecentRecords => _recentRecords;

        public Option<SaveRecord> GetMostRecentRecord()
        {
            SavePoint.TryGenerateFromCurrentSession();
            return _recentRecords.Peek().TrySome(out SaveRecord lastRecord) ? Option<SaveRecord>.Some(lastRecord) : Option.None;
        }

        [NotNull]
        private SaveRecord CreateRecordImmediate(SavePoint.Base savePoint)
        {
            SanitizeCombatOrder();
            ReadOnlySpan<CharacterStats> statsSpan = GetAllCharacterStats();
            CharacterStats[] clonedStats = new CharacterStats[statsSpan.Length];
            for (int i = 0; i < statsSpan.Length; i++)
                clonedStats[i] = statsSpan[i].DeepClone();

            SaveRecord record = new(Name, DateTime.Now, new Dictionary<CleanString, bool>(Booleans), new Dictionary<CleanString, int>(Ints), new Dictionary<CleanString, CleanString>(Strings),
                                    Location, LocationsUnlocked.ToArray(), clonedStats, NemaExhaustion, IsNemaClearingMist, GeneralRandomizer.DeepClone(), CombatOrder.ToArray(), savePoint);

            if (VALIDATE)
            {
                StringBuilder errors = new();
                if (record.IsDataValid(errors) == false)
                {
                    Debug.LogWarning(errors.ToString());
                    return record;
                }
            }

            record.IsDirty = true;
            _recentRecords.Push(record);
            TrimRecords();
            return record;
        }

        public void ForceRecordManually(SavePoint.Base savePoint) => CreateRecordImmediate(savePoint);

        private void TrimRecords()
        {
            int removeCount = _recentRecords.Count - 30;
            for (int i = 0; i < removeCount; i++)
                _recentRecords.RemoveAt(0);
        }

        public static Result<Save> FromRecord([NotNull] SaveRecord record)
        {
            if (VALIDATE)
            {
                StringBuilder errors = new();
                if (record.IsDataValid(errors) == false)
                    return Result<Save>.Error(errors.ToString());
            }
            
            Option<CharacterStats> ethel = Option.None;
            Option<CharacterStats> nema = Option.None;
            foreach (CharacterStats stats in record.Characters)
            {
                if (stats.Key == Ethel.GlobalKey)
                    ethel = Option<CharacterStats>.Some(stats);
                else if (stats.Key == Nema.GlobalKey)
                    nema = Option<CharacterStats>.Some(stats);
            }
            
            if (ethel.IsNone || nema.IsNone)
                return Result<Save>.Error($"Could not find both Ethel and Nema in the save record, {(ethel.IsSome ? "missing Nema" : nema.IsSome ? "missing Ethel" : "missing Both" )}");

            return Result<Save>.Ok(new Save
            {
                Name = record.Name,
                Date = record.Date,
                Booleans = new Dictionary<CleanString, bool>(record.Booleans),
                Ints = new Dictionary<CleanString, int>(record.Ints),
                Strings = new Dictionary<CleanString, CleanString>(record.Strings),
                Location = record.Location,
                _locationsUnlocked = new List<LocationEnum>(record.LocationsUnlocked),
                _ethelStats = ethel.Value.DeepClone(),
                _nemaStats = nema.Value.DeepClone(),
                NemaExhaustion = record.NemaExhaustion,
                IsNemaClearingMist = record.IsNemaClearingMist,
                GeneralRandomizer = record.GeneralRandomizer.DeepClone(),
                CombatOrder = record.CombatOrder.ToList(),
            });
        }
    }
}