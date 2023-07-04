namespace Core.Combat.Scripts.WinningCondition
{
    public interface IWinningCondition
    {
        CombatStatus Tick();
        WinningConditionRecord Serialize();
        string DisplayName { get; }
        float GetTimeToDisplay();
    }
}