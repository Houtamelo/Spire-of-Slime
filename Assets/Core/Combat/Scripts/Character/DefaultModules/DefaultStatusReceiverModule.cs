using Core.Combat.Scripts.Effects.BaseTypes;
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
using Core.Combat.Scripts.Effects.Types.Tempt;
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Utils.Collections;

namespace Core.Combat.Scripts.DefaultModules
{
    public abstract class DefaultStatusReceiverModule : IStatusReceiverModule
    {
        public SelfSortingList<IArousalModifier> ArousalReceiveModifiers { get; } = new(ModifierComparer.Instance);
        public void ModifyEffectReceiving(ref ArousalToApply effectStruct)
        {
            foreach (IArousalModifier modifier in ArousalReceiveModifiers) 
                modifier.Modify(ref effectStruct);
        }
        
        public SelfSortingList<IBuffOrDebuffModifier> BuffOrDebuffReceiveModifiers { get; } = new(ModifierComparer.Instance);
        public void ModifyEffectReceiving(ref BuffOrDebuffToApply effectStruct)
        {
            foreach (IBuffOrDebuffModifier modifier in BuffOrDebuffReceiveModifiers) 
                modifier.Modify(ref effectStruct);
        }
        
        public SelfSortingList<IGuardedModifier> GuardedReceiveModifiers { get; } = new(ModifierComparer.Instance);
        public void ModifyEffectReceiving(ref GuardedToApply effectStruct)
        {
            foreach (IGuardedModifier modifier in GuardedReceiveModifiers) 
                modifier.Modify(ref effectStruct);
        }
        
        public SelfSortingList<IHealModifier> HealReceiveModifiers { get; } = new(ModifierComparer.Instance);
        public void ModifyEffectReceiving(ref HealToApply effectStruct)
        {
            foreach (IHealModifier modifier in HealReceiveModifiers) 
                modifier.Modify(ref effectStruct);
        }
        
        public SelfSortingList<ILustModifier> LustReceiveModifiers { get; } = new(ModifierComparer.Instance);
        public void ModifyEffectReceiving(ref LustToApply effectStruct)
        {
            foreach (ILustModifier modifier in LustReceiveModifiers)
                modifier.Modify(ref effectStruct);
        }
        
        public SelfSortingList<ILustGrappledModifier> LustGrappledReceiveModifiers { get; } = new(ModifierComparer.Instance);
        public void ModifyEffectReceiving(ref LustGrappledToApply effectStruct)
        {
            foreach (ILustGrappledModifier modifier in LustGrappledReceiveModifiers) 
                modifier.Modify(ref effectStruct);
        }
        
        public SelfSortingList<IMarkedModifier> MarkedReceiveModifiers { get; } = new(ModifierComparer.Instance);
        public void ModifyEffectReceiving(ref MarkedToApply effectStruct)
        {
            foreach (IMarkedModifier modifier in MarkedReceiveModifiers) 
                modifier.Modify(ref effectStruct);
        }
        
        public SelfSortingList<IMoveModifier> MoveReceiveModifiers { get; } = new(ModifierComparer.Instance);
        public void ModifyEffectReceiving(ref MoveToApply effectStruct)
        {
            foreach (IMoveModifier modifier in MoveReceiveModifiers) 
                modifier.Modify(ref effectStruct);
        }
        
        public SelfSortingList<IOvertimeHealModifier> OvertimeHealReceiveModifiers { get; } = new(ModifierComparer.Instance);
        public void ModifyEffectReceiving(ref OvertimeHealToApply effectStruct)
        {
            foreach (IOvertimeHealModifier modifier in OvertimeHealReceiveModifiers) 
                modifier.Modify(ref effectStruct);
        }
        
        public SelfSortingList<IPoisonModifier> PoisonReceiveModifiers { get; } = new(ModifierComparer.Instance);
        public void ModifyEffectReceiving(ref PoisonToApply effectStruct)
        {
            foreach (IPoisonModifier modifier in PoisonReceiveModifiers) 
                modifier.Modify(ref effectStruct);
        }
        
        public SelfSortingList<IRiposteModifier> RiposteReceiveModifiers { get; } = new(ModifierComparer.Instance);
        public void ModifyEffectReceiving(ref RiposteToApply effectStruct)
        {
            foreach (IRiposteModifier modifier in RiposteReceiveModifiers) 
                modifier.Modify(ref effectStruct);
        }

        public SelfSortingList<IStunModifier> StunReceiveModifiers { get; } = new(ModifierComparer.Instance);
        public void ModifyEffectReceiving(ref StunToApply effectStruct)
        {
            foreach (IStunModifier modifier in StunReceiveModifiers) 
                modifier.Modify(ref effectStruct);
        }
        
        public SelfSortingList<ITemptModifier> TemptationReceiveModifiers { get; } = new(ModifierComparer.Instance);
        public void ModifyEffectReceiving(ref TemptToApply effectStruct)
        {
            foreach (ITemptModifier modifier in TemptationReceiveModifiers) 
                modifier.Modify(ref effectStruct);
        }

        public abstract void TrackRelatedStatus(StatusInstance statusInstance);
        public abstract void UntrackRelatedStatus(StatusInstance statusInstance);
    }
}