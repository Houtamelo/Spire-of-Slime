/*using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils.Extensions;

namespace Core.Audio.Scripts.MusicControllers
{
    public class OldMusicController : MusicController
    {
        private const float SlowFadeDuration = 0.5f;
        private const float FadeDuration = 0.5f;
        private const float QuickFadeDuration = 0.25f;

        [SerializeField, Required]
        private AudioSource localMapIntro, localMapMain, combat, combatLosing;

        private Tween _tween;

        public override void SetState(MusicState newState)
        {
            MusicState currentState = State;
            if (currentState == newState)
                return;
            
            if (currentState is MusicState.Idle)
                StopAll();

            State = newState;
            switch (currentState, newState)
            {
                case (MusicState.Idle, MusicState.LocalMap):
                    localMapIntro.volume = 1f;
                    localMapIntro.Play();
                    localMapMain.volume = 1f;
                    localMapMain.loop = true;
                    localMapMain.PlayScheduled(AudioSettings.dspTime + localMapIntro.clip.length + 0.01f); // for some reason they are overlapping so I add a small offset
                    _tween = DOVirtual.DelayedCall(localMapIntro.clip.length, localMapIntro.Stop, ignoreTimeScale: true);
                    break;
                case (MusicState.Idle, MusicState.Combat):
                    combat.volume = 0f;
                    combat.loop = true;
                    combat.Play();
                    combat.DOFade(endValue: 1f, FadeDuration);
                    break;
                case (MusicState.Idle, MusicState.CombatLosing):
                    combatLosing.volume = 0f;
                    combatLosing.loop = true;
                    combatLosing.Play();
                    combatLosing.DOFade(endValue: 1f, FadeDuration);
                    break;
                case (MusicState.LocalMap, MusicState.Idle):
                    _tween.CompleteIfActive();
                    if (localMapIntro.isPlaying && localMapIntro.time <= 0.0001f)
                        localMapIntro.Stop();
                    
                    if (localMapMain.isPlaying && localMapMain.time <= 0.0001f)
                        localMapMain.Stop();

                    if (localMapIntro.isPlaying)
                        _tween = localMapIntro.DOFade(endValue: 0f, FadeDuration).OnComplete(StopAll);
                    else if (localMapMain.isPlaying)
                        _tween = localMapMain.DOFade(endValue: 0f, FadeDuration).OnComplete(StopAll);
                    else
                        StopAll();

                    break;
                case (MusicState.LocalMap, MusicState.Combat):
                    _tween.CompleteIfActive();
                    localMapMain.loop = false;
                    combat.loop = true;
                    
                    if (localMapIntro.isPlaying && localMapIntro.time <= 0.0001f)
                        localMapIntro.Stop();
                    
                    if (localMapMain.isPlaying && localMapMain.time <= 0.0001f)
                        localMapMain.Stop();
                    
                    if (localMapIntro.isPlaying)
                    {
                        localMapMain.Stop();
                        combat.volume = 0f;
                        combat.Play();
                        Sequence sequence = DOTween.Sequence();
                        sequence.Append(combat.DOFade(endValue: 1f, QuickFadeDuration));
                        sequence.Join(localMapIntro.DOFade(endValue: 0f, QuickFadeDuration));
                        sequence.OnComplete(localMapIntro.Stop);
                        _tween = sequence;
                    }
                    else if (localMapMain.isPlaying)
                    {
                        localMapIntro.Stop();
                        combat.volume = 0f;
                        combat.Play();
                        Sequence sequence = DOTween.Sequence();
                        sequence.Append(combat.DOFade(endValue: 1f, QuickFadeDuration));
                        sequence.Join(localMapMain.DOFade(endValue: 0f, QuickFadeDuration));
                        sequence.OnComplete(localMapMain.Stop);
                        _tween = sequence;
                    }
                    else
                    {
                        localMapIntro.Stop();
                        localMapMain.Stop();
                        combat.volume = 0f;
                        combat.Play();
                        _tween = combat.DOFade(endValue: 1f, FadeDuration);
                    }

                    break;
                case (MusicState.LocalMap, MusicState.CombatLosing):
                    _tween.CompleteIfActive();
                    localMapMain.loop = false;
                    combatLosing.loop = true;
                    
                    if (localMapIntro.isPlaying && localMapIntro.time <= 0.0001f)
                        localMapIntro.Stop();
                    
                    if (localMapMain.isPlaying && localMapMain.time <= 0.0001f)
                        localMapMain.Stop();

                    if (localMapIntro.isPlaying)
                    {
                        localMapMain.Stop();
                        combatLosing.volume = 0f;
                        combatLosing.Play();
                        Sequence sequence = DOTween.Sequence();
                        sequence.Append(combatLosing.DOFade(endValue: 1f, QuickFadeDuration));
                        sequence.Join(localMapIntro.DOFade(endValue: 0f, QuickFadeDuration));
                        sequence.OnComplete(localMapIntro.Stop);
                        _tween = sequence;
                    }
                    else if (localMapMain.isPlaying)
                    {
                        localMapIntro.Stop();
                        combatLosing.volume = 0f;
                        combatLosing.Play();
                        Sequence sequence = DOTween.Sequence();
                        sequence.Append(combatLosing.DOFade(endValue: 1f, QuickFadeDuration));
                        sequence.Join(localMapMain.DOFade(endValue: 0f, QuickFadeDuration));
                        sequence.OnComplete(localMapMain.Stop);
                        _tween = sequence;
                    }
                    else
                    {
                        localMapIntro.Stop();
                        localMapMain.Stop();
                        combatLosing.volume = 0f;
                        combatLosing.Play();
                        _tween = combatLosing.DOFade(endValue: 1f, FadeDuration);
                    }

                    break;
                case (MusicState.Combat, MusicState.Idle):
                    _tween.CompleteIfActive();
                    combat.loop = false;
                    if (combat.isPlaying)
                    {
                        _tween = combat.DOFade(endValue: 0f, FadeDuration);
                        _tween.OnComplete(StopAll);
                    }
                    else
                    {
                        StopAll();
                    }

                    break;
                case (MusicState.Combat, MusicState.LocalMap):
                    _tween.CompleteIfActive();
                    combat.loop = false;
                    localMapMain.loop = true;

                    if (combat.isPlaying)
                    {
                        localMapMain.volume = 0f;
                        localMapMain.Play();
                        Sequence sequence = DOTween.Sequence();
                        sequence.Append(localMapMain.DOFade(endValue: 1f, QuickFadeDuration));
                        sequence.Join(combat.DOFade(endValue: 0f, QuickFadeDuration));
                        sequence.OnComplete(combat.Stop);
                        _tween = sequence;
                    }
                    else
                    {
                        combat.Stop();
                        localMapMain.volume = 0f;
                        localMapMain.Play();
                        _tween = localMapMain.DOFade(endValue: 1f, FadeDuration);
                    }

                    break;
                case (MusicState.Combat, MusicState.CombatLosing):
                    _tween.CompleteIfActive();
                    combat.loop = false;
                    combatLosing.loop = true;
                    if (combat.isPlaying)
                    {
                        combatLosing.volume = 0f;
                        combatLosing.Play();
                        Sequence sequence = DOTween.Sequence();
                        sequence.Append(combatLosing.DOFade(endValue: 1f, SlowFadeDuration));
                        sequence.Join(combat.DOFade(endValue: 0f, SlowFadeDuration));
                        sequence.onComplete += combat.Stop;
                        _tween = sequence;
                    }
                    else
                    {
                        combat.Stop();
                        combatLosing.volume = 0f;
                        combatLosing.Play();
                        _tween = combatLosing.DOFade(endValue: 1f, FadeDuration);
                    }

                    break;
                case (MusicState.CombatLosing, MusicState.Idle):
                    _tween.CompleteIfActive();
                    combatLosing.loop = false;
                    if (combatLosing.isPlaying)
                    {
                        _tween = combatLosing.DOFade(endValue: 0f, FadeDuration);
                        _tween.OnComplete(StopAll);
                    }
                    else
                    {
                        StopAll();
                    }

                    break;
                case (MusicState.CombatLosing, MusicState.LocalMap):
                    _tween.CompleteIfActive();
                    combatLosing.loop = false;
                    localMapMain.loop = true;
                    
                    if (combatLosing.isPlaying)
                    {
                        localMapMain.volume = 0f;
                        localMapMain.Play();
                        Sequence sequence = DOTween.Sequence();
                        sequence.Append(localMapMain.DOFade(endValue: 1f, QuickFadeDuration));
                        sequence.Join(combatLosing.DOFade(endValue: 0f, QuickFadeDuration));
                        sequence.OnComplete(combatLosing.Stop);
                        _tween = sequence;
                    }
                    else
                    {
                        combatLosing.Stop();
                        localMapMain.volume = 0f;
                        localMapMain.Play();
                        _tween = localMapMain.DOFade(endValue: 1f, FadeDuration);
                    }

                    break;
                case (MusicState.CombatLosing, MusicState.Combat):
                    _tween.CompleteIfActive();
                    combatLosing.loop = false;
                    combat.loop = true;
                    if (combatLosing.isPlaying)
                    {
                        combat.volume = 0f;
                        combat.Play();
                        Sequence sequence = DOTween.Sequence();
                        sequence.Append(combat.DOFade(endValue: 1f, SlowFadeDuration));
                        sequence.Join(combatLosing.DOFade(endValue: 0f, SlowFadeDuration));
                        sequence.onComplete += combatLosing.Stop;
                        _tween = sequence;
                    }
                    else
                    {
                        combatLosing.Stop();
                        combat.volume = 0f;
                        combat.Play();
                        _tween = combat.DOFade(endValue: 1f, FadeDuration);
                    }

                    break;
            }
        }

        private void StopAll()
        {
            _tween.CompleteIfActive();

            localMapIntro.Stop();
            localMapMain.Stop();
            combat.Stop();
            combatLosing.Stop();
            
            localMapIntro.volume = 0f;
            localMapMain.volume = 0f;
            combat.volume = 0f;
            combatLosing.volume = 0f;
        }
    }
}*/