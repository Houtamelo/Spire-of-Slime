using Core.Combat.Scripts.Skills.Action;

namespace Core.Combat.Scripts.Interfaces.Events
{
    public interface ISelfAttackedListener
    {
        void OnSelfAttacked(ref ActionResult actionResult);
    }
}