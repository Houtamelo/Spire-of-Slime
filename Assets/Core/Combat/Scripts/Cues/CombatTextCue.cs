using System.Collections.Generic;
using DG.Tweening;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;

namespace Core.Combat.Scripts.Cues
{
    public sealed class CombatTextCue : MonoBehaviour
    {
        [SerializeField] 
        private TMP_Text tmp;
        
        private Sequence _sequence;
        private readonly Queue<CombatCueOptions> _queue = new();
        private bool _isExclusive;
        public bool IsPlaying => _sequence is { active: true } || _queue.Count > 0;
        public bool IsIdle => IsPlaying == false;

        private TweenCallback _releaseCallback, _checkQueueCallback;

        private void Awake()
        {
            _checkQueueCallback = CheckQueue;
            _releaseCallback = Deactivate;
        }

        public void Exclusive(ref CombatCueOptions options)
        {
            _queue.Clear();
            if (_sequence is { active: true })
                _sequence.Kill();
            
            _isExclusive = true;
            tmp.text = options.Text;
            tmp.color = options.Color;
            tmp.fontSize = options.FontSize;
            gameObject.SetActive(true);
            transform.position = options.WorldPosition;
            _sequence = DOTween.Sequence();
            if (options.Shake)
            {
                _sequence.Append(transform.DOShakePosition(options.Duration / 2f));
            }
            else
            {
                Vector3 endPosition = options.WorldPosition + (options.Speed * options.Duration);
                _sequence.Append(transform.DOMove(endValue: endPosition, duration: options.Duration));
            }
            
            if (options.FadeOnComplete)
                _sequence.Insert(options.Duration / 2, tmp.DOFade(endValue: 0, duration: options.Duration / 2));
            
            _sequence.onComplete += _releaseCallback;
            options.OnPlay?.Invoke();
        }

        public void Enqueue(ref CombatCueOptions options)
        {
            if (_isExclusive && _sequence is { active: true }) 
                _sequence.Kill();
            
            _isExclusive = false;
            if (_sequence is { active: true })
            {
                _queue.Enqueue(item: options);
                return;
            }
            
            StartOnQueue(options: ref options);
        }

        private void StartOnQueue(ref CombatCueOptions options)
        {
            gameObject.SetActive(true);
            tmp.text = options.Text;
            tmp.color = options.Color;
            tmp.fontSize = options.FontSize;
            transform.position = options.WorldPosition;
            
            _sequence = DOTween.Sequence();
            if (options.Shake)
            {
                _sequence.Append(transform.DOShakePosition(options.Duration / 2f));
            }
            else
            {
                Vector3 endPosition = options.WorldPosition + (options.Speed * options.Duration);
                _sequence.Append(t: transform.DOMove(endValue: endPosition, duration: options.Duration));
            }
            
            if (options.FadeOnComplete)
                _sequence.Insert(atPosition: options.Duration / 2f, t: tmp.DOFade(endValue: 0, duration: options.Duration / 2f));
            
            options.OnPlay?.Invoke();
            _sequence.onComplete += _checkQueueCallback;
        }

        private void CheckQueue()
        {
            if (_queue.TryDequeue(result: out CombatCueOptions options))
                StartOnQueue(ref options);
            else
                gameObject.SetActive(false);
        }

        private void Deactivate()
        {
            _queue.Clear();
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_sequence is { active: true })
                _sequence.Kill();
        }

        [NotNull]
        public CombatTextCue OnCreate()
        {
            transform.localScale = Vector3.one;
            return this;
        }
    }
}