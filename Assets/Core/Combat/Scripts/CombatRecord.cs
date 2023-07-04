using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts.BackgroundGeneration;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Managers;
using Core.Combat.Scripts.WinningCondition;
using UnityEngine;
using Utils.Extensions;

namespace Core.Combat.Scripts
{
    public record CombatRecord(CharacterRecord[] Characters, BackgroundRecord Background, WinningConditionRecord WinningCondition, float ElapsedTime, float AccumulatedExhaustionTime, CombatSetupInfo.Record SetupInfo)
    {
        private static readonly List<CharacterRecord> ReusableList = new();

        public static CombatRecord FromCombat(CombatManager combatManager)
        {
            ReusableList.Clear();
            foreach (CharacterStateMachine character in combatManager.Characters.GetAllFixed())
            {
                if (character.StateEvaluator.PureEvaluate() is CharacterState.Defeated)
                    continue;
                
                ReusableList.Add(CharacterRecord.FromState(character));
            }
            
            BackgroundRecord backgroundData;
            if (combatManager.Background.IsSome)
                backgroundData = combatManager.Background.Value.GetRecord();
            else
            {
                Debug.LogWarning("No background generator found while trying to save combat state, cannot save.");
                backgroundData = null;
            }

            WinningConditionRecord winningCondition = combatManager.WinningCondition.Serialize();
            float elapsedTime = combatManager.ElapsedTime;
            return new CombatRecord(ReusableList.ToArray(), backgroundData, winningCondition, elapsedTime, combatManager.AccumulatedExhaustionTime, combatManager.CombatSetupInfo.GetRecord());
        }

        public bool IsDataValid(StringBuilder errors)
        {
            if (Characters == null)
            {
                errors.AppendLine("Invalid ", nameof(CombatRecord), ". Characters array is null");
                return false;
            }

            for (int index = 0; index < Characters.Length; index++)
            {
                CharacterRecord record = Characters[index];
                if (record == null)
                {
                    errors.AppendLine("Invalid ", nameof(CombatRecord), ". Character at index ", index.ToString(), " is null");
                    return false;
                }
            }

            for (int index = 0; index < Characters.Length; index++)
                if (Characters[index].IsDataValid(errors, Characters) == false)
                    return false;

            if (Background == null)
            {
                errors.AppendLine("Invalid ", nameof(CombatRecord), ". Background is null");
                return false;
            }

            if (Background.IsDataValid(errors) == false)
                return false;

            if (WinningCondition == null)
            {
                errors.AppendLine("Invalid ", nameof(CombatRecord), ". Winning condition is null");
                return false;
            }

            if (WinningCondition.IsDataValid(errors) == false)
                return false;

            if (SetupInfo == null)
            {
                errors.AppendLine("Invalid ", nameof(CombatRecord), ". Setup info is null");
                return false;
            }
            
            if (SetupInfo.IsDataValid(errors) == false)
                return false;
            
            return true;
        }
    }
}