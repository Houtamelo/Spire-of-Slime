using Core.Combat.Scripts.Effects.Interfaces;
using Core.Utils.Math;
using UnityEngine;

namespace Core.Combat.Scripts.Behaviour.Modules
{
    public abstract record StatsRecord : ModuleRecord
    {
        public abstract IStatsModule Deserialize(CharacterStateMachine owner);
    }
    
    public interface IStatsModule : IModule
    {
    #region Speed
        public const int MinSpeed = 20;
        public const int MaxSpeed = 300;
        public static int ClampSpeed(int speed) => speed.Clamp(MinSpeed, MaxSpeed);
        
        int BaseSpeed { get; set; }
        void SubscribeSpeed(IBaseAttributeModifier modifier, bool allowDuplicates);
        void UnsubscribeSpeed(IBaseAttributeModifier modifier);
        protected int GetSpeedInternal();
        public sealed int GetSpeed() => ClampSpeed(GetSpeedInternal());
    #endregion

    #region Damage
        public static (int lower, int upper) ClampRawDamage(int lower, int upper)
        {
            upper = Mathf.Max(0, upper);
            lower = lower.Clamp(0, upper);
            return (lower, upper);
        }

        int BaseDamageLower { get; set; }
        int BaseDamageUpper { get; set; }
        
        public (int lower, int upper) GetBaseDamageRaw() => ClampRawDamage(BaseDamageLower, BaseDamageUpper);
        
        public const int MinPower = 0;
        public const int MaxPower = 500;
        public static int ClampPower(int power) => power.Clamp(MinPower, MaxPower);

        int BasePower { get; set; }
        void SubscribePower(IBaseAttributeModifier modifier, bool allowDuplicates);
        void UnsubscribePower(IBaseAttributeModifier modifier);
        protected int GetPowerInternal();
        public sealed int GetPower() => ClampPower(GetPowerInternal());
    #endregion

    #region Accuracy
        public const int MinAccuracy = -300;
        public const int MaxAccuracy = 300;
        public static int ClampAccuracy(int accuracy) => accuracy.Clamp(MinAccuracy, MaxAccuracy);

        int BaseAccuracy { get; set; }
        void SubscribeAccuracy(IBaseAttributeModifier modifier, bool allowDuplicates);
        void UnsubscribeAccuracy(IBaseAttributeModifier modifier);
        protected int GetAccuracyInternal();
        public sealed int GetAccuracy() => ClampAccuracy(GetAccuracyInternal());
    #endregion

    #region Critical Chance
        public const int MinCriticalChance = -300;
        public const int MaxCriticalChance = 300;
        public static int ClampCriticalChance(int criticalChance) => criticalChance.Clamp(MinCriticalChance, MaxCriticalChance);

        int BaseCriticalChance { get; set; }
        void SubscribeCriticalChance(IBaseAttributeModifier modifier, bool allowDuplicates);
        void UnsubscribeCriticalChance(IBaseAttributeModifier modifier);
        protected int GetCriticalChanceInternal();
        public sealed int GetCriticalChance() => ClampCriticalChance(GetCriticalChanceInternal());
    #endregion

    #region Dodge
        public const int MinDodge = -300;
        public const int MaxDodge = 300;
        public static int ClampDodge(int dodge) => dodge.Clamp(MinDodge, MaxDodge);
        
        int BaseDodge { get; set; }
        void SubscribeDodge(IBaseAttributeModifier modifier, bool allowDuplicates);
        void UnsubscribeDodge(IBaseAttributeModifier modifier);
        protected int GetDodgeInternal();
        public sealed int GetDodge() => ClampDodge(GetDodgeInternal());
    #endregion

        StatsRecord GetRecord();
    }
}