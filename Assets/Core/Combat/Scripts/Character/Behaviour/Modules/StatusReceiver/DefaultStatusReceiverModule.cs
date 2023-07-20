using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts.Effects;
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
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Timeline;
using Core.Utils.Collections;
using Core.Utils.Collections.Extensions;
using Core.Utils.Extensions;
using Core.Utils.Math;
using JetBrains.Annotations;

namespace Core.Combat.Scripts.Behaviour.Modules
{
    public record DefaultStatusReceiverRecord(StatusRecord[] Statuses) : StatusReceiverRecord
    {
        [NotNull]
        public override IStatusReceiverModule Deserialize(CharacterStateMachine owner) => new DefaultStatusReceiverModule(owner);

        public override void AddSerializedStatuses(CharacterStateMachine owner, DirectCharacterEnumerator allCharacters)
        {
            foreach (StatusRecord statusRecord in Statuses)
                statusRecord.Deserialize(owner, allCharacters);
        }

        public override bool IsDataValid(in StringBuilder errors, CharacterRecord[] allCharacters)
        {
            if (Statuses == null)
            {
                errors.AppendLine("Invalid ", nameof(CharacterRecord), ". ", nameof(Statuses), " is null.");
                return false;
            }

            for (int index = 0; index < Statuses.Length; index++)
            {
                StatusRecord statusRecord = Statuses[index];
                if (statusRecord == null)
                {
                    errors.AppendLine("Invalid ", nameof(CharacterRecord), ". Status at index ", index.ToString(), " is null.");
                    return false;
                }

                if (statusRecord.IsDataValid(errors, allCharacters) == false)
                    return false;
            }
            
            return true;
        }
    }

    public class DefaultStatusReceiverModule : IStatusReceiverModule
    {
        private readonly CharacterStateMachine _owner;

        public DefaultStatusReceiverModule(CharacterStateMachine owner) => _owner = owner;

        /// <summary> Be careful while accessing this - always enumerate with Fixed </summary>
        private readonly HashSet<StatusInstance> _statuses = new();

        /// <summary> Be careful while accessing this - always enumerate with Fixed </summary>
        private readonly HashSet<StatusInstance> _relatedStatuses = new();

        public FixedEnumerator<StatusInstance> GetAll => _statuses.FixedEnumerate();
        public FixedEnumerator<StatusInstance> GetAllRelated => _relatedStatuses.FixedEnumerate();

        [NotNull]
        public StatusReceiverRecord GetRecord()
        {
            StatusRecord[] statuses = new StatusRecord[_statuses.Count];
            int index = 0;
            foreach (StatusInstance status in _statuses)
            {
                statuses[index] = status.GetRecord();
                index++;
            }
            
            return new DefaultStatusReceiverRecord(statuses);
        }

        public bool HasActiveStatusOfType(EffectType effectType)
        {
            foreach (StatusInstance status in GetAll)
            {
                if (status.EffectType == effectType && status.IsDeactivated == false)
                    return true;
            }

            return false;
        }

        public void AddStatus([NotNull] StatusInstance statusInstance, [NotNull] CharacterStateMachine caster)
        {
            if (statusInstance.EffectType.OnlyOneAllowed())
                DeactivateStatusByType(statusInstance.EffectType);
            
            StatusResult result = StatusResult.Success(caster, _owner, statusInstance, true, statusInstance.EffectType);
            _statuses.Add(statusInstance);
            
            caster.Events.OnStatusApplied(ref result);
            _owner.Events.OnStatusReceived(ref result);

            if (statusInstance.IsDeactivated || statusInstance.EffectType.HasIcon() == false)
                return;
            
            if (_owner.Display.AssertSome(out DisplayModule display))
                display.CreateIconForStatus(statusInstance);
        }

        public void RemoveStatus(StatusInstance effectInstance)
        {
            _statuses.Remove(effectInstance);
            if (_owner.Display.AssertSome(out DisplayModule display))
                display.StatusIconRemoved(effectInstance);
        }

        public bool DeactivateStatusByType(EffectType effectType)
        {
            bool found = false;
            foreach (StatusInstance statusInstance in GetAll)
            {
                if (statusInstance.EffectType != effectType)
                    continue;
                
                statusInstance.RequestDeactivation();
                found = true;
            }
            
            return found;
        }

        public void RemoveAll()
        {
            foreach (StatusInstance status in GetAll) 
                status.RequestDeactivation();

            _statuses.Clear();
        }

        public virtual void TrackRelatedStatus(StatusInstance statusInstance) => _relatedStatuses.Add(statusInstance);

        public virtual void UntrackRelatedStatus(StatusInstance statusInstance) => _relatedStatuses.Remove(statusInstance);

        public void Tick(TSpan timeStep)
        {
            foreach (StatusInstance status in GetAll)
                status.Tick(timeStep);
        }

        public void ShowStatusTooltip(string description)
        {
            if (_owner.Display.TrySome(out DisplayModule display))
                display.ShowStatusTooltip(description);
        }

        public void HideStatusTooltip()
        {
            if (_owner.Display.TrySome(out DisplayModule display))
                display.HideStatusTooltip();
        }

        public void FillTimelineEvents(in SelfSortingList<CombatEvent> events)
        {
            foreach (StatusInstance statusInstance in _statuses)
                statusInstance.FillTimelineEvents(events);
        }

#region Receive Modifiers

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

#endregion
    }
}