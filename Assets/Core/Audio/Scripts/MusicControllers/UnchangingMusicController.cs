using Core.Utils.Extensions;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Audio.Scripts.MusicControllers
{
    public class UnchangingMusicController : MusicController
    {
        [SerializeField, Required]
        private AudioSource source;

        private Tween _tween;
        
        public override void SetState(MusicEvent newState) { }

        public override void FadeDownAndDestroy(float duration)
        {
            _tween.KillIfActive();
            if (source.volume <= 0f)
            {
                Destroy(this.gameObject);
                return;
            }

            GameObject self = this.gameObject;
            _tween = source.DOFade(endValue: 0f, duration).SetUpdate(isIndependentUpdate: true).OnComplete(() =>
            {
                if (self != null)
                    Destroy(self);
            });
        }

        public override void SetVolume(float volume)
        {
            _tween.KillIfActive();
            _tween = source.DOFade(endValue: volume, duration: 3f * Mathf.Abs(volume - source.volume)).SetUpdate(isIndependentUpdate: true);
        }

        private void OnDestroy()
        {
            _tween.KillIfActive();
        }
    }
}