using Core.Combat.Scripts.Skills.Action;
using Core.Utils.Extensions;
using Core.Utils.Patterns;
using DG.Tweening;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Core.Combat.Scripts.Managers
{
    public class Announcer : MonoBehaviour
    {
        private const float FadeDurationBase = 0.25f;
        public static float FadeDuration => FadeDurationBase * IActionSequence.DurationMultiplier;
        
        private static readonly int FadeInId = Animator.StringToHash("FadeIn");
        private static readonly int FadeOutId = Animator.StringToHash("FadeOut");

        [SerializeField, Required]
        private TMP_Text tmp;

        [SerializeField, Required]
        private Animator animator;
        public Animator Animator => animator;

        public TweenCallback FadeInCallback { get; private set; }
        public TweenCallback FadeOutCallback { get; private set; }
        public bool IsBusy => _animationSequence is { active: true };

        private Sequence _animationSequence;
        public string TextToSet { get; set; }
        public DOTweenTMPAnimator DoTweenTMPAnimator { get; private set; }
        
        private void Start()
        {
            FadeInCallback = () =>
            {
                tmp.text = TextToSet;
                animator.SetTrigger(FadeInId);
            };

            FadeOutCallback = () => animator.SetTrigger(FadeOutId);
            DoTweenTMPAnimator = new DOTweenTMPAnimator(tmp);
        }

        private void OnDestroy()
        {
            _animationSequence.KillIfActive();
        }

        public void Announce(string text, Option<float> delayBeforeStart, float totalDuration, float speed)
        {
            _animationSequence.KillIfActive();
            _animationSequence = DefaultAnnounce(announcer: this, text, delayBeforeStart, totalDuration, speed);
        }

        public static Sequence DefaultAnnounce([NotNull] Announcer announcer, string text, Option<float> delayBeforeStart, float totalDuration, float speed)
        {
            announcer.animator.speed = speed;
            totalDuration -= FadeDuration;
            announcer.TextToSet = text;
            Sequence sequence = DOTween.Sequence();
            if (delayBeforeStart.IsSome)
                sequence.AppendInterval(delayBeforeStart.Value);

            sequence.AppendCallback(announcer.FadeInCallback);
            sequence.AppendInterval(totalDuration);
            sequence.AppendCallback(announcer.FadeOutCallback);
            return sequence;
        }

        public void Announce([NotNull] PlannedSkill plan, float startDuration, float popDuration, float speed)
        {
            _animationSequence.KillIfActive();
            _animationSequence = plan.Skill.Announce(announcer: this, plan, startDuration, popDuration, speed);
        }
    }
}