using System.Text;
using Core.Combat.Scripts.Managers;
using Core.Utils.Extensions;
using Core.Utils.Math;
using JetBrains.Annotations;

namespace Core.Combat.Scripts.WinningCondition
{
    public record SurviveDurationRecord(TSpan Duration) : WinningConditionRecord(ConditionType.SurviveDuration)
    {
        [NotNull]
        public override IWinningCondition Deserialize(CombatManager combatManager) => new SurviveDuration(combatManager, Duration);

        public override bool IsDataValid(StringBuilder errors)
        {
            if (Duration.Ticks <= 0)
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
        private readonly TSpan _duration;

        public SurviveDuration(CombatManager combatManager, TSpan duration)
        {
            _combatManager = combatManager;
            _duration = duration;
        }

        public CombatStatus Evaluate() => this.DefaultTick(_combatManager, _duration);

        [NotNull]
        public WinningConditionRecord Serialize() => new SurviveDurationRecord(_duration);
        [NotNull]
        public string DisplayName => this.DefaultDisplayName(_duration);
        public TSpan GetTimeToDisplay() => this.DefaultTimeToDisplay(_combatManager, _duration);
    }
}