namespace Core.Combat.Scripts.Effects.BaseTypes
{
    public abstract record StatusScriptDurationBased(bool Permanent = false, float BaseDuration = 2) : StatusScript
    {
        public const float DurationMultiplierOnCrit = 1.5f;

        public bool Permanent { get; protected set; } = Permanent;
        public float BaseDuration { get; protected set; } = BaseDuration;
        
        public string GetDurationString => Permanent ? "permanent" : $"for {BaseDuration.ToString("0.00")}s";
    }
}