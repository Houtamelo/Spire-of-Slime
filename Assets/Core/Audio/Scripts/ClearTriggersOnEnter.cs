using Core.Utils.Extensions;
using UnityEngine;

namespace Core.Audio.Scripts
{
    public class ClearTriggersOnEnter : StateMachineBehaviour
    {
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnStateEnter(animator, stateInfo, layerIndex);
            animator.ClearTriggers();
        }
    }
}