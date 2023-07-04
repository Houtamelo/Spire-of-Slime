using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Visual_Novel.Scripts.Animations
{
    [RequireComponent(typeof(Animator))]
    public class AnimatorBasedAnimation : VisualNovelAnimation
    {
        private static readonly int PlayHash = Animator.StringToHash("Play");
        private static readonly int FinalStateHash = Animator.StringToHash("FinalState");
        
        [InfoBox("Triggers: Play, FinalState"), SerializeField]
        private Animator animator;
        
        [SerializeField]
        private float duration = 1;

        public override YieldInstruction Play()
        {
            animator.SetTrigger(PlayHash);
            return new WaitForSeconds(duration);
        }

        public override void ForceFinalState()
        {
            animator.SetTrigger(FinalStateHash);
        }

        private void Reset()
        {
            animator = GetComponent<Animator>();
        }
    }
}