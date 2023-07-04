/*using Core.Combat.Scripts.Managers;
using UnityEngine;
using Save = Save_Management.Save;

namespace Core.Combat.Scripts.AI.Training
{
    public class CombatTester : MonoBehaviour
    {
        [SerializeField] 
        private ScriptableCombatSetupInfo combatSetupInfo;
        
        [ContextMenu("Start Test")]
        private void StartTest()
        {
            Save.StartNewGame("test");
            CombatManager.Instance.SomeOrDefault().SetupCombatFromBeginning(combatSetupInfo.ToStruct(), new CombatTracker(new CombatTracker.ReturnToLocalMap()), 
                                                                            combatSetupInfo.WinningConditionGenerator, combatSetupInfo.BackgroundPrefab.Key);
        }
    }
}*/