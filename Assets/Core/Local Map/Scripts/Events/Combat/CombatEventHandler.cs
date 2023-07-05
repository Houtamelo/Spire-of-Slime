using System.Collections;
using Core.Combat.Scripts;
using Core.Combat.Scripts.WinningCondition;
using Core.Game_Manager.Scripts;
using Core.Local_Map.Scripts.Enums;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Async;
using Core.Utils.Patterns;
using Core.World_Map.Scripts;
using UnityEngine;
using Utils.Patterns;

namespace Core.Local_Map.Scripts.Events.Combat
{
    public static class CombatEventHandler
    {
        public static CoroutineWrapper HandleCombat(TileInfo tileInfo, in Option<float> multiplier, in Option<(CombatSetupInfo setupInfo, WinningConditionGenerator winningConditionGenerator, string backgroundKey)> combatInfo) 
            => new(CombatRoutine(tileInfo, multiplier, combatInfo), nameof(CombatRoutine), context: null, autoStart: true);

        private static IEnumerator CombatRoutine(TileInfo tileInfo, Option<float> multiplierOption, Option<(CombatSetupInfo setupInfo, WinningConditionGenerator winningConditionGenerator, string backgroundKey)> combatInfoOption)
        {
            float multiplier;
            if (multiplierOption.IsNone)
            {
                Debug.LogWarning("Multiplier was not provided for combat event. Defaulting to 1");
                multiplier = 1;
            }
            else
                multiplier = multiplierOption.Value;

            if (tileInfo.Type == TileType.WorldLocation)
            {
                Debug.LogError("Combat event was called on a tile that is a world location", tileInfo);
                yield break;
            }

            Result<BothWays> locationResult = tileInfo.GetBothWaysPath();
            if (locationResult.IsErr)
            {
                Debug.LogWarning(locationResult.Reason);
                yield break;
            }

            (CombatSetupInfo setupInfo, WinningConditionGenerator winningConditionGenerator, CleanString backgroundKey) = combatInfoOption.IsSome ? combatInfoOption.Value :
                                                                                                                              CombatScriptDatabase.GenerateTemporaryDefaultCombatForLocation(locationResult.Value, multiplier);
            CombatTracker tracker = new(OnFinish: new CombatTracker.StandardLocalMap());
            
            if (GameManager.AssertInstance(out GameManager gameManager))
                gameManager.LocalMapToCombat(setupInfo, tracker, winningConditionGenerator, backgroundKey);
            else
                yield break;

            while (!tracker.IsDone)
                yield return null;
        }
    }
}