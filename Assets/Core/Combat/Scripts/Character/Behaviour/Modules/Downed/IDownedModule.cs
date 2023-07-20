using Core.Utils.Math;

namespace Core.Combat.Scripts.Behaviour.Modules
{
    public abstract record DownedRecord : ModuleRecord
    {
        public abstract IDownedModule Deserialize(CharacterStateMachine owner);
    }
    
    public interface IDownedModule : IModule
    {
        protected static TSpan Zero => TSpan.Zero;
        
        public static readonly TSpan DefaultDownedDurationOnZeroStamina = TSpan.FromSeconds(8.0);
        public static readonly TSpan DefaultDownedDurationOnGrappleRelease = TSpan.FromSeconds(2.5);

        public static TSpan ClampInitialDuration(TSpan initialDuration) => initialDuration.Clamp(Zero, initialDuration);
        public static TSpan ClampRemaining(TSpan remaining, TSpan initialDuration) => remaining.Clamp(Zero, initialDuration);
        
        protected TSpan InitialDuration { get; }
        public TSpan GetInitialDuration() => ClampInitialDuration(InitialDuration);
        
        protected TSpan Remaining { get; }
        public TSpan GetRemaining() => ClampRemaining(Remaining, GetInitialDuration());
        
        TSpan GetEstimatedRemaining();

        protected void SetInitialInternal(TSpan clampedInitialDuration);
        public sealed void SetInitial(TSpan initialDuration)
        {
            initialDuration = ClampInitialDuration(initialDuration);
            SetInitialInternal(initialDuration);
        }
        
        protected void SetBothInternal(TSpan clampedInitialDuration, TSpan clampedRemaining);
        public sealed void SetBoth(TSpan initialDuration, TSpan remaining)
        {
            initialDuration = ClampInitialDuration(initialDuration);
            remaining = ClampRemaining(remaining, initialDuration);
            SetBothInternal(initialDuration, remaining);
        }
        
        bool Tick(ref TSpan timeStep);
        void Reset();
        
        bool HandleZeroStamina();
        bool CanHandleNextZeroStamina();

        DownedRecord GetRecord();
    }
}