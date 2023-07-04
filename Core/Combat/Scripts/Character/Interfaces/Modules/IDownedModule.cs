using Utils.Math;

namespace Core.Combat.Scripts.Interfaces.Modules
{
    public interface IDownedModule : IModule
    {
        public const float DefaultDownedDurationOnZeroStamina = 8f;
        public const float DefaultDownedDurationOnGrappleRelease = 2.5f;

        public static float ClampInitialDuration(float initialDuration) => initialDuration.Clamp(0f, initialDuration);
        public static float ClampRemaining(float remaining, float initialDuration) => remaining.Clamp(0f, initialDuration);
        
        protected float InitialDuration { get; }
        public float GetInitialDuration() => ClampInitialDuration(InitialDuration);
        
        protected float Remaining { get; }
        public float GetRemaining() => ClampRemaining(Remaining, GetInitialDuration());

        float GetEstimatedRealRemaining();

        protected void SetInitialInternal(float initialDuration);
        public sealed void SetInitial(float initialDuration)
        {
            initialDuration = ClampInitialDuration(initialDuration);
            SetInitialInternal(initialDuration);
        }
        
        protected void SetBothInternal(float initialDuration, float remaining);
        public sealed void SetBoth(float initialDuration, float remaining)
        {
            initialDuration = ClampInitialDuration(initialDuration);
            remaining = ClampRemaining(remaining, initialDuration);
            SetBothInternal(initialDuration, remaining);
        }
        
        bool Tick(ref float timeStep);
        void Reset();
        
        bool HandleZeroStamina();
        bool CanHandleNextZeroStamina();
    }
}