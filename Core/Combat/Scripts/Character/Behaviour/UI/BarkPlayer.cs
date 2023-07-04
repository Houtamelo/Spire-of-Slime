using Core.Combat.Scripts.Barks;
using Core.Combat.Scripts.Managers;
using Core.Pause_Menu.Scripts;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using Utils.Collections;
using Utils.Extensions;
using Utils.Patterns;
using Random = System.Random;

namespace Core.Combat.Scripts.Behaviour.UI
{
    public class BarkPlayer : MonoBehaviour
    {
        private const float FadeDuration = 0.2f;
        private const float CharDuration = 0.06f;
        private const float BarkDefaultProbability = 0.075f;
        private const float StayDuration = 2f;
        
        private static readonly Random Random = new();

        [SerializeField, Required]
        private SpriteRenderer bubble;

        [SerializeField, Required]
        private TMP_Text tmp;

        [SerializeField]
        private Vector2 leftPosition, rightPosition;

        [SerializeField, Required]
        private RectTransform selfRect;

        [SerializeField, Required]
        private AudioSource typeWriterSource;
        
        [SerializeField, Required]
        private AudioClip typewriterClip;

        private bool _allowTypeWriterSound;
        private int _previousTextLength;
        private readonly ListQueue<(BarkType barkType, string result)> _barkQueue = new();
        private Option<CombatManager> _combatManager = Option<CombatManager>.None;

        private Sequence _sequence;
        private BarkType _currentBarkType;

        private TweenCallback _textTweenUpdateCallback;

        [ShowInInspector]
        public bool IsBusy => _sequence is { active: true };

        public void AssignCombatManager(CombatManager combatManager)
        {
            _combatManager = combatManager != null ? Option<CombatManager>.Some(combatManager) : Option<CombatManager>.None;
        }
        private void Start()
        {
            _allowTypeWriterSound = PauseMenuManager.TypeWriterSoundHandler.Value;
            _textTweenUpdateCallback = OnTextTweenUpdate;
            PauseMenuManager.TypeWriterSoundHandler.Changed += OnTypeWriterSoundChanged;
        }

        private void OnDestroy()
        {
            PauseMenuManager.TypeWriterSoundHandler.Changed -= OnTypeWriterSoundChanged;
        }

        private void OnTypeWriterSoundChanged(bool shouldPlay) => _allowTypeWriterSound = shouldPlay;

        private void Update()
        {
            if (_sequence is { active: true } || _combatManager.IsNone || _combatManager.Value.Animations.EvaluateState() is not QueueState.Idle)
                return;

            Option<(BarkType barkType, string result)> queued = _barkQueue.Dequeue();
            if (queued.IsNone)
                return;

            _sequence.KillIfActive();
            (BarkType barkType, string text) = queued.Value;
            float charDuration = CharDuration * text.Length;
            _currentBarkType = barkType;
            tmp.text = string.Empty;
            _sequence = DOTween.Sequence();
            if (FadeDuration > charDuration)
            {
                _sequence.Append(bubble.DOFade(1f, FadeDuration));
                _sequence.Join(tmp.DOFade(1f, FadeDuration));
                _sequence.Join(tmp.DOText(text, CharDuration * text.Length).OnUpdate(_textTweenUpdateCallback));
            }
            else
            {
                _sequence.Append(tmp.DOText(text, CharDuration * text.Length).OnUpdate(_textTweenUpdateCallback));
                _sequence.Join(bubble.DOFade(1f, FadeDuration));
                _sequence.Join(tmp.DOFade(1f, FadeDuration));
            }
            
            _sequence.AppendInterval(StayDuration);
            _sequence.Append(bubble.DOFade(0f, FadeDuration));
            _sequence.Join(tmp.DOFade(0f, FadeDuration));
        }

        private void OnDisable()
        {
            _barkQueue.Clear();
            _sequence.KillIfActive();
        }

        public void EnqueueBark(BarkType barkType, string text, bool calculateProbability = true)
        {
            if (_currentBarkType == barkType && _sequence is { active: true }) 
                return;

            foreach ((BarkType type, _) in _barkQueue)
                if (type == barkType)
                    return;

            if (calculateProbability == false || Random.NextDouble() <= BarkDefaultProbability)
            {
                gameObject.SetActive(true);
                _barkQueue.Add((barkType, text));
            }
        }

        public void SetSide(bool isLeftSide)
        {
            if (isLeftSide)
            {
                bubble.flipX = false;
                selfRect.anchoredPosition = leftPosition;
            }
            else
            {
                bubble.flipX = true;
                selfRect.anchoredPosition = rightPosition;
            }
        }
        
        private void OnTextTweenUpdate()
        {
            if (_allowTypeWriterSound == false)
                return;
            
            int currentTextLength = tmp.text.Length;
            if (currentTextLength != _previousTextLength)
            {
                typeWriterSource.PlayOneShot(typewriterClip, 0.5f);
                _previousTextLength = currentTextLength;
            }
        }

        public void Stop()
        {
            gameObject.SetActive(false);
        }
    }
}