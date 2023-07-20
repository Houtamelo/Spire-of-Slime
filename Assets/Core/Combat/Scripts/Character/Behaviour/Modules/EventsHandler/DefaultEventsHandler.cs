using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts.Interfaces.Events;
using Core.Combat.Scripts.Skills.Action;
using JetBrains.Annotations;
using ListPool;

namespace Core.Combat.Scripts.Behaviour.Modules
{
    public record DefaultEventsHandlerRecord : EventsHandlerRecord
    {
        [NotNull]
        public override IEventsHandler Deserialize(CharacterStateMachine owner) => new DefaultEventsHandler();
        public override bool IsDataValid(in StringBuilder errors, CharacterRecord[] allCharacters) => true;
    }
    
    public class DefaultEventsHandler : IEventsHandler
    {
        public List<ISelfAttackedListener> SelfAttackedListeners { get; } = new();
        public List<ITargetAttackedListener> TargetAttackedListeners { get; } = new();
        public List<IActionCompletedListener> ActionCompletedListeners { get; } = new();
        public List<IStatusReceivedListener> StatusReceivedListeners { get; } = new();
        public List<IStatusAppliedListener> StatusAppliedListeners { get; } = new();
        public List<IRiposteActivatedListener> RiposteActivatedListeners { get; } = new();
        
        [NotNull]
        public EventsHandlerRecord GetRecord() => new DefaultEventsHandlerRecord();

        public void OnSelfAttacked(ref ActionResult result)
        {
            for (int i = 0; i < SelfAttackedListeners.Count; i++)
                SelfAttackedListeners[i].OnSelfAttacked(ref result);
        }
        
        public void OnTargetAttacked(ref ActionResult result)
        {
            for (int i = 0; i < TargetAttackedListeners.Count; i++)
                TargetAttackedListeners[i].OnTargetAttacked(ref result);
        }
        
        public void OnActionCompleted(ListPool<ActionResult> results)
        {
            for (int i = 0; i < ActionCompletedListeners.Count; i++)
                ActionCompletedListeners[i].OnActionCompleted(results);
        }
        
        public void OnStatusReceived(ref StatusResult result)
        {
            for (int i = 0; i < StatusReceivedListeners.Count; i++)
                StatusReceivedListeners[i].OnStatusReceived(ref result);
        }
        
        public void OnStatusApplied(ref StatusResult result)
        {
            for (int i = 0; i < StatusAppliedListeners.Count; i++)
                StatusAppliedListeners[i].OnStatusApplied(ref result);
        }

        public void OnRiposteActivated(ref ActionResult result)
        {
            for (int i = 0; i < RiposteActivatedListeners.Count; i++)
                RiposteActivatedListeners[i].OnRiposteActivated(ref result);
        }
    }
}