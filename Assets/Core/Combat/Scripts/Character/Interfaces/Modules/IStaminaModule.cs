using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.Interfaces;
using Utils.Math;
using Utils.Patterns;

namespace Core.Combat.Scripts.Interfaces.Modules
{
    public interface IStaminaModule : IModule
    {
        public static uint ClampCurrentStamina(uint currentStamina, uint maxStamina) => currentStamina.Clamp(0, maxStamina);

        uint BaseMax { get; }
        uint ActualMax { get; }

        protected void SetMaxInternal(uint maxStamina);
        public sealed void SetMax(uint maxStamina)
        {
            SetMaxInternal(maxStamina);
            SetCurrentInternal(GetCurrent());
        }

        protected uint Current { get; }
        public sealed uint GetCurrent() => ClampCurrentStamina(Current, ActualMax);
        
        protected void SetCurrentInternal(uint currentStamina);
        public sealed void SetCurrent(uint currentStamina) => SetCurrentInternal(ClampCurrentStamina(currentStamina, ActualMax));

        void ReceiveDamage(uint damage, DamageType damageType, CharacterStateMachine source);
        void DoHeal(uint heal, bool isOvertime);

        public const float MinResilience = -1f;
        public const float MaxResilience = 1f;
        public static float ClampResilience(float resilience) => resilience.Clamp(MinResilience, MaxResilience);

        float BaseResilience { get; set; }
        void SubscribeResilience(IBaseFloatAttributeModifier modifier, bool allowDuplicates);
        void UnsubscribeResilience(IBaseFloatAttributeModifier modifier);
        protected float GetResilienceInternal();
        public sealed float GetResilience() => ClampResilience(GetResilienceInternal());

        Option<CharacterStateMachine> LastDamager { get; }
    }
}