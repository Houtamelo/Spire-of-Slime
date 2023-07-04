using System.Collections.Generic;
using System.Linq;
using Core.Combat.Scripts.Interfaces;
using Core.Combat.Scripts.WinningCondition;
using Core.World_Map.Scripts;
using Main_Database.Combat;
using Main_Database.Local_Map;
using Save_Management;
using Sirenix.OdinInspector;
using UnityEngine;
using Save = Save_Management.Save;

namespace Core.Combat.Scripts.Managers
{
    public class CombatTester : MonoBehaviour
    {
        [Button("StartTest")]
        private void StartTest()
        {
            Save.StartNewGame("test");

            BothWays tileIdentifier = new(LocationEnum.BellPlantGrove, LocationEnum.Chapel);
            (ICharacterScript enemy, CombatSetupInfo.RecoveryInfo recovery)[] enemies = MonsterTeamDatabase.GetEnemyTeam(tileIdentifier, 1).Value.Select(e => ((ICharacterScript)e, CombatSetupInfo.RecoveryInfo.Default)).ToArray();
            
            Save save = Save.Current;
            CleanString backgroundKey = PathDatabase.GetPathInfo(tileIdentifier).Value.BackgroundPrefab.Key;

            IReadOnlyList<(IReadonlyCharacterStats stats, bool bindToSave)> alliesOrder = save.GetCombatOrderAsStats();
            List<(ICharacterScript, CombatSetupInfo.RecoveryInfo, float expAtStart, bool bindToSave)> allies = new(alliesOrder.Count);

            foreach ((IReadonlyCharacterStats stats, bool bindToSave) in alliesOrder)
            {
                ICharacterScript characterScript = stats.GetScript();
                allies.Add((characterScript, CombatSetupInfo.RecoveryInfo.Default, stats.Experience, bindToSave));
            }
            
            CombatSetupInfo combatSetupInfo = new(allies.ToArray(), enemies, true, allowLust: true, GeneralPaddingSettings.Default);

            CombatTracker flag = new(new CombatTracker.StandardLocalMap());
            CombatManager combatManager = FindObjectOfType<CombatManager>();
            if (combatManager != null)
                combatManager.SetupCombatFromBeginning(combatSetupInfo, flag, WinningConditionGenerator.Default, backgroundKey);
            else
            {
                Debug.LogWarning("No CombatManager found");
            }
        }
    }
}