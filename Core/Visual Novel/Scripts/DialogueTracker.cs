using UnityEngine;

namespace Core.Visual_Novel.Scripts
{
    public class DialogueTracker : CustomYieldInstruction
    {
        public bool IsDone { get; private set; }
        public bool Interrupted { get; private set; }

        public void SetDone(bool interrupted)
        {
            IsDone = true;
            Interrupted = interrupted;
        }

        public override bool keepWaiting => IsDone == false;
    }
}