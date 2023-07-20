using Core.Main_Database.Visual_Novel;
using Core.Utils.Extensions;
using Core.Utils.Patterns;
using DG.Tweening;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable Unity.InefficientPropertyAccess

namespace Core.Visual_Novel.Scripts
{
    public class PortraitDisplay : MonoBehaviour
    {
        private const float AnimationDuration = 0.5f;
        private static readonly Color ClearWhite = new(1, 1, 1, 0);

        [SerializeField, Required]
        private Image portraitOne, portraitTwo;

        [SerializeField]
        private Vector3 activePosition, backupPosition;

        [SerializeField]
        private bool isLeftSide;

        [SerializeField, Required]
        private AnimationCurve curve;

        [SerializeField, Required]
        private Color nonSpeakerColor = Color.gray;
        private Color ClearNonSpeakerColor => new(nonSpeakerColor.r, nonSpeakerColor.g, nonSpeakerColor.b, 0f);

        private Image _activePortrait, _inactivePortrait;
        private int _activeSiblingIndex, _inactiveSiblingIndex;
        private Sequence _sequence;
        private TweenCallback _hidePortraitOne, _hidePortraitTwo;
        private bool _awaken;

        private void Awake()
        {
            if (_awaken)
                return;
            
            _awaken = true;
            _activePortrait = portraitOne;
            _inactivePortrait = portraitTwo;
            _activeSiblingIndex = _activePortrait.transform.GetSiblingIndex();
            _inactiveSiblingIndex = _inactivePortrait.transform.GetSiblingIndex();
            _hidePortraitOne = HidePortraitOne;
            _hidePortraitTwo = HidePortraitTwo;
        }

        private void OnDestroy()
        {
            _sequence.KillIfActive();
        }

        public void SetPortrait(string fileName, bool isSpeaker)
        {
            Awake();
            
            if (_sequence is { active: true })
            {
                _sequence.Kill();
                _inactivePortrait = portraitTwo;
                HidePortraitTwo();
                
                _activePortrait = portraitOne;
                if (fileName.IsSome() && PortraitDatabase.GetPortrait(fileName).AssertSome(out (Sprite portrait, bool isLeftSide) portraitInfo))
                {
                    _activePortrait.sprite = portraitInfo.portrait;
                    _activePortrait.color = isSpeaker ? Color.white : nonSpeakerColor;
                    CheckIfShouldFlip(_activePortrait.transform, portraitInfo.isLeftSide);
                }
                else
                {
                    _activePortrait.sprite = null;
                    _activePortrait.SetAlpha(0f);
                }
                
                return;
            }
            
            bool currentPortraitExists = _activePortrait.sprite != null && _activePortrait.color.a > 0f;
            Option<(Sprite sprite, bool isLeftSide)> newPortrait = fileName.IsSome() ? PortraitDatabase.GetPortrait(fileName) : Option.None;
            bool isSameCharacter = false;

            if (currentPortraitExists && newPortrait.IsSome)
            {
                string currentName = _activePortrait.sprite.name;
                int currentIndex = currentName.IndexOf('_');
                int fileIndex = fileName!.IndexOf('_');
                if (currentIndex != -1 && fileIndex != -1)
                {
                    string currentCharacterName = currentName[..currentIndex];
                    string newCharacterName = fileName[..fileIndex];
                    isSameCharacter = currentCharacterName == newCharacterName;
                }
            }
            
            if (newPortrait.IsSome && currentPortraitExists && newPortrait.Value.sprite == _activePortrait.sprite)
            {
                _activePortrait.color = isSpeaker ? Color.white : nonSpeakerColor;
                return;
            }

            bool isOneActive = _activePortrait == portraitOne;
            if (isSameCharacter)
            {
                _inactivePortrait.transform.position = activePosition;
                _inactivePortrait.color = isSpeaker ? ClearWhite : ClearNonSpeakerColor;
                _inactivePortrait.sprite = newPortrait.Value.sprite;
                _inactivePortrait.transform.SetSiblingIndex(_activeSiblingIndex);
                CheckIfShouldFlip(_inactivePortrait.transform, newPortrait.Value.isLeftSide);

                _activePortrait.transform.position = activePosition;
                _activePortrait.color = isSpeaker ? Color.white : nonSpeakerColor;
                _activePortrait.transform.SetSiblingIndex(_inactiveSiblingIndex);

                _sequence = DOTween.Sequence().SetEase(curve);
                _sequence.Append(_inactivePortrait.DOFade(endValue: 1f, AnimationDuration / 4f));
                _sequence.Join(_activePortrait.DOFade(endValue: 0f, AnimationDuration / 4f));
                _sequence.onComplete += isOneActive ? _hidePortraitOne : _hidePortraitTwo;

                (_activePortrait, _inactivePortrait) = (_inactivePortrait, _activePortrait);
                return;
            }

            switch (currentPortraitExists, newPortrait.IsSome)
            {
                case (true, true):
                    _inactivePortrait.transform.position = backupPosition;
                    _inactivePortrait.color = isSpeaker ? ClearWhite : ClearNonSpeakerColor;
                    _inactivePortrait.sprite = newPortrait.Value.sprite;
                    _inactivePortrait.transform.SetSiblingIndex(_activeSiblingIndex);
                    CheckIfShouldFlip(_inactivePortrait.transform, newPortrait.Value.isLeftSide);

                    _activePortrait.transform.position = activePosition;
                    _activePortrait.color = Color.white;
                    _activePortrait.transform.SetSiblingIndex(_inactiveSiblingIndex);

                    _sequence = DOTween.Sequence().SetEase(Ease.OutQuad);
                    _sequence.Append(_activePortrait.DOFade(endValue: 0f, AnimationDuration));
                    _sequence.Join(_activePortrait.transform.DOMove(backupPosition, AnimationDuration));
                    _sequence.Join(_inactivePortrait.DOFade(endValue: 1f, AnimationDuration));
                    _sequence.Join(_inactivePortrait.transform.DOMove(activePosition, AnimationDuration));
                    
                    _sequence.onComplete += isOneActive ? _hidePortraitOne : _hidePortraitTwo;

                    (_activePortrait, _inactivePortrait) = (_inactivePortrait, _activePortrait);
                    break;
                case (true, false):
                    _sequence = DOTween.Sequence().SetEase(Ease.OutQuad);
                    _sequence.Append(_activePortrait.DOFade(endValue: 0f, AnimationDuration));
                    _sequence.Join(_activePortrait.transform.DOMove(backupPosition, AnimationDuration));
                    
                    _sequence.onComplete += isOneActive ? _hidePortraitOne : _hidePortraitTwo;

                    HidePortrait(_inactivePortrait);
                    break;
                case (false, true):
                    _activePortrait.transform.position = backupPosition;
                    _activePortrait.color = isSpeaker ? ClearWhite : ClearNonSpeakerColor;
                    _activePortrait.sprite = newPortrait.Value.sprite;
                    _activePortrait.transform.SetSiblingIndex(_activeSiblingIndex);
                    CheckIfShouldFlip(_activePortrait.transform, newPortrait.Value.isLeftSide);

                    _sequence = DOTween.Sequence().SetEase(Ease.OutQuad);
                    _sequence.Append(_activePortrait.DOFade(endValue: 1f, AnimationDuration));
                    _sequence.Join(_activePortrait.transform.DOMove(activePosition, AnimationDuration));

                    HidePortrait(_inactivePortrait);
                    break;
                case (false, false):
                    HidePortraitOne();
                    HidePortraitTwo();
                    break;
            }
        }

        private void CheckIfShouldFlip([NotNull] Transform portraitTransform, bool isNewPortraitLeftSide)
        {
            portraitTransform.eulerAngles = isLeftSide == isNewPortraitLeftSide ? Vector3.zero : new Vector3(0, 180, 0);
        }

        private void HidePortrait([NotNull] Image portrait)
        {
            portrait.transform.position = backupPosition;
            portrait.color = ClearWhite;
            portrait.sprite = null;
        }

        private void HidePortraitOne() => HidePortrait(portraitOne);
        private void HidePortraitTwo() => HidePortrait(portraitTwo);

        public void HideAll()
        {
            HidePortraitOne();
            HidePortraitTwo();
        }

        public void FinishAnimation()
        {
            _sequence.CompleteIfActive();
        }
    }
}