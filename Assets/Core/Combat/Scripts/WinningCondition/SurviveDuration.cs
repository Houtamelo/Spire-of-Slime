using System.Text;
using Core.Combat.Scripts.Managers;
using Utils.Extensions;

namespace Core.Combat.Scripts.WinningCondition
{
    public record SurviveDurationRecord(float Duration) : WinningConditionRecord(ConditionType.SurviveDuration)
    {
        public override IWinningCondition Deserialize(CombatManager combatManager) => new SurviveDuration(combatManager, Duration);

        public override bool IsDataValid(StringBuilder errors)
        {
            if (Duration <= 0f)
            {
                errors.AppendLine("Invalid ", nameof(SurviveDurationRecord), ". Duration must be greater than 0.");
                return false;
            }
            
            return true;
        }
    }

    public sealed class SurviveDuration : ISurviveDuration
    {
        private readonly CombatManager _combatManager;
        private readonly float _duration;

        public SurviveDuration(CombatManager combatManager, float duration)
        {
            _combatManager = combatManager;
            _duration = duration;
        }

        public CombatStatus Tick() => this.DefaultTick(_combatManager, _duration);

        public WinningConditionRecord Serialize() => new SurviveDurationRecord(_duration);
        public string DisplayName => this.DefaultDisplayName(_duration);
        public float GetTimeToDisplay() => this.DefaultTimeToDisplay(_combatManager, _duration);
    }
}