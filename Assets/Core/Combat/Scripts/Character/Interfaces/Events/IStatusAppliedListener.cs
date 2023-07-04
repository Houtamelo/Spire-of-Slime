using Core.Combat.Scripts.Skills.Action;

namespace Core.Combat.Scripts.Interfaces.Events
{
    public interface IStatusAppliedListener
    {
        void OnStatusApplied(ref StatusResult statusResult);
    }
}