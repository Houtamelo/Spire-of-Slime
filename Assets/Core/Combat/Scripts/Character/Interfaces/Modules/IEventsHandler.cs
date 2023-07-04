using System.Collections.Generic;
using Core.Combat.Scripts.Interfaces.Events;
using Core.Combat.Scripts.Skills.Action;
using ListPool;

namespace Core.Combat.Scripts.Interfaces.Modules
{
    public interface IEventsHandler : IModule
    {
        List<ISelfAttackedListener> SelfAttackedListeners { get; }
        void OnSelfAttacked(ref ActionResult result);

        List<ITargetAttackedListener> TargetAttackedListeners { get; }
        void OnTargetAttacked(ref ActionResult result);

        List<IActionCompletedListener> ActionCompletedListeners { get; }
        void OnActionCompleted(ListPool<ActionResult> results);

        List<IStatusReceivedListener> StatusReceivedListeners { get; }
        void OnStatusReceived(ref StatusResult result);
        
        List<IStatusAppliedListener> StatusAppliedListeners { get; }
        void OnStatusApplied(ref StatusResult result);

        List<IRiposteActivatedListener> RiposteActivatedListeners { get; }
        void OnRiposteActivated(ref ActionResult result);
    }
}