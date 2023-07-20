using System.Diagnostics.Contracts;
using Core.Combat.Scripts.Effects.Interfaces;
using Core.Combat.Scripts.Effects.Types.BuffOrDebuff;
using Core.Utils.Math;

namespace Core.Combat.Scripts.Behaviour.Modules
{
    public abstract record StunRecord : ModuleRecord
    {
        public abstract IStunModule Deserialize(CharacterStateMachine owner);
    }

    public interface IStunModule : IModule
    {
        protected static TSpan Zero => TSpan.Zero;

        private const double MitigationSecondsMultiplier = 0.65;
        
        public static TSpan ClampInitialDuration(TSpan initialDuration) => initialDuration.Clamp(Zero, initialDuration);
        public static TSpan ClampRemaining(TSpan remaining, TSpan initialDuration) => remaining.Clamp(Zero, initialDuration);
        
        protected CharacterStateMachine Owner { get; }

        [Pure]
        public static (TSpan duration, int bonusMitigation) CalculateDuration(int power, int mitigation)
        {
            double powerD = power;
            double mitigationD = mitigation;

            double dividend = powerD + (powerD * powerD / 500) - mitigationD - (mitigationD * mitigationD / 500);
            double divisor = 125 + (powerD * 0.25) + (mitigationD * 0.25) + (powerD * mitigationD * 0.0005);
            
            TSpan duration = TSpan.FromSeconds(dividend / divisor);
            
            if (duration.Ticks < 1)
                duration = Zero;
            
            int bonusMitigation = (int)(duration.Seconds * MitigationSecondsMultiplier);
            
            return (duration, bonusMitigation);
        }

        protected TSpan InitialDuration { get; }
        public sealed TSpan GetInitialDuration() => ClampInitialDuration(InitialDuration);
        
        private static readonly TSpan StunRedundancyDuration = TSpan.FromSeconds(4);
        
        public void AddFromPower(int power)
        {
            int mitigation = GetStunMitigation();
            (TSpan duration, int bonusMitigation) = CalculateDuration(power, mitigation);
            SetInitialIgnoringMitigation(duration);
            
            if (Owner.StatusReceiverModule.GetAll.FindType<StunRedundancyBuff>().TrySome(out StunRedundancyBuff stunRedundancyBuff))
                stunRedundancyBuff.AddRedundancy(bonusMitigation);
            else
                StunRedundancyBuff.CreateFromAppliedStun(StunRedundancyDuration, isPermanent: false, Owner, bonusMitigation);
        }
        
        protected void SetInitialIgnoringMitigationInternal(TSpan clampedInitialDuration);
        public sealed void SetInitialIgnoringMitigation(TSpan initialDuration)
            => SetInitialIgnoringMitigationInternal(ClampInitialDuration(initialDuration));

        protected TSpan Remaining { get; }
        public sealed TSpan GetRemaining() => ClampRemaining(Remaining, GetInitialDuration());
        
        TSpan GetEstimatedRemaining();

        protected void SetBothInternal(TSpan clampedInitialDuration, TSpan clampedRemaining);
        public sealed void SetBoth(TSpan initialDuration, TSpan remaining)
        {
            initialDuration = ClampInitialDuration(initialDuration);
            remaining = ClampRemaining(remaining, initialDuration);
            SetBothInternal(initialDuration, remaining);
        }

#region Mitigation
        public const int MinStunMitigation = -100;
        public const int MaxStunMitigation = 300;
        
        public static int ClampStunMitigation(int stunMitigation) => stunMitigation.Clamp(MinStunMitigation, MaxStunMitigation);
        
        int BaseStunMitigation { get; set; }
        void SubscribeStunMitigation(IBaseAttributeModifier modifier, bool allowDuplicates);
        void UnsubscribeStunMitigation(IBaseAttributeModifier modifier);
        protected int GetStunMitigationInternal();
        public sealed int GetStunMitigation() => ClampStunMitigation(GetStunMitigationInternal());
#endregion

        bool Tick(ref TSpan timeStep);
        void Reset();
        
        StunRecord GetRecord();
    }
}