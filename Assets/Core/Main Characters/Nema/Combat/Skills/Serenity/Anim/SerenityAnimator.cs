using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Behaviour.Rendering;
using Core.Combat.Scripts.Skills.Action;
using Core.Utils.Collections.Extensions;
using Core.Utils.Extensions;
using Core.Utils.Objects;
using Core.Utils.Patterns;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Main_Characters.Nema.Combat.Skills.Serenity.Anim
{
    public class SerenityAnimator : MonoBehaviour
    {
        [SerializeField, Required]
        private Transform effects;

        [SerializeField]
        private float duration;
        private float GetActualDuration() => duration * IActionSequence.DurationMultiplier;

        [SerializeField, Required]
        private Transform selfCharacter;
        
        [SerializeField, Required]
        private CustomAudioSource[] sounds;

        private Sequence _sequence;
        private Option<CharacterStateMachine> _currentTarget;
        private TweenCallback _onSequenceUpdate;
        private TweenCallback _onSequenceComplete;
        private ICharacterRenderer _renderer;
        private bool _awaken;

        private void Awake()
        {
            _onSequenceUpdate = OnSequenceUpdate;
            _onSequenceComplete = OnSequenceComplete;
            _renderer = selfCharacter.GetComponent<ICharacterRenderer>();
            _awaken = true;
        }
        
        public void AnimateSerenity(Option<CasterContext> casterContext)
        {
            if (_awaken == false)
                Awake();
            
            _sequence.KillIfActive();
            if (sounds.HasElements())
                sounds.GetRandom().Play();
            
            Option<CharacterStateMachine> target = Option<CharacterStateMachine>.None;
            if (casterContext.IsSome)
            {
                foreach (ActionResult result in casterContext.Value.Results)
                {
                    if (result.Caster == result.Target)
                        continue;
                    
                    target = Option<CharacterStateMachine>.Some(result.Target);
                    break;
                }
                
                if (target.IsNone)
                    foreach (ActionResult result in casterContext.Value.Results)
                    {
                        target = Option<CharacterStateMachine>.Some(result.Target);
                        break;
                    }
            }

            effects.gameObject.SetActive(true);
            _currentTarget = target;

            _sequence = DOTween.Sequence().OnUpdate(_onSequenceUpdate).OnComplete(_onSequenceComplete);
            _sequence.AppendInterval(GetActualDuration());

            if (_currentTarget.IsSome && _currentTarget.Value.Display.AssertSome(out DisplayModule targetDisplay) && targetDisplay.GetBounds().TrySome(out Bounds targetBounds))
            {
                Vector3 desiredPosition = effects.position;
                desiredPosition.x = targetBounds.center.x;
                effects.position = desiredPosition;
            }
            else if (_renderer != null)
            {
                effects.position = _renderer.GetBounds().center;
            }
            else
            {
                Debug.LogWarning($"No renderer found for {selfCharacter.name}", context: this);
                effects.position = transform.position + new Vector3(0, 1.8f, 0);
            }
        }

        private void OnSequenceUpdate()
        {
            if (_currentTarget.IsNone || _currentTarget.Value.Display.IsNone || _currentTarget.Value.Display.Value.GetBounds().TrySome(out Bounds bounds) == false)
                return;
            
            Vector3 desiredPosition = effects.position;
            desiredPosition.x = bounds.center.x;
            effects.position = desiredPosition;
        }

        private void OnSequenceComplete()
        {
            _currentTarget = Option<CharacterStateMachine>.None;
            effects.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            _sequence.KillIfActive();
        }
    }
}