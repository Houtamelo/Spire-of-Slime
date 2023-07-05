using System;
using Core.Combat.Scripts.Managers;
using Core.Localization.Scripts;
using Core.Main_Characters.Nema.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using Core.Utils.Patterns;
using Data.Main_Characters.Ethel;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils.Patterns;
using Save = Core.Save_Management.SaveObjects.Save;
using Threshold = Core.Save_Management.SaveObjects.Corruption.Threshold;

namespace Core.Combat.Scripts.Skills.Action.Overlay
{
    public class DesireThresholdOverlayAnimator : OverlayAnimator
    {
        private static float StayDuration => IActionSequence.AnimationDuration;
        
        [SerializeField, Required]
        private SpriteRenderer spriteRenderer;

        [SerializeField, Required]
        private Sprite ethelVeryLowSprite, ethelLowSprite, ethelMediumSprite, ethelHighSprite, ethelMaxSprite;
        
        [SerializeField, Required]
        private Sprite nemaVeryLowSprite, nemaLowSprite, nemaMediumSprite, nemaHighSprite, nemaMaxSprite;
        
        [SerializeField]
        private LocalizedText veryLowText, lowText, mediumText, highText, maxText;
        
        [SerializeField]
        private float loopMinDuration = 0.3f, loopMaxDuration = 0.6f;

        [SerializeField]
        private float shakeStrength = 2f;

        [SerializeField]
        private int vibrato = 10;

        [SerializeField]
        private float randomness = 90f;

        private Tween _tween;
        private void OnDestroy() => _tween.KillIfActive();

        public override Sequence Announce(Announcer announcer, PlannedSkill plan, float startDuration, float popDuration, float speed)
        {
            CleanString targetKey = plan.Target.Script.Key;
            if (Save.AssertInstance(out Save save) == false || save.GetReadOnlyStats(targetKey).AssertSome(out IReadonlyCharacterStats stats) == false)
                return Announcer.DefaultAnnounce(announcer, plan.Skill.DisplayName, delayBeforeStart: Option.None, startDuration, speed);
            
            Character target = targetKey switch
            {
                _ when targetKey == Ethel.GlobalKey => Character.Ethel,
                _ when targetKey == Nema.GlobalKey  => Character.Nema,
                _                                   => Character.Unknown
            };

            if (target is Character.Unknown)
            {
                Debug.LogWarning($"Trying to animate tempt skill on unsupported character, key: {targetKey}");
                return Announcer.DefaultAnnounce(announcer, plan.Skill.DisplayName, delayBeforeStart: Option.None, startDuration, speed);
            }

            announcer.Animator.speed = speed;
            startDuration -= Announcer.FadeDuration;
            announcer.TextToSet = "?";
            
            Threshold threshold = Corruption.DesireThreshold(stats, plan.Caster.Script.Race);
            DOTweenTMPAnimator tmpAnimator = announcer.DoTweenTMPAnimator;
            
            Sequence sequence = DOTween.Sequence();

            sequence.AppendCallback(announcer.FadeInCallback);
            sequence.AppendInterval(startDuration);
            
            string displayName = ThresholdToText(threshold);
            sequence.AppendCallback(() => tmpAnimator.target.text = displayName);

            if (threshold != Threshold.VeryLow)
            {
                float thresholdStrength = threshold switch
                {
                    Threshold.Low    => 0.25f,
                    Threshold.Medium => 0.5f,
                    Threshold.High   => 0.75f,
                    Threshold.Max    => 1f,
                    _                => throw new ArgumentOutOfRangeException(nameof(threshold), threshold, message: null)
                };

                for (int i = 0; i < displayName.Length; i++)
                    sequence.Join(tmpAnimator.DOShakeCharOffset(i, StayDuration, shakeStrength * thresholdStrength, vibrato, randomness, fadeOut: false));
                
                Color baseColor = tmpAnimator.target.color;
                baseColor.a = 1f;
                Color targetColor = Color.Lerp(baseColor, ColorReferences.Lust, thresholdStrength);
                
                float loopDuration = Mathf.Lerp(loopMaxDuration, loopMinDuration, thresholdStrength);
                int loopCount = Mathf.FloorToInt(StayDuration / loopDuration);
                if (loopCount % 2 == 1)
                    loopCount--;
            
                for (int i = 0; i < displayName.Length; i++)
                    sequence.Join(tmpAnimator.DOColorChar(i, targetColor, loopDuration).SetLoops(loopCount, LoopType.Yoyo));
            }

            sequence.AppendCallback(announcer.FadeOutCallback);
            return sequence;
        }

        private string ThresholdToText(Threshold threshold)
        {
            LocalizedText localizedText = threshold switch
            {
                Threshold.VeryLow => veryLowText,
                Threshold.Low     => lowText,
                Threshold.Medium  => mediumText,
                Threshold.High    => highText,
                Threshold.Max     => maxText,
                _                 => throw new ArgumentOutOfRangeException(nameof(threshold), threshold, message: null)
            };
            
            return localizedText.Translate().GetText();
        }

        public override void FadeUp(float duration, PlannedSkill plan)
        {
            _tween.KillIfActive();
            CleanString targetKey = plan.Target.Script.Key;
            if (Save.AssertInstance(out Save save) == false || save.GetReadOnlyStats(targetKey).AssertSome(out IReadonlyCharacterStats stats) == false)
                return;

            Character target = targetKey switch
            {
                _ when targetKey == Ethel.GlobalKey => Character.Ethel,
                _ when targetKey == Nema.GlobalKey  => Character.Nema,
                _                                   => Character.Unknown
            };

            if (target is Character.Unknown)
            {
                Debug.LogWarning($"Trying to animate tempt skill on unsupported character, key: {targetKey}");
                return;
            }

            Sprite sprite = (Corruption.DesireThreshold(stats, plan.Caster.Script.Race), target) switch
            {
                (Threshold.VeryLow, Character.Ethel) => ethelVeryLowSprite,
                (Threshold.VeryLow, Character.Nema)  => nemaVeryLowSprite,
                (Threshold.Low, Character.Ethel)     => ethelLowSprite,
                (Threshold.Low, Character.Nema)      => nemaLowSprite,
                (Threshold.Medium, Character.Ethel)  => ethelMediumSprite,
                (Threshold.Medium, Character.Nema)   => nemaMediumSprite,
                (Threshold.High, Character.Ethel)    => ethelHighSprite,
                (Threshold.High, Character.Nema)     => nemaHighSprite,
                (Threshold.Max, Character.Ethel)     => ethelMaxSprite,
                (Threshold.Max, Character.Nema)      => nemaMaxSprite,
                _                                    => throw new ArgumentOutOfRangeException(nameof(target), target, null)
            };
            
            spriteRenderer.sprite = sprite;
            spriteRenderer.SetAlpha(0f);
            _tween = spriteRenderer.DOFade(endValue: 1f, duration);
        }

        private void Reset()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public override void FadeDown(float duration)
        {
            _tween.KillIfActive();
            _tween = spriteRenderer.DOFade(endValue: 0f, duration);
        }
        
        private enum Character
        {
            Ethel,
            Nema,
            Unknown
        }
    }
}