using System.Linq;
using CartoonFX;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Managers;
using Core.Combat.Scripts.Skills.Action;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils.Extensions;
using Utils.Objects;
using Utils.Patterns;

namespace Core.Main_Characters.Ethel.Combat.Skills.Poison_Dagger.Anim
{
    public class PoisonDaggerAnimator : MonoBehaviour
    {
        private const float CloudFxBaseDuration = 0.83f;
        private static float CloudFxDuration => CloudFxBaseDuration * IActionSequence.DurationMultiplier;
        
        private const float StayBaseDuration = 0.5f;
        private static float StayDuration => StayBaseDuration * IActionSequence.DurationMultiplier;
        
        [SerializeField]
        private float daggerSpeed;
        private float GetActualDaggerSpeed() => daggerSpeed * IActionSequence.SpeedMultiplier;
        
        [SerializeField] 
        private float daggerShakeStrength;
        
        [SerializeField] 
        private float daggerShakeDuration;
        private float GetActualDaggerShakeDuration() => daggerShakeDuration * IActionSequence.DurationMultiplier;
        
        [SerializeField]
        private float defaultWorldX;

        [SerializeField, Required]
        private float outsideScreenWorldX;
        
        [SerializeField, Required] 
        private SpriteRenderer dagger;
        
        [SerializeField, Required]
        private CFXR_Effect poisonClouds;
        
        [SerializeField]
        private Vector3 poisonCloudsOffset;
        
        [SerializeField, Required]
        private SpriteRenderer daggerParticle;
        
        [SerializeField] 
        private Vector3 localStartPosition;
        
        [SerializeField, Required] 
        private Transform daggerParent;

        [SerializeField, Required]
        private CustomAudioSource[] baseSounds;

        [SerializeField, Required]
        private CustomAudioSource[] hitSounds;

        private Sequence _sequence;
        private TweenCallback _deactivateDagger;
        private TweenCallback _activatePoisonClouds;
        
        private Transform _daggerTransform;
        private Transform _daggerParentTransform;
        private void Awake()
        {
            _deactivateDagger = DeactivateDagger;
            _daggerTransform = dagger.transform;
            _daggerParentTransform = daggerParent.transform;
            _activatePoisonClouds = ActivatePoisonClouds;
        }

        public void AnimateDagger(CasterContext casterContext)
        {
            _sequence.KillIfActive();
            
            if (baseSounds.HasElements())
                baseSounds.GetRandom().Play();
            
            _sequence = DOTween.Sequence();
            dagger.color = Color.white;
            _daggerTransform.gameObject.SetActive(true);

            float remainingDuration = IActionSequence.AnimationDuration;
            float duration;
            if (CombatManager.Instance.IsSome && casterContext.Results.Any()) // otherwise we are testing in the animation scene
            {
                Option<CharacterStateMachine> target = Option<CharacterStateMachine>.None;
                for (int index = 0; index < casterContext.Results.Length; index++)
                {
                    ref ActionResult result = ref casterContext.Results[index];
                    if (result.Hit && result.Caster != result.Target)
                    {
                        target = Option<CharacterStateMachine>.Some(result.Target);
                        break;
                    }
                }

                if (target.IsSome)
                    duration = CharacterAsDestination(target.Value);
                else
                {
                    float worldX = outsideScreenWorldX;
                    if (casterContext.Results.First().Caster.PositionHandler.IsRightSide)
                        worldX *= -1f;
                    
                    duration = PositionAsDestination(_sequence, GetActualDaggerSpeed(), localStartPosition, _daggerTransform, worldX);
                }
            }
            else
            {
                float worldX = defaultWorldX;
                duration = PositionAsDestination(_sequence, GetActualDaggerSpeed(), localStartPosition, _daggerTransform, worldX);
            }
            
            remainingDuration -= duration;

            _sequence.AppendCallback(_activatePoisonClouds);
            if (casterContext.AnyHit && hitSounds.HasElements())
            {
                CustomAudioSource sound = hitSounds.GetRandom();
                _sequence.AppendCallback(() =>
                {
                    sound.transform.position = _daggerTransform.position;
                    sound.Play();
                });
            }
            
            _sequence.Append(daggerParticle.DOFade(endValue: 0f, CloudFxDuration));
            _sequence.Join(_daggerTransform.DOShakePosition(GetActualDaggerShakeDuration(), daggerShakeStrength));

            remainingDuration -= CloudFxDuration;
            
            float interval = remainingDuration - StayDuration;
            if (interval > 0f)
            {
                _sequence.AppendInterval(interval);
                remainingDuration -= interval;
            }
            
            if (remainingDuration > 0f) 
                _sequence.Append(dagger.DOFade(endValue: 0, Mathf.Min(remainingDuration, IActionSequence.PopDuration)));
            
            _sequence.AppendCallback(_deactivateDagger);
        }

        private float CharacterAsDestination(CharacterStateMachine target)
        {
            if (target.Display.AssertSome(out CharacterDisplay targetDisplay) == false || targetDisplay.GetBounds().AssertSome(out Bounds bounds) == false)
            {
                float worldX = outsideScreenWorldX;
                if (transform.position.x > 0f)
                    worldX *= -1f;
                
                return PositionAsDestination(_sequence, GetActualDaggerSpeed(), localStartPosition, _daggerTransform, worldX);
            }
            
            Transform targetTransform = targetDisplay.transform;
            Vector3 destination = daggerParent.TransformPoint(localStartPosition);
            destination.x = bounds.center.x;
            _daggerTransform.localPosition = localStartPosition;
            float duration = Vector3.Distance(_daggerTransform.position, destination) / GetActualDaggerSpeed();
            _sequence.Append(_daggerTransform.DOMove(destination, duration));
            _sequence.AppendCallback(() => _daggerTransform.SetParent(targetTransform, worldPositionStays: true));
            return duration;
        }

        private static float PositionAsDestination(Sequence sequence, float speed, Vector3 localStartPosition, Transform daggerTransform, float worldX)
        {
            daggerTransform.localPosition = localStartPosition;
            Vector3 daggerWorldPosition = daggerTransform.position;
            float duration = Mathf.Abs(worldX - daggerWorldPosition.x) / speed;
            sequence.Append(daggerTransform.DOMoveX(worldX, duration));
            return duration;
        }

        private void DeactivateDagger()
        {
            _daggerTransform.SetParent(_daggerParentTransform); // in case we binded it to a target earlier
            dagger.gameObject.SetActive(false);
            poisonClouds.gameObject.SetActive(false);
        }

        private void ActivatePoisonClouds()
        {
            poisonClouds.transform.position = _daggerTransform.position + poisonCloudsOffset;
            poisonClouds.ResetState();
            poisonClouds.gameObject.SetActive(true);
        }
    }
}