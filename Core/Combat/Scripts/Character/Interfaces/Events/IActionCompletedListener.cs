using Core.Combat.Scripts.Skills.Action;
using ListPool;

namespace Core.Combat.Scripts.Interfaces.Events
{
    public interface IActionCompletedListener
    {
        void OnActionCompleted(ListPool<ActionResult> actionResults);
    }
}