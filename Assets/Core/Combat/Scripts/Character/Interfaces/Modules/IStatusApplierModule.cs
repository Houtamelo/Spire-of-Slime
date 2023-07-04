using Core.Combat.Scripts.Effects.Interfaces;
using Core.Combat.Scripts.Effects.Types.Arousal;
using Core.Combat.Scripts.Effects.Types.BuffOrDebuff;
using Core.Combat.Scripts.Effects.Types.Grappled;
using Core.Combat.Scripts.Effects.Types.Guarded;
using Core.Combat.Scripts.Effects.Types.Heal;
using Core.Combat.Scripts.Effects.Types.Lust;
using Core.Combat.Scripts.Effects.Types.Marked;
using Core.Combat.Scripts.Effects.Types.Move;
using Core.Combat.Scripts.Effects.Types.OvertimeHeal;
using Core.Combat.Scripts.Effects.Types.Poison;
using Core.Combat.Scripts.Effects.Types.Riposte;
using Core.Combat.Scripts.Effects.Types.Stun;
using Core.Combat.Scripts.Effects.Types.Summon;
using Core.Combat.Scripts.Effects.Types.Tempt;
using Utils.Collections;
using Utils.Math;

namespace Core.Combat.Scripts.Interfaces.Modules
{
    public interface IStatusApplierModule : IModule
    {
    #region Arousal
        public const float MinArousalApplyChance = -3f;
        public const float MaxArousalApplyChance = 3f;
        public static float ClampArousalApplyChance(float arousalApplyChance) => arousalApplyChance.Clamp(MinArousalApplyChance, MaxArousalApplyChance);
        
        float BaseArousalApplyChance { get; set; }
        SelfSortingList<IArousalModifier> ArousalApplyModifiers { get; }
        void SubscribeArousalApplyChance(IBaseFloatAttributeModifier modifier, bool allowDuplicates);
        void UnsubscribeArousalApplyChance(IBaseFloatAttributeModifier modifier);
        void ModifyEffectApplying(ref ArousalToApply effectStruct);
        protected float GetArousalApplyChanceInternal();
        public sealed float GetArousalApplyChance() => ClampArousalApplyChance(GetArousalApplyChanceInternal());
    #endregion

    #region Debuff
        public const float MinDebuffApplyChance = -3f;
        public const float MaxDebuffApplyChance = 3f;
        public static float ClampDebuffApplyChance(float debuffApplyChance) => debuffApplyChance.Clamp(MinDebuffApplyChance, MaxDebuffApplyChance);

        float BaseDebuffApplyChance { get; set; }
        SelfSortingList<IBuffOrDebuffModifier> BuffOrDebuffApplyModifiers { get; }
        void SubscribeDebuffApplyChance(IBaseFloatAttributeModifier modifier, bool allowDuplicates);
        void UnsubscribeDebuffApplyChance(IBaseFloatAttributeModifier modifier);
        void ModifyEffectApplying(ref BuffOrDebuffToApply effectStruct);
        protected float GetDebuffApplyChanceInternal();
        public sealed float GetDebuffApplyChance() => ClampDebuffApplyChance(GetDebuffApplyChanceInternal());
    #endregion

    #region Move
        public const float MinMoveApplyChance = -3f;
        public const float MaxMoveApplyChance = 3f;
        public static float ClampMoveApplyChance(float moveApplyChance) => moveApplyChance.Clamp(MinMoveApplyChance, MaxMoveApplyChance);

        float BaseMoveApplyChance { get; set; }
        SelfSortingList<IMoveModifier> MoveApplyModifiers { get; }
        void SubscribeMoveApplyChance(IBaseFloatAttributeModifier modifier, bool allowDuplicates);
        void UnsubscribeMoveApplyChance(IBaseFloatAttributeModifier modifier);
        void ModifyEffectApplying(ref MoveToApply effectStruct);
        protected float GetMoveApplyChanceInternal();
        public sealed float GetMoveApplyChance() => ClampMoveApplyChance(GetMoveApplyChanceInternal());
    #endregion

    #region Poison
        public const float MinPoisonApplyChance = -3f;
        public const float MaxPoisonApplyChance = 3f;
        public static float ClampPoisonApplyChance(float poisonApplyChance) => poisonApplyChance.Clamp(MinPoisonApplyChance, MaxPoisonApplyChance);

        float BasePoisonApplyChance { get; set; }
        SelfSortingList<IPoisonModifier> PoisonApplyModifiers { get; }
        void ModifyEffectApplying(ref PoisonToApply effectStruct);
        void SubscribePoisonApplyChance(IBaseFloatAttributeModifier modifier, bool allowDuplicates);
        void UnsubscribePoisonApplyChance(IBaseFloatAttributeModifier modifier);
        protected float GetPoisonApplyChanceInternal();
        public sealed float GetPoisonApplyChance() => ClampPoisonApplyChance(GetPoisonApplyChanceInternal());
    #endregion

        SelfSortingList<IOvertimeHealModifier> OvertimeHealApplyModifiers { get; }
        void ModifyEffectApplying(ref OvertimeHealToApply effectStruct);
        
        SelfSortingList<IGuardedModifier> GuardedApplyModifiers { get; }
        void ModifyEffectApplying(ref GuardedToApply effectStruct);

        SelfSortingList<IHealModifier> HealApplyModifiers { get; }
        void ModifyEffectApplying(ref HealToApply effectStruct);

        SelfSortingList<ILustModifier> LustApplyModifiers { get; }
        void ModifyEffectApplying(ref LustToApply effectStruct);

        SelfSortingList<ILustGrappledModifier> LustGrappledApplyModifiers { get; }
        void ModifyEffectApplying(ref LustGrappledToApply effectStruct);

        SelfSortingList<IMarkedModifier> MarkedApplyModifiers { get; }
        void ModifyEffectApplying(ref MarkedToApply effectStruct);

        SelfSortingList<IRiposteModifier> RiposteApplyModifiers { get; }
        void ModifyEffectApplying(ref RiposteToApply effectStruct);

        SelfSortingList<IStunModifier> StunApplyModifiers { get; }
        void ModifyEffectApplying(ref StunToApply effectStruct);

        SelfSortingList<ISummonModifier> SummonApplyModifiers { get; }
        void ModifyEffectApplying(ref SummonToApply effectStruct);
        
        SelfSortingList<ITemptModifier> TemptationApplyModifiers { get; }
        void ModifyEffectApplying(ref TemptToApply effectStruct);
    }
}