using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils.Extensions;
using Utils.Objects;
using Utils.Patterns;

namespace Core.Combat.Scripts.Cues
{
    public class StatusEffectCue : MonoBehaviour
    {
        private const float StayDuration = 1f;
        private const float FadeDuration = 0.35f;
        private const float FullDuration = StayDuration + FadeDuration;

        [SerializeField, Required]
        private Image image;
        
        [SerializeField, Required]
        private TMP_Text tmp;
        
        [SerializeField, Required]
        private CustomAudioSource audioSource;
        
        private Sequence _sequence;
        public bool BeingUsed => _sequence is { active: true };
        private StatusVFXManager _manager;
        private TweenCallback _onFadeEnd;
        private TweenCallback _onAudioEnd;

        private void Start()
        {
            gameObject.SetActive(false);
        }

        public void Initialize(StatusVFXManager manager)
        {
            _manager = manager;
            _onFadeEnd = () =>
            {
                image.sprite = null;
                tmp.text = string.Empty;
                _manager.NotifyFadeFinished(cue: this);
            };

            _onAudioEnd = () =>
            {
                gameObject.SetActive(false);
                _manager.NotifyAudioFinished(cue: this);
            };
        }

        public void Animate(Option<Sprite> sprite, string text, Color textColor, Vector3 worldPosition, Vector3 direction, Option<AudioClip> clip)
        {
            _sequence.KillIfActive();
            gameObject.SetActive(true);
            
            if (sprite.IsSome)
            {
                image.color = Color.white;
                image.sprite = sprite.Value;
                image.gameObject.SetActive(true);
            }
            else
            {
                image.gameObject.SetActive(false);
            }

            tmp.text = text;
            tmp.color = textColor;
            transform.position = worldPosition;

            Vector3 endPosition = worldPosition + direction * StayDuration;

            _sequence = DOTween.Sequence();
            _sequence.Append(transform.DOMove(endPosition, StayDuration));
            
            _sequence.Append(image.DOFade(endValue: 0f, FadeDuration));
            _sequence.Join(tmp.DOFade(endValue: 0f, FadeDuration));
            _sequence.AppendCallback(_onFadeEnd);

            if (clip.IsNone)
            {
                _sequence.AppendCallback(_onAudioEnd);
                return;
            }

            AudioClip clipValue = clip.Value;
            audioSource.Clip = clipValue;
            audioSource.Play();
            if (clipValue.length > FullDuration)
            {
                DOVirtual.DelayedCall(clipValue.length, _onAudioEnd, ignoreTimeScale: false).SetTarget(this);
            }
            else
            {
                _sequence.AppendCallback(_onAudioEnd);
            }
        }

        private void OnDestroy()
        {
            _sequence.KillIfActive();
            this.DOKill();
        }
    }
}