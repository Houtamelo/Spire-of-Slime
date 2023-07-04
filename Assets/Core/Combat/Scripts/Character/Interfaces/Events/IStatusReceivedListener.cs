using Core.Combat.Scripts.Skills.Action;

namespace Core.Combat.Scripts.Interfaces.Events
{
    public interface IStatusReceivedListener
    {
        void OnStatusReceived(ref StatusResult statusResult);
    }
}