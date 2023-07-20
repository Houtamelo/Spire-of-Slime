using Core.Combat.Scripts;
using DG.Tweening;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Core.Test
{
    public class AnnouncingTest : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text tmp;

        [SerializeField]
        private float loopMinDuration = 0.2f, loopMaxDuration = 0.5f;

        [SerializeField]
        private float shakeStrength = 1f;

        [SerializeField]
        private int vibrato = 10;

        [SerializeField]
        private float randomness = 90f;

        [SerializeField]
        private float threshold;

        private DOTweenTMPAnimator _tmpAnimator;
        [NotNull]
        private DOTweenTMPAnimator TmpAnimator => _tmpAnimator ??= new DOTweenTMPAnimator(tmp);

        [Button]
        private void DoColor()
        {
            Color color = Color.Lerp(Color.white, ColorReferences.Lust, threshold);
            float duration = Mathf.Lerp(loopMaxDuration, loopMinDuration, threshold);
            
            for (int i = 0; i < tmp.text.Length; i++)
                TmpAnimator.DOColorChar(i, color, duration).SetLoops(10, LoopType.Yoyo);
        }

        [Button]
        private void DoShake()
        {
            for (int i = 0; i < tmp.text.Length; i++)
                TmpAnimator.DOShakeCharOffset(i, loopMaxDuration * 10, shakeStrength * threshold, vibrato, randomness, fadeOut: false);
        }

        [Button]
        private void DoBoth()
        {
            DoColor();
            DoShake();
        }

        [Button]
        private void Test()
        {
            Sequence sequence = DOTween.Sequence();
            sequence.AppendCallback(() => Debug.Log("Start: " + Time.time));
            sequence.AppendInterval(1f);
            sequence.Insert(0.5f,DOVirtual.DelayedCall(1.5f, () => Debug.Log("Join: " + Time.time)));
            sequence.AppendCallback(() => Debug.Log("End: " + Time.time));
        }
    }
}