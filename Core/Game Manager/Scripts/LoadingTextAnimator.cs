using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Core.Game_Manager.Scripts
{
    public sealed class LoadingTextAnimator : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text text;

        private Sequence _sequence;
        private void Awake()
        {
            text.DOColor(new Color(0.5f, 0.5f, 0.5f, 1f), 1.5f).SetLoops(-1, LoopType.Yoyo);
            Sequence sequence = DOTween.Sequence();
            
            sequence.AppendCallback(Loading1);
            sequence.AppendInterval(1f);
            sequence.AppendCallback(Loading2);
            sequence.AppendInterval(1f);
            sequence.AppendCallback(Loading3);
            sequence.AppendInterval(1f);
            sequence.SetLoops(-1, LoopType.Restart);
            sequence.SetTarget(text);
        }

        private void OnEnable()
        {
            if (_sequence is { active: true })
                _sequence.Restart();
        }

        private void Loading1() => text.text = "Loading.";
        private void Loading2() => text.text = "Loading..";
        private void Loading3() => text.text = "Loading...";
        private void OnDestroy() => text.DOKill();
        private void Reset() => text = GetComponent<TMP_Text>();
    }
}