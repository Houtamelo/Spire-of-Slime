using System;
using Core.Utils.Extensions;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Audio.Scripts.MusicControllers
{
    public class ExplorationMusicController : MusicController
    {
        [SerializeField, Required]
        private AudioSource exploration, combat;

        [SerializeField]
        private float fadeDuration = 3f;
        private float RatioedDuration => fadeDuration * _ratio;

        [SerializeField]
        private float normalPitch = 1f, losingCombatPitch = 1.2f;

        private float _maxVolume = 1f;
        private float _ratio = 1f;
        private Tween _tween;

        private void Start()
        {
            exploration.time = 0;
            exploration.Play();
            combat.time = 0;
            combat.Play();
            
            _tween = exploration.DOFade(endValue: _maxVolume, RatioedDuration).SetUpdate(isIndependentUpdate: true).SetTarget(this);
        }

        public override void SetVolume(float volume)
        {
            _maxVolume = volume;
            _ratio = volume != 0f ? 1f / volume : 0f;

            _tween.KillIfActive();
            Sequence sequence = DOTween.Sequence(this).SetUpdate(isIndependentUpdate: true);
            _tween = sequence;
            switch (State)
            {
                case MusicEvent.Exploration:
                {
                    sequence.Append(exploration.DOFade(endValue: volume, RatioedDuration * Mathf.Abs(exploration.volume - volume)));
                    if (combat.volume > 0f)
                        sequence.Join(combat.DOFade(endValue: 0f, RatioedDuration * combat.volume));
                    break;
                }
                case MusicEvent.CombatLosing:
                case MusicEvent.Combat:
                {
                    sequence.Append(exploration.DOFade(endValue: volume, RatioedDuration * Mathf.Abs(exploration.volume - volume)));
                    sequence.Join(combat.DOFade(endValue: volume, RatioedDuration * Mathf.Abs(combat.volume - volume)));
                    break;
                }
                default: throw new ArgumentOutOfRangeException(nameof(State), State, message: $"Unhandled state {State}");
            }
        }

        public override void SetState(MusicEvent newState)
        {
            MusicEvent oldState = State;
            State = newState;
            if (oldState == newState)
                return;
            
            _tween.KillIfActive();
            switch (oldState, newState)
            {
                case (MusicEvent.Exploration, MusicEvent.Combat):
                {
                    float volumeDifference = Math.Abs(_maxVolume - combat.volume);
                    if (volumeDifference > 0.00001f)
                        _tween = combat.DOFade(endValue: _maxVolume, RatioedDuration * volumeDifference).SetUpdate(isIndependentUpdate: true).SetTarget(this);
                    break;
                }
                case (MusicEvent.Exploration, MusicEvent.CombatLosing):
                {
                    exploration.pitch = losingCombatPitch;
                    combat.pitch = losingCombatPitch;
                    
                    float volumeDifference = Math.Abs(_maxVolume - combat.volume);
                    if (volumeDifference > 0.00001f)
                        _tween = combat.DOFade(endValue: _maxVolume, RatioedDuration * volumeDifference).SetUpdate(isIndependentUpdate: true).SetTarget(this);
                    break;
                }
                case (MusicEvent.Combat, MusicEvent.Exploration):
                {
                    if (combat.volume > 0f)
                        _tween = combat.DOFade(endValue: 0f, RatioedDuration * combat.volume).SetUpdate(isIndependentUpdate: true).SetTarget(this);
                    break;
                }
                case (MusicEvent.Combat, MusicEvent.CombatLosing):
                {
                    exploration.pitch = losingCombatPitch;
                    combat.pitch = losingCombatPitch;
                    
                    float volumeDifference = Math.Abs(_maxVolume - combat.volume);
                    if (volumeDifference > 0.00001f)
                        _tween = combat.DOFade(endValue: _maxVolume, RatioedDuration * volumeDifference).SetUpdate(isIndependentUpdate: true).SetTarget(this);
                    break;
                }
                case (MusicEvent.CombatLosing, MusicEvent.Exploration):
                {
                    exploration.pitch = normalPitch;
                    combat.pitch = normalPitch;
                    
                    if (combat.volume > 0f)
                        _tween = combat.DOFade(endValue: 0f, RatioedDuration * combat.volume).SetUpdate(isIndependentUpdate: true).SetTarget(this);
                    break;
                }
                case (MusicEvent.CombatLosing, MusicEvent.Combat):
                {
                    exploration.pitch = normalPitch;
                    combat.pitch = normalPitch;
                    
                    float volumeDifference = Math.Abs(_maxVolume - combat.volume);
                    if (volumeDifference > 0.00001f)
                        _tween = combat.DOFade(endValue: _maxVolume, RatioedDuration * volumeDifference).SetUpdate(isIndependentUpdate: true).SetTarget(this);
                    break;
                }
            }
        }

        public override void FadeDownAndDestroy(float duration)
        {
            _tween.KillIfActive();

            if (exploration.volume <= 0f)
            {
                Destroy(gameObject);
                return;
            }
            
            Sequence sequence = DOTween.Sequence(this).SetUpdate(isIndependentUpdate: true);
            sequence.Append(exploration.DOFade(endValue: 0f, duration));
            if (combat.volume > 0f)
                sequence.Join(combat.DOFade(endValue: 0f, duration));
            
            GameObject self = gameObject;
            sequence.onComplete += () =>
            {
                if (self != null)
                    Destroy(self);
            };
        }

        private void OnDestroy()
        {
            _tween.KillIfActive();
            this.DOKill();
        }
    }
}