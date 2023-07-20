using Core.Combat.Scripts.Effects.Interfaces;
using Core.Utils.Math;
using Core.Utils.Patterns;

namespace Core.Combat.Scripts.Behaviour.Modules
{
    public abstract record StaminaRecord : ModuleRecord
    {
        public abstract IStaminaModule Deserialize(CharacterStateMachine owner);
    }
    
    public interface IStaminaModule : IModule
    {
        public const int MinimumMaxStamina = 1;
        public const int MaximumMaxStamina = 500;
        public static int ClampMaxStamina(int maxStamina) => maxStamina.Clamp(MinimumMaxStamina, MaximumMaxStamina); 
        public static int ClampCurrentStamina(int currentStamina, int maxStamina) => currentStamina.Clamp(0, ClampMaxStamina(maxStamina));

        int BaseMax { get; }
        int ActualMax { get; }

        protected void SetMaxInternal(int clampedMaxStamina);
        public sealed void SetMax(int maxStamina)
        {
            SetMaxInternal(ClampMaxStamina(maxStamina));
            SetCurrentInternal(GetCurrent());
        }

        protected int Current { get; }
        public sealed int GetCurrent() => ClampCurrentStamina(Current, ActualMax);
        
        protected void SetCurrentInternal(int clampedCurrentStamina);
        public sealed void SetCurrent(int currentStamina) => SetCurrentInternal(ClampCurrentStamina(currentStamina, ActualMax));

        void ReceiveDamage(int damage, DamageType damageType, CharacterStateMachine source);
        void DoHeal(int heal, bool isOvertime);

        public const int MinResilience = -100;
        public const int MaxResilience = 100;
        public static int ClampResilience(int resilience) => resilience.Clamp(MinResilience, MaxResilience);

        int BaseResilience { get; set; }
        void SubscribeResilience(IBaseAttributeModifier modifier, bool allowDuplicates);
        void UnsubscribeResilience(IBaseAttributeModifier modifier);
        protected int GetResilienceInternal();
        public sealed int GetResilience() => ClampResilience(GetResilienceInternal());

        Option<CharacterStateMachine> LastDamager { get; }

        StaminaRecord GetRecord();
    }
}