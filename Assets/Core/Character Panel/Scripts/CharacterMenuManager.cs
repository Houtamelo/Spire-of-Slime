using System;
using Core.Character_Panel.Scripts.Perks;
using Core.Character_Panel.Scripts.Positioning;
using Core.Character_Panel.Scripts.Skills;
using Core.Character_Panel.Scripts.Stats;
using Core.Main_Characters.Ethel.Combat;
using Core.Main_Characters.Nema.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using Core.Utils.Handlers;
using Core.Utils.Patterns;
using DG.Tweening;
using DG.Tweening.Plugins.Core.PathCore;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.UI;
using Save = Core.Save_Management.SaveObjects.Save;

namespace Core.Character_Panel.Scripts
{
    public sealed class CharacterMenuManager : Singleton<CharacterMenuManager>
    {
        private const float FadeDuration = 0.5f;
        private const float PortraitActiveAlpha = 0.588f;
        
        public readonly NullableHandler<IReadonlyCharacterStats> SelectedCharacter = new();
        
        [OdinSerialize, SceneObjectsOnly, Required]
        private readonly Toggle _ethelToggle, _nemaToggle;
        
        [OdinSerialize, SceneObjectsOnly, Required]
        private readonly RectTransform _ethelToggleRect, _nemaToggleRect;
        
        [OdinSerialize, Required]
        private Vector3[] _forwardWaypoints, _backwardWaypoints;
        
        [OdinSerialize, Required]
        private float _forwardScale, _backwardScale;
        
        [OdinSerialize]
        private float _tweenDuration;
        
        [OdinSerialize, SceneObjectsOnly, Required]
        private readonly Button _resumeButton;
        
        [OdinSerialize, SceneObjectsOnly, Required]
        private readonly GameObject _menuPanel;
        
        [SerializeField] 
        private GameObject togglesParent;

        [OdinSerialize, SceneObjectsOnly, Required]
        private readonly Toggle _statsToggle, _perksToggle, _skillsToggle, _orderToggle;
        
        [SerializeField, SceneObjectsOnly, Required]
        private StatsPanel statsPanel;

        [SerializeField, SceneObjectsOnly, Required]
        private PerksPanel perksPanel;

        [SerializeField, SceneObjectsOnly, Required]
        private SkillsPanel skillsPanel;

        [SerializeField, SceneObjectsOnly, Required]
        private PositioningPanel positioningPanel;

        [SerializeField, SceneObjectsOnly, Required]
        private Image ethelPortrait, nemaPortrait;

        private Sequence _ethelToggleSequence, _nemaToggleSequence;
        private bool _previousEthelToggleState = true, _previousNemaToggleState;

        private void Start()
        {
            _ethelToggle.onValueChanged.AddListener(EthelToggleValueChanged);
            _nemaToggle.onValueChanged.AddListener(NemaToggleValueChanged);
            _resumeButton.onClick.AddListener(Close);
            
            _statsToggle.onValueChanged.AddListener(statsPanel.SetOpen);
            _perksToggle.onValueChanged.AddListener(perksPanel.SetOpen);
            _skillsToggle.onValueChanged.AddListener(skillsPanel.SetOpen);
            _orderToggle.onValueChanged.AddListener(positioningPanel.SetOpen);
            
            SelectedCharacter.Changed += SelectedCharacterChanged;
            Save.Handler.Changed += OnSaveChanged;
            SelectedCharacterChanged(SelectedCharacter.AsOption());
            Close();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Save.Handler.Changed -= OnSaveChanged;
        }

        private void SelectedCharacterChanged(Option<IReadonlyCharacterStats> character)
        {
            togglesParent.SetActive(character.IsSome);
        }

        private void EthelToggleValueChanged(bool value)
        {
            if (Save.AssertInstance(out Save save) == false)
                return;
            
            if (value != _previousEthelToggleState)
                AnimateToggle(value, ref _ethelToggleSequence, _ethelToggleRect, ethelPortrait);

            _previousEthelToggleState = value;
            if (value && save.GetReadOnlyStats(Ethel.GlobalKey).AssertSome(out IReadonlyCharacterStats ethelStats))
                SelectedCharacter.AddValue(ethelStats, checkIfDefault: true);
        }

        private void AnimateToggle(bool value, ref Sequence sequence, RectTransform toggleRect, Image portrait)
        {
            sequence.KillIfActive();
            Path path;
            float scale;
            float endAlpha;
            if (value)
            {
                path = new Path(PathType.CubicBezier, _forwardWaypoints, subdivisionsXSegment: 5, Color.white);
                scale = _forwardScale;
                endAlpha = PortraitActiveAlpha;
            }
            else
            {
                path = new Path(PathType.CubicBezier, _backwardWaypoints, subdivisionsXSegment: 5, Color.white);
                scale = _backwardScale;
                endAlpha = 0;
            }

            sequence = DOTween.Sequence().SetUpdate(isIndependentUpdate: true);
            sequence.Append(toggleRect.DOLocalPath(path, _tweenDuration, PathMode.Sidescroller2D));
            sequence.Join(toggleRect.DOScale(scale, _tweenDuration));
            sequence.Join(portrait.DOFade(endAlpha, duration: FadeDuration));
        }

        private void NemaToggleValueChanged(bool value)
        {
            if (Save.AssertInstance(out Save save) == false)
                return;
            
            if (value != _previousNemaToggleState)
                AnimateToggle(value, ref _nemaToggleSequence, _nemaToggleRect, nemaPortrait);

            _previousNemaToggleState = value;
            if (value && save.GetReadOnlyStats(Nema.GlobalKey).AssertSome(out IReadonlyCharacterStats nemaStats))
                SelectedCharacter.AddValue(nemaStats, checkIfDefault: true);
        }

        private void OnSaveChanged([CanBeNull] Save save)
        {
            if (save == null)
                SelectedCharacter.ClearValue();
            else
                SelectFirst(save);
        }

        private void SelectFirst([NotNull] Save save)
        {
            ReadOnlySpan<IReadonlyCharacterStats> allStats = save.GetAllReadOnlyCharacterStats();
            foreach (IReadonlyCharacterStats stats in allStats)
            {
                if (stats.Key == Ethel.GlobalKey)
                {
                    if (_previousEthelToggleState == false)
                    {
                        AnimateToggle(value: true, ref _ethelToggleSequence, _ethelToggleRect, ethelPortrait);
                        _previousEthelToggleState = true;
                    }

                    SelectedCharacter.AddValue(stats, checkIfDefault: true);
                    return;
                }

                if (stats.Key == Nema.GlobalKey)
                {
                    if (_previousNemaToggleState == false)
                    {
                        AnimateToggle(value: true, ref _nemaToggleSequence, _nemaToggleRect, nemaPortrait);
                        _previousNemaToggleState = true;
                    }

                    SelectedCharacter.AddValue(stats, checkIfDefault: true);
                    return;
                }
            }

            SelectedCharacter.ClearValue();
        }

        private Option<float> _timeScaleBeforeOpening;

        public void Open()
        {
            if (_menuPanel.gameObject.activeSelf)
                return;
            
            Save save = Save.Current;
            if (save == null)
            {
                Debug.LogWarning("Trying to open character panel but no save is active.");
                return;
            }

            if (SelectedCharacter.IsNone)
                SelectFirst(save);

            _menuPanel.gameObject.SetActive(true);
            _timeScaleBeforeOpening = Option<float>.Some(Time.timeScale);
            Time.timeScale = 0f;
        }
        
        private void Close()
        {
            if (_menuPanel.gameObject.activeSelf == false)
                return;
            
            _menuPanel.SetActive(false);
            if (_timeScaleBeforeOpening.TrySome(out float scale))
                Time.timeScale = scale;
            
            _timeScaleBeforeOpening = Option<float>.None;
        }
    }
}