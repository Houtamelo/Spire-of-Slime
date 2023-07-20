using Core.Utils.Math;

namespace Core.Combat.Scripts.Behaviour.Modules
{
    public abstract record RecoveryRecord : ModuleRecord
    {
        public abstract IRecoveryModule Deserialize(CharacterStateMachine owner);
    }
    
    public interface IRecoveryModule : IModule
    {
        protected static TSpan Zero => TSpan.Zero;
        
        public static TSpan ClampInitialDuration(TSpan initialDuration) => initialDuration.Clamp(Zero, initialDuration);
        public static TSpan ClampRemaining(TSpan remaining, TSpan initialDuration) => remaining.Clamp(Zero, initialDuration);
        
        protected TSpan InitialDuration { get; }
        public sealed TSpan GetInitialDuration() => ClampInitialDuration(InitialDuration);
        
        protected TSpan Remaining { get; }
        public sealed TSpan GetRemaining() => ClampRemaining(Remaining, InitialDuration);
        
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
        
        RecoveryRecord GetRecord();
    }
}