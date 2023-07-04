using System.Text;
using Core.Combat.Scripts.Behaviour;
using UnityEngine;
using CombatManager = Core.Combat.Scripts.Managers.CombatManager;

namespace Core.Combat.Scripts.WinningCondition
{
    public record DefeatAllRecord() : WinningConditionRecord(ConditionType.DefeatAll)
    {
        public override IWinningCondition Deserialize(CombatManager combatManager) => new DefeatAll(combatManager);
        public override bool IsDataValid(StringBuilder errors) => true;
    }
    
    public class DefeatAll : IWinningCondition
    {
        private CombatManager _combatManager;
        public DefeatAll(CombatManager combatManager) => _combatManager = combatManager;

        public CombatStatus Tick()
        {
            if (ReferenceEquals(_combatManager, null))
            {
                Debug.LogWarning("Trying to evaluate winning condition but combat manager is not assigned, trying to find it... Will log more if it fails.");
                if (CombatManager.AssertInstance(out _combatManager) == false)
                {
                    Debug.LogError("Failed to find combat manager, returning left side won.");
                    return CombatStatus.LeftSideWon;
                }
            }

            bool anyAliveOnLeftSide = false;
            bool anyAliveOnRightSide = false;
            foreach (CharacterStateMachine character in _combatManager.Characters.GetAllFixed())
            {
                if (character.StateEvaluator.FullPureEvaluate() is { Corpse: true } or { Defeated: true })
                    continue;
                
                if (character.PositionHandler.IsLeftSide)
                    anyAliveOnLeftSide = true;
                else
                    anyAliveOnRightSide = true;
            }

            return (anyAliveOnLeftSide, anyAliveOnRightSide) switch
            {
                (true, true)  => CombatStatus.InProgress,
                (true, false) => CombatStatus.LeftSideWon,
                (false, true) => CombatStatus.RightSideWon,
                _             => CombatStatus.RightSideWon
            };
        }

        public WinningConditionRecord Serialize() => new DefeatAllRecord();
        public string DisplayName => "Defeat all enemies";
        public float GetTimeToDisplay() => _combatManager.ElapsedTime;
    }
}