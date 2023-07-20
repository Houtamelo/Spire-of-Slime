using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Animations;

namespace Core.Animation
{
    public class RandomStateMachinePicker : StateMachineBehaviour
    {
        [InfoBox("Trigger name is " + TriggerName), SerializeField] private int stateCount;

        private const string TriggerName = "RandomState";
        private static readonly int RandomState = Animator.StringToHash(TriggerName);
        private readonly System.Random _random = new();
        
        public override void OnStateEnter([NotNull] Animator animator, AnimatorStateInfo stateInfo, int layerIndex, AnimatorControllerPlayable controller)
        {
            animator.SetInteger(RandomState, _random.Next(0, stateCount));
            base.OnStateEnter(animator, stateInfo, layerIndex, controller);
        }
    }
}