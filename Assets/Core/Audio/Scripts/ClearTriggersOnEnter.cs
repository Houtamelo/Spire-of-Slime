using Core.Utils.Extensions;
using JetBrains.Annotations;
using UnityEngine;

namespace Core.Audio.Scripts
{
    public class ClearTriggersOnEnter : StateMachineBehaviour
    {
        public override void OnStateEnter([NotNull] Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnStateEnter(animator, stateInfo, layerIndex);
            animator.ClearTriggers();
        }
    }
}