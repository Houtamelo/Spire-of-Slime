using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Core.Main_Menu.Scripts
{
    public sealed class CreditsPanel : MonoBehaviour, IScrollHandler
    {
        [SerializeField] private TMP_Text creditsText;
        [SerializeField] private TextAsset creditsTextAsset;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private float speed = 0.05f;
        private Tween _tween;

        private void Awake()
        {
            creditsText.text = creditsTextAsset.text;
        }

        private IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(1);

            if (_tween is not { active: true })
                _tween = scrollRect.DOVerticalNormalizedPos(endValue: 0, duration: speed).SetSpeedBased();
        }

        private void OnEnable()
        {
            scrollRect.verticalNormalizedPosition = 1;
            StartCoroutine(Start());
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (_tween is { active: true })
                _tween.Kill();
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (_tween is { active: true })
                _tween.Kill();
        }

#if UNITY_EDITOR

        private void OnValidate()
        {
            if (scrollRect == null)
            {
                scrollRect = GetComponent<ScrollRect>();
                if (scrollRect == null)
                    Debug.LogError("ScrollRect is null");
                else
                    UnityEditor.EditorUtility.SetDirty(target: this);
            }
        }
#endif
    }
}