using Core.Combat.Scripts.Skills.Action;

namespace Core.Combat.Scripts.Interfaces.Events
{
    public interface IRiposteActivatedListener
    {
        void OnRiposteActivated(ref ActionResult actionResult);
    }
}