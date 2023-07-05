using Core.Combat.Scripts.Effects.BaseTypes;
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
using Core.Combat.Scripts.Effects.Types.Tempt;
using Core.Utils.Collections;

namespace Core.Combat.Scripts.Interfaces.Modules
{
    public interface IStatusReceiverModule : IModule
    {
        SelfSortingList<IArousalModifier> ArousalReceiveModifiers { get; }
        void ModifyEffectReceiving(ref ArousalToApply effectStruct);
        
        SelfSortingList<IBuffOrDebuffModifier> BuffOrDebuffReceiveModifiers { get; }
        void ModifyEffectReceiving(ref BuffOrDebuffToApply effectStruct);
        
        SelfSortingList<IGuardedModifier> GuardedReceiveModifiers { get; }
        void ModifyEffectReceiving(ref GuardedToApply effectStruct);
        
        SelfSortingList<IHealModifier> HealReceiveModifiers { get; }
        void ModifyEffectReceiving(ref HealToApply effectStruct);
        
        SelfSortingList<ILustModifier> LustReceiveModifiers { get; }
        void ModifyEffectReceiving(ref LustToApply effectStruct);
        
        SelfSortingList<ILustGrappledModifier> LustGrappledReceiveModifiers { get; }
        void ModifyEffectReceiving(ref LustGrappledToApply effectStruct);
        
        SelfSortingList<IMarkedModifier> MarkedReceiveModifiers { get; }
        void ModifyEffectReceiving(ref MarkedToApply effectStruct);
        
        SelfSortingList<IMoveModifier> MoveReceiveModifiers { get; }
        void ModifyEffectReceiving(ref MoveToApply effectStruct);
        
        SelfSortingList<IOvertimeHealModifier> OvertimeHealReceiveModifiers { get; }
        void ModifyEffectReceiving(ref OvertimeHealToApply effectStruct);
        
        SelfSortingList<IPoisonModifier> PoisonReceiveModifiers { get; }
        void ModifyEffectReceiving(ref PoisonToApply effectStruct);
        
        SelfSortingList<IRiposteModifier> RiposteReceiveModifiers { get; }
        void ModifyEffectReceiving(ref RiposteToApply effectStruct);

        SelfSortingList<IStunModifier> StunReceiveModifiers { get; }
        void ModifyEffectReceiving(ref StunToApply effectStruct);
        
        SelfSortingList<ITemptModifier> TemptationReceiveModifiers { get; }
        void ModifyEffectReceiving(ref TemptToApply effectStruct);

        void TrackRelatedStatus(StatusInstance statusInstance);
        void UntrackRelatedStatus(StatusInstance statusInstance);
    }
}