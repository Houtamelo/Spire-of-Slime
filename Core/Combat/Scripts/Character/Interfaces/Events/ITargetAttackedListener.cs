using Core.Combat.Scripts.Skills.Action;

namespace Core.Combat.Scripts.Interfaces.Events
{
    public interface ITargetAttackedListener
    {
        void OnTargetAttacked(ref ActionResult actionResult);
    }
}