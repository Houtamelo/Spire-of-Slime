namespace Core.Combat.Scripts.Skills.Action
{
    public readonly struct TargetContext
    {
        public readonly ActionResult Result;

        public TargetContext(ActionResult result) => Result = result;
    }
}