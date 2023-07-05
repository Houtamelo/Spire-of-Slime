using Core.Combat.Scripts.Effects.Interfaces;
using Core.Utils.Math;

namespace Core.Combat.Scripts.Interfaces.Modules
{
    public interface IStatsModule : IModule
    {
    #region Speed
        public const float MinSpeed = 0.2f;
        public const float MaxSpeed = 3f;
        public static float ClampSpeed(float speed) => speed.Clamp(MinSpeed, MaxSpeed);
        
        float BaseSpeed { get; set; }
        void SubscribeSpeed(IBaseFloatAttributeModifier modifier, bool allowDuplicates);
        void UnsubscribeSpeed(IBaseFloatAttributeModifier modifier);
        protected float GetSpeedInternal();
        public sealed float GetSpeed() => ClampSpeed(GetSpeedInternal());
    #endregion

    #region Damage
        public static (float lower, float upper) ClampFloatDamage(float lower, float upper)
        {
            upper = upper.Clamp(lower, upper);
            lower = lower.Clamp(0f, upper);
            return (lower, upper);
        }

        public static (uint lower, uint upper) ClampRoundedDamage(uint lower, uint upper)
        {
            upper = upper.Clamp(lower, upper);
            lower = lower.Clamp(0, upper);
            return (lower, upper);
        }

        uint BaseDamageLower { get; set; }
        uint BaseDamageUpper { get; set; }

        public sealed (float lower, float upper) GetDamageWithMultiplier()
        {
            (float lower, float upper) = (BaseDamageLower, BaseDamageUpper);
            float damageMultiplier = GetPower();
            
            upper *= damageMultiplier;
            lower *= damageMultiplier;
            
            return ClampFloatDamage(lower, upper);
        }

        public sealed (uint lower, uint upper) GetDamageWithMultiplierRounded()
        {
            (float lower, float upper) = (BaseDamageLower, BaseDamageUpper);
            float damageMultiplier = GetPower();
            
            upper *= damageMultiplier;
            lower *= damageMultiplier;

            return ClampRoundedDamage(lower.CeilToUInt(), upper.CeilToUInt());
        }
        
        public const float MinPower = 0f;
        public const float MaxPower = 5f;
        public static float ClampPower(float power) => power.Clamp(MinPower, MaxPower);

        float BasePower { get; set; }
        void SubscribePower(IBaseFloatAttributeModifier modifier, bool allowDuplicates);
        void UnsubscribePower(IBaseFloatAttributeModifier modifier);
        protected float GetPowerInternal();
        public sealed float GetPower() => ClampPower(GetPowerInternal());
    #endregion

    #region Accuracy
        public const float MinAccuracy = -3f;
        public const float MaxAccuracy = 3f;
        public static float ClampAccuracy(float accuracy) => accuracy.Clamp(MinAccuracy, MaxAccuracy);

        float BaseAccuracy { get; set; }
        void SubscribeAccuracy(IBaseFloatAttributeModifier modifier, bool allowDuplicates);
        void UnsubscribeAccuracy(IBaseFloatAttributeModifier modifier);
        protected float GetAccuracyInternal();
        public sealed float GetAccuracy() => ClampAccuracy(GetAccuracyInternal());
    #endregion

    #region Critical Chance
        public const float MinCriticalChance = -3f;
        public const float MaxCriticalChance = 3f;
        public static float ClampCriticalChance(float criticalChance) => criticalChance.Clamp(MinCriticalChance, MaxCriticalChance);

        float BaseCriticalChance { get; set; }
        void SubscribeCriticalChance(IBaseFloatAttributeModifier modifier, bool allowDuplicates);
        void UnsubscribeCriticalChance(IBaseFloatAttributeModifier modifier);
        protected float GetCriticalChanceInternal();
        public sealed float GetCriticalChance() => ClampCriticalChance(GetCriticalChanceInternal());
    #endregion

    #region Dodge
        public const float MinDodge = -3f;
        public const float MaxDodge = 3f;
        public static float ClampDodge(float dodge) => dodge.Clamp(MinDodge, MaxDodge);
        
        float BaseDodge { get; set; }
        void SubscribeDodge(IBaseFloatAttributeModifier modifier, bool allowDuplicates);
        void UnsubscribeDodge(IBaseFloatAttributeModifier modifier);
        protected float GetDodgeInternal();
        public sealed float GetDodge() => ClampDodge(GetDodgeInternal());
    #endregion
    }
}