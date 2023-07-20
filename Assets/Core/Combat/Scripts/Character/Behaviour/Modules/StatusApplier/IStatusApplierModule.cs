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
using Core.Utils.Collections;
using Core.Utils.Math;

namespace Core.Combat.Scripts.Behaviour.Modules
{
    public abstract record StatusApplierRecord : ModuleRecord
    {
        public abstract IStatusApplierModule Deserialize(CharacterStateMachine owner);
    }
    
    public interface IStatusApplierModule : IModule
    {
    #region Arousal
        public const int MinArousalApplyChance = -300;
        public const int MaxArousalApplyChance = 300;
        public static int ClampArousalApplyChance(int arousalApplyChance) => arousalApplyChance.Clamp(MinArousalApplyChance, MaxArousalApplyChance);
        
        int BaseArousalApplyChance { get; set; }
        SelfSortingList<IArousalModifier> ArousalApplyModifiers { get; }
        void SubscribeArousalApplyChance(IBaseAttributeModifier modifier, bool allowDuplicates);
        void UnsubscribeArousalApplyChance(IBaseAttributeModifier modifier);
        void ModifyEffectApplying(ref ArousalToApply effectStruct);
        protected int GetArousalApplyChanceInternal();
        public sealed int GetArousalApplyChance() => ClampArousalApplyChance(GetArousalApplyChanceInternal());
    #endregion

    #region Debuff
        public const int MinDebuffApplyChance = -300;
        public const int MaxDebuffApplyChance = 300;
        public static int ClampDebuffApplyChance(int debuffApplyChance) => debuffApplyChance.Clamp(MinDebuffApplyChance, MaxDebuffApplyChance);

        int BaseDebuffApplyChance { get; set; }
        SelfSortingList<IBuffOrDebuffModifier> BuffOrDebuffApplyModifiers { get; }
        void SubscribeDebuffApplyChance(IBaseAttributeModifier modifier, bool allowDuplicates);
        void UnsubscribeDebuffApplyChance(IBaseAttributeModifier modifier);
        void ModifyEffectApplying(ref BuffOrDebuffToApply effectStruct);
        protected int GetDebuffApplyChanceInternal();
        public sealed int GetDebuffApplyChance() => ClampDebuffApplyChance(GetDebuffApplyChanceInternal());
    #endregion

    #region Move
        public const int MinMoveApplyChance = -300;
        public const int MaxMoveApplyChance = 300;
        public static int ClampMoveApplyChance(int moveApplyChance) => moveApplyChance.Clamp(MinMoveApplyChance, MaxMoveApplyChance);

        int BaseMoveApplyChance { get; set; }
        SelfSortingList<IMoveModifier> MoveApplyModifiers { get; }
        void SubscribeMoveApplyChance(IBaseAttributeModifier modifier, bool allowDuplicates);
        void UnsubscribeMoveApplyChance(IBaseAttributeModifier modifier);
        void ModifyEffectApplying(ref MoveToApply effectStruct);
        protected int GetMoveApplyChanceInternal();
        public sealed int GetMoveApplyChance() => ClampMoveApplyChance(GetMoveApplyChanceInternal());
    #endregion

    #region Poison
        public const int MinPoisonApplyChance = -300;
        public const int MaxPoisonApplyChance = 300;
        public static int ClampPoisonApplyChance(int poisonApplyChance) => poisonApplyChance.Clamp(MinPoisonApplyChance, MaxPoisonApplyChance);

        int BasePoisonApplyChance { get; set; }
        SelfSortingList<IPoisonModifier> PoisonApplyModifiers { get; }
        void ModifyEffectApplying(ref PoisonToApply effectStruct);
        void SubscribePoisonApplyChance(IBaseAttributeModifier modifier, bool allowDuplicates);
        void UnsubscribePoisonApplyChance(IBaseAttributeModifier modifier);
        protected int GetPoisonApplyChanceInternal();
        public sealed int GetPoisonApplyChance() => ClampPoisonApplyChance(GetPoisonApplyChanceInternal());
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
        
        StatusApplierRecord GetRecord();
    }
}