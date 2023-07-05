using System;
using System.Collections.Generic;
using System.Linq;
using Core.Combat.Scripts;
using Core.Combat.Scripts.BackgroundGeneration;
using Core.Combat.Scripts.Interfaces;
using Core.Combat.Scripts.WinningCondition;
using Core.Save_Management.SaveObjects;
using Core.Utils.Patterns;
using Core.World_Map.Scripts;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils.Patterns;
using Save = Core.Save_Management.SaveObjects.Save;

namespace Core.Main_Database.Combat
{
    public sealed class CombatScriptDatabase : ScriptableObject
    {
        private static DatabaseManager Instance => DatabaseManager.Instance;
        
        [SerializeField, Required]
        private ScriptableCombatSetupInfo[] allCombats;
        
        private readonly Dictionary<CleanString, ScriptableCombatSetupInfo> _mappedCombats = new();

        public static Option<ScriptableCombatSetupInfo> GetCombat(CleanString key) => GetCombat(Instance.CombatScriptsDatabase, key);
        public static Option<ScriptableCombatSetupInfo> GetCombat(CombatScriptDatabase database, CleanString key)
        {
            return database._mappedCombats.TryGetValue(key, out ScriptableCombatSetupInfo combatSetupInfo)
                       ? Option<ScriptableCombatSetupInfo>.Some(combatSetupInfo)
                       : Option<ScriptableCombatSetupInfo>.None;
        }

        [MustUseReturnValue]
        public static (CombatSetupInfo setupInfo, WinningConditionGenerator winningConditionGenerator, CleanString backgroundKey) GenerateTemporaryDefaultCombatForLocation(BothWays location, float multiplier)
        {
            Option<CombatBackground> backgroundPrefab = BackgroundDatabase.GetBackgroundPrefab(location);
            if (backgroundPrefab.IsNone)
            {
                Debug.LogWarning($"No background prefab found for location: {location}, using empty (black) background.");
                GameObject obj = new();
                CombatBackground background = obj.AddComponent<CombatBackground>();
                background.SetLocation = location;
                backgroundPrefab = Option<CombatBackground>.Some(background);
            }

            Option<CharacterScriptable[]> enemyTeam = MonsterTeamDatabase.GetEnemyTeam(location, multiplier);
            CharacterScriptable[] enemyList;
            if (enemyTeam.IsSome)
            {
                enemyList = enemyTeam.Value;
            }
            else
            {
                Debug.LogWarning($"No enemy team found for location: {location}, no enemies will be spawned.");
                enemyList = Array.Empty<CharacterScriptable>();
            }
            
            (ICharacterScript, CombatSetupInfo.RecoveryInfo)[] monsterTeam = enemyList.Select(enemyScript => ((ICharacterScript)enemyScript, CombatSetupInfo.RecoveryInfo.Default)).ToArray();
            
            Save save = Save.Current;
            (ICharacterScript, CombatSetupInfo.RecoveryInfo, float expAtStart, bool bindToSave)[] allies;
            if (save != null)
            {
                IReadOnlyList<(IReadonlyCharacterStats stats, bool bindToSave)> alliesOrder = save.GetCombatOrderAsStats();
                allies = alliesOrder.Select(element => (element.stats.GetScript(), CombatSetupInfo.RecoveryInfo.Default, element.stats.Experience, element.bindToSave)).ToArray();
            }
            else
            {
                allies = new (ICharacterScript, CombatSetupInfo.RecoveryInfo, float expAtStart, bool bindToSave)[]
                {
                    (CharacterDatabase.DefaultEthel, CombatSetupInfo.RecoveryInfo.Default, expAtStart: 0f, bindToSave: false),
                    (CharacterDatabase.DefaultNema, CombatSetupInfo.RecoveryInfo.Default, expAtStart: 0f, bindToSave: false)
                };
            }

            return (new CombatSetupInfo(allies, monsterTeam, mistExists: true, allowLust: true, GeneralPaddingSettings.Default),
                    WinningConditionGenerator.Default, backgroundPrefab.Value.Key);
        }

        public void Initialize()
        {
            foreach (ScriptableCombatSetupInfo combat in allCombats)
                _mappedCombats.Add(combat.Key, combat);
            
            _mappedCombats.TrimExcess();
        }

#if UNITY_EDITOR        
        public void AssignData(IEnumerable<ScriptableCombatSetupInfo> combatEvents)
        {
            allCombats = combatEvents.ToArray();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        public bool Exists(CleanString key) => allCombats.Any(c => c.Key == key);
#endif
    }
}