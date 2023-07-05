using Core.Combat.Scripts.Effects.Interfaces;
using Core.Utils.Math;

namespace Core.Combat.Scripts.Interfaces.Modules
{
    public interface IResistancesModule : IModule
    {
        public const float MinResistance = -3f;
        public const float MaxResistance = 3f;
        
        public static float ClampResistance(float resistance) => resistance.Clamp(MinResistance, MaxResistance);
        
        float BaseDebuffResistance { get; set; }
        void SubscribeDebuffResistance(IBaseFloatAttributeModifier modifier, bool allowDuplicates);
        void UnsubscribeDebuffResistance(IBaseFloatAttributeModifier modifier);
        protected float GetDebuffResistanceInternal();
        public sealed float GetDebuffResistance() => ClampResistance(GetDebuffResistanceInternal());

        float BaseMoveResistance { get; set; }
        void SubscribeMoveResistance(IBaseFloatAttributeModifier modifier, bool allowDuplicates);
        void UnsubscribeMoveResistance(IBaseFloatAttributeModifier modifier);
        protected float GetMoveResistanceInternal();
        public sealed float GetMoveResistance() => ClampResistance(GetMoveResistanceInternal());

        float BasePoisonResistance { get; set; }
        void SubscribePoisonResistance(IBaseFloatAttributeModifier modifier, bool allowDuplicates);
        void UnsubscribePoisonResistance(IBaseFloatAttributeModifier modifier); 
        protected float GetPoisonResistanceInternal();
        public sealed float GetPoisonResistance() => ClampResistance(GetPoisonResistanceInternal());

        public const float MinStunRecoverySpeed = 0.2f;
        public const float MaxStunRecoverySpeed = 3f;
        public static float ClampStunRecoverySpeed(float stunRecoverySpeed) => stunRecoverySpeed.Clamp(MinStunRecoverySpeed, MaxStunRecoverySpeed);
        
        float BaseStunRecoverySpeed { get; set; }
        void SubscribeStunRecoverySpeed(IBaseFloatAttributeModifier modifier, bool allowDuplicates);
        void UnsubscribeStunRecoverySpeed(IBaseFloatAttributeModifier modifier);
        protected float GetStunRecoverySpeedInternal();
        public sealed float GetStunRecoverySpeed() => ClampStunRecoverySpeed(GetStunRecoverySpeedInternal());
    }
}