using Core.Combat.Scripts.Animations;
using Core.Utils.Extensions;
using Core.Utils.Patterns;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Combat.Scripts.Behaviour.Rendering
{
    public abstract class BaseCharacterAnimatorController : MonoBehaviour, ICharacterAnimatorController
    {
        private static readonly int SpeedParameter = Animator.StringToHash("Speed");

        [SerializeField, Required]
        protected Animator animator;
        
        private bool _isIdleSpeedPaused;
        private Option<float> _speedBeforePausing;

        public void AllowIdleAnimationTimeUpdate(bool value)
        {
            if (value == !_isIdleSpeedPaused)
                return;
            
            _isIdleSpeedPaused = !value;
            if (value == false)
            {
                _speedBeforePausing = Option<float>.Some(animator.GetFloat(SpeedParameter));
                animator.SetFloat(SpeedParameter, 0);
                return;
            }

            if (_speedBeforePausing.IsNone)
            {
                Debug.Log("No speeds before pausing were saved, setting to 1.", this);
                animator.SetFloat(SpeedParameter, 1);
                return;
            }

            animator.SetFloat(SpeedParameter, _speedBeforePausing.Value);
            _speedBeforePausing = Option<float>.None;
        }

        public void SetIdleSpeed(float value)
        {
            if (_isIdleSpeedPaused)
            {
                _speedBeforePausing = Option<float>.Some(value);
                return;
            }
            
            animator.SetFloat(SpeedParameter, value);
        }
        
        public void SetBaseSpeed(float speed) => animator.speed = speed;

        public void ClearParameters()
        {
            animator.ClearBoolsAndTriggers();
        }

        public abstract void SetAnimation(in CombatAnimation combatAnimation);
    }
}