using Core.Utils.Math;

namespace Core.Combat.Scripts.WinningCondition
{
    public interface IWinningCondition
    {
        CombatStatus Evaluate();
        WinningConditionRecord Serialize();
        string DisplayName { get; }
        TSpan GetTimeToDisplay();
    }
}