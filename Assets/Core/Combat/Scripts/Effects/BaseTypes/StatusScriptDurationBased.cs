using Core.Utils.Math;

namespace Core.Combat.Scripts.Effects.BaseTypes
{
    public abstract record StatusScriptDurationBased(bool Permanent, TSpan BaseDuration) : StatusScript
    {
        public const double DurationMultiplierOnCrit = 1.5;

        public bool Permanent { get; protected set; } = Permanent;
        public TSpan BaseDuration { get; protected set; } = BaseDuration;
        
        public string GetDurationString => Permanent ? StatusUtils.GetPermanentDurationString() : StatusUtils.GetDurationString(BaseDuration);
    }
}