using Utils.Math;

namespace Core.Combat.Scripts.Interfaces.Modules
{
    public interface IStunModule : IModule
    {
        public const float StunRecoveryStepTime = 0.5f;
        public const float StunResistPerStep = 0.25f;
        
        public static float ClampInitialDuration(float initialDuration) => initialDuration.Clamp(0f, initialDuration);
        public static float ClampRemaining(float remaining, float initialDuration) => remaining.Clamp(0f, initialDuration);

        protected float InitialDuration { get; }
        public sealed float GetInitialDuration() => ClampInitialDuration(InitialDuration);

        protected void SetInitialInternal(float initialDuration);
        public sealed void SetInitial(float initialDuration) => SetInitialInternal(ClampInitialDuration(initialDuration));

        protected float Remaining { get; }
        public sealed float GetRemaining() => ClampRemaining(Remaining, GetInitialDuration());
        float GetEstimatedRealRemaining();

        protected void SetBothInternal(float initialDuration, float remaining);

        public sealed void SetBoth(float initialDuration, float remaining)
        {
            initialDuration = ClampInitialDuration(initialDuration);
            remaining = ClampRemaining(remaining, initialDuration);
            SetBothInternal(initialDuration, remaining);
        }

        public static float ClampTimeSinceStart(float time) => time.Clamp(0f, time);

        protected float TimeSinceStunStarted { get; }
        public sealed float GetTimeSinceStunStarted() => ClampTimeSinceStart(TimeSinceStunStarted);

        uint StunRecoverySteps { get; }

        bool Tick(ref float timeStep);
        void Reset();
    }
}