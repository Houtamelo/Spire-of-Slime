using System.Text;
using Core.Combat.Scripts.Managers;

namespace Core.Combat.Scripts.WinningCondition
{
    public abstract record WinningConditionRecord(ConditionType ConditionType)
    {
        public abstract IWinningCondition Deserialize(CombatManager combatManager);
        public abstract bool IsDataValid(StringBuilder errors);
    }
}