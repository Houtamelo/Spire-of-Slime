using System;
using System.Collections.Generic;
using Core.Combat.Scripts.Animations;
using Core.Combat.Scripts.Behaviour.Modules;
using Core.Combat.Scripts.Behaviour.Rendering;
using Core.Combat.Scripts.Behaviour.UI;
using Core.Combat.Scripts.Effects;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Effects.Types.Grappled;
using Core.Combat.Scripts.Effects.Types.Riposte;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Interfaces;
using Core.Combat.Scripts.Managers;
using Core.Combat.Scripts.Skills.Action;
using Core.Utils.Extensions;
using Core.Utils.Math;
using Core.Utils.Patterns;
using DG.Tweening;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Core.Combat.Scripts.Behaviour
{
    /// <summary> This class serves as a middle man between a CharacterStateMachine and all rendering related to it. </summary>
    public class DisplayModule : MonoBehaviour, IEquatable<DisplayModule>
    {
        [SerializeField, Required]
        private CharacterInputHandler inputHandler;
        
        [SerializeField, Required]
        private BarkPlayer barkPlayer;
        public BarkPlayer BarkPlayer => barkPlayer;

        [SerializeField]
        public bool hasWire;

        public bool IsMouseOver() => inputHandler.IsMouseOver;
        
        public Option<CharacterStateMachine> StateMachine { get; private set; }
        public CombatManager CombatManager { get; private set; }
        
        private AnimationStatus _animationStatus = AnimationStatus.Common;
        public AnimationStatus AnimationStatus
        {
            get => _animationStatus;
            private set
            {
                if (hasWire)
                    Debug.Log($"Set to {_animationStatus} to {value}");
                
                _animationStatus = value;
            }
        }
        
        private void OnDestroy()
        {
            _positionTween.KillIfActive();
            _uiBarsCanvasGroupTween.KillIfActive();
            transform.DOKill();
            this.DOKill();
            if (StateMachine.IsSome)
                StateMachine.Value.DisplayDestroyed();
        }

        public void SetCombatManager([NotNull] CombatManager manager)
        {
            CombatManager = manager;
            indicators.SubscribeToCombatManager(manager);
            inputHandler.AssignCombatManager(manager);
            barkPlayer.AssignCombatManager(manager);
        }

    #region Renderer
        private Option<ICharacterRenderer> _renderer;
        public Option<Transform> AnimatorTransform => _renderer.IsSome ? _renderer.Value.GetTransform() : Option<Transform>.None;

        [SerializeField]
        private AnimationCurve moveToDefaultPositionAnimationCurve;

        public Option<Bounds> GetBounds() => _renderer.IsSome ? Option<Bounds>.Some(_renderer.Value.GetBounds()) : Option<Bounds>.None;

        public Option<Vector3> GetCuePosition()
        {
            if (AnimationStatus is AnimationStatus.Grappled)
            {
                if (StateMachine.TrySome(out CharacterStateMachine owner) == false)
                    return Option<Vector3>.None;

                foreach (StatusInstance status in owner.StatusReceiverModule.GetAll)
                {
                    if (status is LustGrappled lustGrappled && lustGrappled.Restrainer.Display.TrySome(out DisplayModule restrainerDisplay))
                        return restrainerDisplay.GetCuePosition();
                }

                Debug.LogWarning("Grappled but no restrainer found", context: this);
                return Option<Vector3>.None;
            }
            
            if (GetBounds().TrySome(out Bounds bounds) == false)
                return Option<Vector3>.None;

            Vector3 pos = bounds.center;
            pos.x = transform.position.x;
            pos.z = 0;
            
            float extentsY = Mathf.Min(bounds.extents.y, 3.6f);
            pos.y += extentsY;

            return Option<Vector3>.Some(pos);
        }
        
        public void AllowIdleAnimationTimeUpdateExternally(bool value)
        {
            if (_renderer.TrySome(out ICharacterRenderer characterRenderer))
                characterRenderer.AllowIdleAnimationTimeUpdate(value);
        }

        private void AllowRendering(bool value)
        {
            if (_renderer.TrySome(out ICharacterRenderer characterRenderer))
                characterRenderer.AllowRendering(value);
        }
        
        public void AllowShadowsExternally(bool value)
        {
            if (_renderer.TrySome(out ICharacterRenderer characterRenderer))
                characterRenderer.AllowShadows(value);
        }

        private void SetIdleSpeed(float value)
        {
            if (_renderer.TrySome(out ICharacterRenderer characterRenderer))
                characterRenderer.SetIdleSpeed(value);
        }

        public void SetBaseSpeed(float speedMultiplier)
        {
            if (_renderer.TrySome(out ICharacterRenderer characterRenderer))
                characterRenderer.SetBaseSpeed(speedMultiplier);
        }
        
        private Tween _positionTween;
        public bool IsBusy => gameObject.activeSelf && (barkPlayer.IsBusy || _positionTween is { active: true } || (_renderer.TrySome(out ICharacterRenderer selfRenderer) && selfRenderer.FadeTween is { active: true }));

        public void MoveToPosition(Vector3 worldPosition, Option<float> baseDuration)
        {
            if (StateMachine.IsNone || AnimationStatus is AnimationStatus.Defeated)
                return;
            
            _positionTween.KillIfActive();
            int index = CombatManager.Characters.IndexOf(StateMachine.Value);
            int sortingOrder = index % 2 == 0 ? 5 - (index * 2) : -index * 2;
            SetSortingOrder(sortingOrder);

            // CharacterStateMachine stateMachine = StateMachine.Value;
            // if (stateMachine.StateEvaluator.PureEvaluate() is CharacterState.Grappled or CharacterState.Defeated || CombatManager == null)
            //     return;
            
            Vector3 currentPos = transform.position;
            if (baseDuration.TrySome(out float duration) == false)
            {
                transform.position = worldPosition;
                return;
            }

            float distance = Vector3.Distance(currentPos, worldPosition);
            float referenceDistance = CombatManager.CombatSetupInfo.PaddingSettings.InBetween;
            duration *= distance / referenceDistance;
            if (duration <= 0f)
            {
                transform.position = worldPosition;
                return;
            }

            _positionTween = transform.DOMove(endValue: worldPosition, UnityEngine.Random.Range(minInclusive: duration * 0.9f, maxInclusive: duration * 1.1f)).SetEase(moveToDefaultPositionAnimationCurve);
        }

        public void MoveToDefaultPosition(Option<float> baseDuration)
        {
            if (StateMachine.IsNone || AnimationStatus is AnimationStatus.Defeated)
                return;
            
            Vector3 targetPos = CombatManager.PositionManager.GetCharacterDefaultWorldPosition(StateMachine.Value);
            MoveToPosition(targetPos, baseDuration);
        }

        public void SetSortingOrder(int value)
        {
            if (_renderer.TrySome(out ICharacterRenderer characterRenderer))
                characterRenderer.SetSortingOrder(value);
        }
        
        public void SetAnimationWithoutNotifyStatus(in CombatAnimation combatAnimation)
        {
            if (_renderer.TrySome(out ICharacterRenderer characterRenderer))
                characterRenderer.SetAnimation(combatAnimation);
        }
        
        public Option<Tween> FadeRenderer(float endValue, float duration)
        {
            if (_renderer.TrySome(out ICharacterRenderer characterRenderer))
                return Option<Tween>.Some(characterRenderer.Fade(endValue, duration));

            return Option<Tween>.None;
        }

    #endregion

    #region Indicators
        [SerializeField, Required]
        private Indicators indicators;

        private bool _areIndicatorsAllowedInternally = true;
        private bool _areIndicatorsAllowedExternally = true;

        private void AllowIndicatorsInternally(bool value)
        {
            _areIndicatorsAllowedInternally = value;
            indicators.Allow(_areIndicatorsAllowedInternally && _areIndicatorsAllowedExternally);
        }

        public void AllowIndicatorsExternally(bool value)
        {
            _areIndicatorsAllowedExternally = value;
            indicators.Allow(_areIndicatorsAllowedInternally && _areIndicatorsAllowedExternally);
        }

        public void CheckIndicators()
        {
            if (_areIndicatorsAllowedInternally && _areIndicatorsAllowedExternally)
                indicators.CheckForChanges();
        }

        public void AnimateIndicatorsForAction()
        {
            indicators.AnimateForAction();
        }
    #endregion

    #region StatusIcons
        [SerializeField, Required]
        private StatusEffectIcon statusIconPrefab;

        [SerializeField, Required]
        private Transform statusIconsParent;

        [SerializeField, Required]
        private GameObject statusTooltip;

        [SerializeField, Required]
        private TMP_Text statusTooltipText;
        
        private readonly Dictionary<EffectType, StatusEffectIcon> _spawnedStatusIcons = new();
        
        public void CreateIconForStatus([NotNull] StatusInstance statusInstance)
        {
            CharacterStateMachine owner = statusInstance.Owner;
            if (_spawnedStatusIcons.TryGetValue(statusInstance.EffectType, out StatusEffectIcon icon) == false)
            {
                icon = Instantiate(statusIconPrefab, statusIconsParent);
                icon.AssignCharacter(owner, display: this);
                _spawnedStatusIcons.Add(statusInstance.EffectType, icon);
            }

            Option<EffectType> iconEffectType = icon.GetEffectType();
            if (iconEffectType.TrySome(out EffectType iconType) && iconType != statusInstance.EffectType)
            {
                Debug.LogWarning($"Icon of type:{iconType} was assigned to wrong key:{statusInstance.EffectType} on dictionary, silently fixed.");
                ClearStatusIconAndReassign();
            }
            else
            {
                icon.AddStatus(statusInstance);
            }
        }

        private void ClearStatusIconAndReassign()
        {
            foreach (StatusEffectIcon icon in _spawnedStatusIcons.Values)
                icon.ClearStatuses();
            
            if (StateMachine.IsNone)
                return;
            
            foreach (StatusInstance status in StateMachine.Value.StatusReceiverModule.GetAll)
            {
                if (status.IsDeactivated)
                    continue;
                
                if (_spawnedStatusIcons.TryGetValue(status.EffectType, out StatusEffectIcon icon) == false)
                {
                    icon = Instantiate(statusIconPrefab, statusIconsParent);
                    icon.AssignCharacter(StateMachine.Value, display: this);
                    _spawnedStatusIcons.Add(status.EffectType, icon);
                }
                    
                icon.AddStatus(status);
            }
        }

        public void ShowStatusTooltip(string description)
        {
            statusTooltipText.text = description;
            statusTooltip.SetActive(true);
        }

        public void HideStatusTooltip()
        {
            statusTooltipText.text = string.Empty;
            statusTooltip.SetActive(false);
        }

        public void StatusIconRemoved([NotNull] StatusInstance effectInstance)
        {
            if (_spawnedStatusIcons.TryGetValue(effectInstance.EffectType, out StatusEffectIcon icon))
                icon.RemoveStatus(effectInstance);
        }
    #endregion
        
    #region Bars
        private const float BarsFillLerpBaseDuration = 1f;
        public static float BarsFillLerpDuration => BarsFillLerpBaseDuration * IActionSequence.DurationMultiplier;

        [SerializeField, Required]
        private CanvasGroup uiBarsCanvasGroup;
        
        [SerializeField, Required]
        private StaminaBar staminaBar;
        public void SetStaminaBar(bool active, int current, int max) => staminaBar.Set(active, current, max);

        [SerializeField, Required]
        private LustBar lustBar;
        public void SetLustBar(bool active, int lust) => lustBar.Set(active, lust);

        [SerializeField, Required]
        private TemptationBar temptationBar;
        public void SetTemptationBar(bool active, int temptation) => temptationBar.Set(active, temptation);

        [SerializeField, Required]
        private ActionBars actionBars;

        private bool _areBarsAllowed = true;
        private Tween _uiBarsCanvasGroupTween;

        public void SetRecoveryBar(bool active, TSpan remaining, TSpan total) => actionBars.SetRecovery(active, remaining, total, CombatManager);
        
        public void SetDownedBar(bool active, TSpan remaining, TSpan total) => actionBars.SetDowned(active, remaining, total, CombatManager);
        
        public void SetChargeBar(bool active, TSpan remaining, TSpan total) => actionBars.SetCharge(active, remaining, total, CombatManager);
        
        public void SetStunBar(bool active, TSpan remaining, TSpan total) => actionBars.SetStun(active, remaining, total, CombatManager);
        
        private void AllowBarsInternally(bool value)
        {
            if (value == false)
                _uiBarsCanvasGroupTween.CompleteIfActive();

            _areBarsAllowed = value;
            uiBarsCanvasGroup.gameObject.SetActive(_areBarsAllowed);
        }

        public void FadeBars(float endValue, float duration)
        {
            _uiBarsCanvasGroupTween.KillIfActive();
            if (_areBarsAllowed)
                _uiBarsCanvasGroupTween = uiBarsCanvasGroup.DOFade(endValue, duration);
            else
                uiBarsCanvasGroup.alpha = endValue;
        }
        
        public void SetBarsAlpha(float value) => uiBarsCanvasGroup.alpha = value;
    #endregion

    #region Prediction Icon
        [SerializeField, Required]
        private PredictionIconsDisplay predictionIconsDisplay;

        public void UpdatePredictionIcon(Option<PlannedSkill> plan) => predictionIconsDisplay.SetPlan(plan);
    #endregion

        public void UpdateSide(bool isLeftSide)
        {
            barkPlayer.SetSide(isLeftSide);
            if (_renderer.IsNone || _renderer.Value.GetTransform().TrySome(out Transform rendererTransform) == false)
                return;
            
            Vector3 rotation = rendererTransform.localEulerAngles;
            rotation.y = isLeftSide ? 0 : 180;
            rendererTransform.localEulerAngles = rotation;
        }

        public void SetStateMachine([CanBeNull] CharacterStateMachine stateMachine)
        {
            if (stateMachine == null)
            {
                StateMachine = Option<CharacterStateMachine>.None;
                gameObject.SetActive(false);
                Debug.LogWarning($"CharacterDisplay.SetStateMachine() was called with null stateMachine, disabling display.");
                return;
            }
            
            gameObject.SetActive(true);
            StateMachine = Option<CharacterStateMachine>.Some(stateMachine);
            ICharacterScript script = stateMachine.Script;
            
            name = script.CharacterName.Translate().GetText();

            if (_renderer.IsSome && _renderer.Value != null)
                _renderer.Value.DestroySelf();
            
            GameObject rendererObj = script.RendererPrefab.InstantiateWithFixedLocalScaleAndAnchoredPosition(transform);
            ICharacterRenderer rendererInterface = rendererObj.GetComponent<ICharacterRenderer>();
            if (rendererInterface != null)
            {
                _renderer = Option<ICharacterRenderer>.Some(rendererInterface);
                indicators.transform.localScale = rendererInterface.IndicatorScale * Vector3.one;
            }
            else
            {
                Debug.LogWarning($"Character renderer prefab {script.RendererPrefab.name} does not have component that implements ICharacterRenderer interface.", context: this);
                _renderer = Option<ICharacterRenderer>.None;
            }

            SetIdleSpeed(UnityEngine.Random.Range(0.85f, 1.15f));
            AllowIdleAnimationTimeUpdateExternally(true);
            lustBar.gameObject.SetActive(script.CanActAsGirl);
            UpdateSide(stateMachine.PositionHandler.IsLeftSide);
        }

        public void SetRendererAlpha(float alpha)
        {
            if (_renderer.TrySome(out ICharacterRenderer characterRenderer))
                characterRenderer.SetAlpha(alpha);
        }

        public void MatchAnimationWithState(CharacterState state)
        {
            if (_renderer.IsNone || StateMachine.IsNone)
                return;

            _renderer.Value.ClearParameters();
            CharacterStateMachine owner = StateMachine.Value;
            switch (state)
            {
                case CharacterState.Defeated:
                case CharacterState.Grappled:
                    gameObject.SetActive(false);
                    break;
                case CharacterState.Idle:
                case CharacterState.Charging:
                case CharacterState.Recovering:
                case CharacterState.Stunned:
                {
                    gameObject.SetActive(true);
                    AnimationStatus = AnimationStatus.Common;
                    AllowIndicatorsInternally(true);
                    AllowBarsInternally(true);
                    AllowRendering(true);
                    SetAnimationWithoutNotifyStatus(CombatAnimation.Idle);
                    FadeRenderer(endValue: 1f, ICharacterRenderer.FadeToCorpseHalfDuration);
                    MoveToDefaultPosition(baseDuration: Option<float>.None);
                    break;
                }
                case CharacterState.Downed when AnimationStatus is AnimationStatus.Downed:       CheckDownedAnimation(); break;
                case CharacterState.Downed:                                                      AnimateDowned(); break;
                case CharacterState.Grappling when AnimationStatus is AnimationStatus.Grappling: CheckGrapplingAnimation(); break;
                case CharacterState.Grappling:
                    gameObject.SetActive(true);
                    foreach (StatusInstance status in owner.StatusReceiverModule.GetAllRelated)
                    {
                        if (status is not LustGrappled lustGrappled || lustGrappled.IsDeactivated || lustGrappled.Restrainer != owner)
                            continue;

                        CombatAnimation grapplingAnimation = new(lustGrappled.TriggerName, Option<CasterContext>.None, Option<TargetContext>.None);
                        AnimateGrappling(grapplingAnimation);
                        return;
                    }

                    Debug.LogWarning($"Character {owner.Script.CharacterName} is in grappling state but none of the related statuses are grappling.", context: this);
                    break;
                case CharacterState.Corpse when AnimationStatus is AnimationStatus.Corpse:                               CheckCorpseAnimation(); break;
                case CharacterState.Corpse when owner.Script.BecomesCorpseOnDefeat(out CombatAnimation corpseAnimation): AnimateCorpse(corpseAnimation); break;
                case CharacterState.Corpse:
                    gameObject.SetActive(false);
                    Debug.LogWarning($"Character {owner.Script.CharacterName} is in corpse state but does not have a corpse animation.", context: this);
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        private void CheckDownedAnimation()
        {
            gameObject.SetActive(true);
            AnimationStatus = AnimationStatus.Downed;
            AllowIndicatorsInternally(true);
            AllowBarsInternally(true);
            AllowRendering(true);
            SetAnimationWithoutNotifyStatus(CombatAnimation.Downed);
            FadeRenderer(endValue: 1f, ICharacterRenderer.FadeToDownedHalfDuration);
        }

        public void AnimateDowned()
        {
            gameObject.SetActive(true);
            if (AnimationStatus is AnimationStatus.Downed)
                return;

            AnimationStatus = AnimationStatus.Downed;
            AllowIndicatorsInternally(true);
            AllowBarsInternally(true);
            AllowRendering(true);

            Option<Tween> tween = FadeRenderer(endValue: 0.5f, ICharacterRenderer.FadeToDownedHalfDuration);
            if (tween.IsNone)
            {
                SetAnimationWithoutNotifyStatus(CombatAnimation.Downed);
                return;
            }
            
            DisplayModule self = this;
            tween.Value.onComplete += () =>
            {
                if (self.AnimationStatus is not AnimationStatus.Downed)
                    return;
                
                self.gameObject.SetActive(true);
                self.FadeRenderer(endValue: 1f, ICharacterRenderer.FadeToDownedHalfDuration);
                self.SetAnimationWithoutNotifyStatus(CombatAnimation.Downed);
            };
        }

        /// <summary> Should only be used during action. </summary>
        public void AnimateDownedAfterRiposte()
        {
            gameObject.SetActive(true);
            if (AnimationStatus is AnimationStatus.Downed)
                return;
            
            AnimationStatus = AnimationStatus.Downed;
            DisplayModule self = this;
            DOVirtual.DelayedCall(Riposte.Delay * 1.5f, () =>
            {
                if (self == null || self.AnimationStatus is not AnimationStatus.Downed)
                    return;

                self.gameObject.SetActive(true);
                self.AllowIndicatorsInternally(true);
                self.AllowBarsInternally(true);
                self.AllowRendering(true);
                Option<Tween> tween = self.FadeRenderer(endValue: 0.5f, ICharacterRenderer.FadeToDownedHalfDuration);
                if (tween.IsNone)
                {
                    self.SetAnimationWithoutNotifyStatus(CombatAnimation.Downed);
                    self.SetRendererAlpha(1f);
                    return;
                }

                tween.Value.onComplete += () =>
                {
                    if (self == null || self.AnimationStatus is not AnimationStatus.Downed)
                        return;

                    self.gameObject.SetActive(true);
                    self.FadeRenderer(endValue: 1f, ICharacterRenderer.FadeToDownedHalfDuration);
                    self.SetAnimationWithoutNotifyStatus(CombatAnimation.Downed);
                };
            });
        }

        private void CheckCorpseAnimation()
        {
            gameObject.SetActive(true);
            AnimationStatus = AnimationStatus.Corpse;
            AllowRendering(true);
            AllowIndicatorsInternally(true);
            AllowBarsInternally(false);

            if (StateMachine.AssertSome(out CharacterStateMachine owner) == false)
                return;
            
            if (owner.Script.BecomesCorpseOnDefeat(out CombatAnimation corpseAnimation) == false)
            {
                Debug.LogWarning($"Character {owner.Script.CharacterName} is in corpse state but does not have a corpse animation.", context: this);
                return;
            }
            
            FadeRenderer(endValue: 1f, ICharacterRenderer.FadeToCorpseHalfDuration);
            SetAnimationWithoutNotifyStatus(corpseAnimation);
        }
        
        public void AnimateCorpse(CombatAnimation corpseAnimation)
        {
            gameObject.SetActive(true);
            Option<CharacterStateMachine> stateMachine = StateMachine;
            if (_renderer.IsNone || stateMachine.IsNone)
            {
                Debug.LogWarning($"Corpse animation was called on {name} but it has no animator or state machine. Has animator: {_renderer.IsSome}, has state machine: {stateMachine.IsSome}");
                return;
            }
            
            ICharacterRenderer characterRenderer = _renderer.Value;
            if (AnimationStatus is AnimationStatus.Corpse)
            {
                CombatAnimation lastAnimation = characterRenderer.LastAnimationSent;
                if (lastAnimation != corpseAnimation)
                    SetAnimationWithoutNotifyStatus(corpseAnimation);

                FadeRenderer(endValue: 1f, ICharacterRenderer.FadeToCorpseHalfDuration);
                return;
            }

            AnimationStatus previous = AnimationStatus;
            AnimationStatus = AnimationStatus.Corpse;
            AllowRendering(true);
            AllowIndicatorsInternally(true);
            AllowBarsInternally(false);
            DisplayModule self = this;
            switch (previous)
            {
                case AnimationStatus.Common:
                case AnimationStatus.Downed:
                case AnimationStatus.Grappling:
                {
                    gameObject.SetActive(true);
                    Tween tween = characterRenderer.Fade(endValue: 0.5f, ICharacterRenderer.FadeToCorpseHalfDuration);
                    tween.OnComplete(() =>
                    {
                        if (self == null || self.AnimationStatus is not AnimationStatus.Corpse)
                            return;
                        
                        self.gameObject.SetActive(true);
                        self.SetAnimationWithoutNotifyStatus(corpseAnimation);
                        self.FadeRenderer(endValue: 1f, ICharacterRenderer.FadeToCorpseHalfDuration);
                    });
                    break;
                }
                case AnimationStatus.Grappled:
                case AnimationStatus.Defeated:
                {
                    if (gameObject.activeSelf == false)
                    {
                        gameObject.SetActive(true);
                        SetRendererAlpha(1f);
                        SetAnimationWithoutNotifyStatus(corpseAnimation);
                        return;
                    }

                    Tween tween = characterRenderer.Fade(endValue: 0.5f, ICharacterRenderer.FadeToCorpseHalfDuration);
                    tween.OnComplete(() =>
                    {
                        if (self == null || self.AnimationStatus is not AnimationStatus.Corpse)
                            return;

                        self.gameObject.SetActive(true);
                        self.SetAnimationWithoutNotifyStatus(corpseAnimation);
                        self.FadeRenderer(endValue: 1f, ICharacterRenderer.FadeToCorpseHalfDuration);
                    });
                    break;
                }
                default: throw new ArgumentOutOfRangeException($"Invalid animation status: {AnimationStatus}");
            }
        }

        public void AnimateCorpseAfterRiposte(CombatAnimation corpseAnimation)
        {
            gameObject.SetActive(true);
            if (AnimationStatus is AnimationStatus.Corpse && _renderer.AssertSome(out ICharacterRenderer selfRenderer) && selfRenderer.LastAnimationSent == corpseAnimation)
                return;

            AnimationStatus = AnimationStatus.Corpse;
            DisplayModule self = this;
            DOVirtual.DelayedCall(Riposte.Delay * 1.5f, () =>
            {
                if (self == null || self.AnimationStatus is not AnimationStatus.Corpse)
                    return;
                
                self.AllowRendering(true);
                self.AllowIndicatorsInternally(true);
                self.AllowBarsInternally(false);
                if (self.gameObject.activeSelf == false)
                {
                    self.gameObject.SetActive(true);
                    self.SetAnimationWithoutNotifyStatus(corpseAnimation);
                    self.SetRendererAlpha(1f);
                    return;
                }
                
                Option<Tween> tween = self.FadeRenderer(endValue: 0.5f, ICharacterRenderer.FadeToCorpseHalfDuration);
                if (tween.IsNone)
                {
                    self.SetAnimationWithoutNotifyStatus(corpseAnimation);
                    return;
                }

                tween.Value.onComplete += () =>
                {
                    if (self == null || self.AnimationStatus is not AnimationStatus.Corpse)
                        return;
                    
                    if (self.gameObject.activeSelf == false)
                    {
                        self.gameObject.SetActive(true);
                        self.SetAnimationWithoutNotifyStatus(corpseAnimation);
                        self.SetRendererAlpha(1f);
                        return;
                    }

                    self.SetAnimationWithoutNotifyStatus(corpseAnimation);
                    self.FadeRenderer(endValue: 1f, ICharacterRenderer.FadeToCorpseHalfDuration);
                };
            });
        }
        
        public void AnimateDefeated(Action<DisplayModule> onFinish)
        {
            Option<CharacterStateMachine> stateMachine = StateMachine;
            if (stateMachine.IsNone || _renderer.TrySome(out ICharacterRenderer selfRenderer) == false)
            {
                Debug.LogWarning($"Defeat animation was called on {name} but it has no animator or state machine. Has animator: {_renderer.IsSome}, has state machine: {stateMachine.IsSome}", context: this);
                onFinish?.Invoke(this);
                gameObject.SetActive(false);
                return;
            }

            if (gameObject.activeSelf == false)
            {
                onFinish?.Invoke(this);
                return;
            }

            DisplayModule self = this;
            if (AnimationStatus is AnimationStatus.Defeated)
            {
                Tween currentTween = selfRenderer.FadeTween;
                if (currentTween is { active: true })
                    currentTween.onComplete += () => onFinish?.Invoke(self);
                else
                    onFinish?.Invoke(self);
                
                return;
            }

            AnimationStatus = AnimationStatus.Defeated;
            AllowBarsInternally(false);
            AllowIndicatorsInternally(false);
            CombatAnimation hitAnimation = new(CombatAnimation.Param_Hit, Option<CasterContext>.None, Option<TargetContext>.None);
            SetAnimationWithoutNotifyStatus(hitAnimation);
            selfRenderer.Fade(endValue: 0f, ICharacterRenderer.FadeToDeathDuration).OnComplete(() =>
            {
                onFinish?.Invoke(self);
                self.gameObject.SetActive(false);
            });
        }

        public void AnimateDefeatedAfterRiposte()
        {
            if (AnimationStatus is AnimationStatus.Defeated && gameObject.activeSelf == false)
                return;
            
            AnimationStatus = AnimationStatus.Defeated;
            DisplayModule self = this;
            DOVirtual.DelayedCall(Riposte.Delay * 1.5f, () =>
            {
                if (self == null || self.AnimationStatus is not AnimationStatus.Defeated)
                    return;

                self.AllowBarsInternally(false);
                self.AllowIndicatorsInternally(false);
                if (self.gameObject.activeSelf == false)
                    return;
                
                Option<Tween> tween = self.FadeRenderer(endValue: 0f, ICharacterRenderer.FadeToDeathDuration);
                if (tween.IsNone)
                {
                    self.gameObject.SetActive(false);
                    return;
                }
                
                tween.Value.onComplete += () =>
                {
                    if (self != null && self.AnimationStatus is AnimationStatus.Defeated)
                        self.gameObject.SetActive(false);
                };
            });
        }

        private void CheckGrapplingAnimation()
        {
            gameObject.SetActive(true);
            AnimationStatus = AnimationStatus.Grappling;
            AllowBarsInternally(true);
            AllowRendering(true);
            AllowIndicatorsInternally(true);

            if (StateMachine.AssertSome(out CharacterStateMachine owner) == false)
                return;

            foreach (StatusInstance status in owner.StatusReceiverModule.GetAllRelated)
            {
                if (status is not LustGrappled lustGrappled || lustGrappled.IsDeactivated || lustGrappled.Restrainer != owner)
                    continue;

                CombatAnimation grapplingAnimation = new(lustGrappled.TriggerName, Option<CasterContext>.None, Option<TargetContext>.None);
                SetAnimationWithoutNotifyStatus(grapplingAnimation);
                FadeRenderer(endValue: 1f, ICharacterRenderer.FadeToCorpseHalfDuration);
                return;
            }
            
            Debug.LogWarning($"No grappling animation was found for {name} but it is grappling", context: this);
        }
        
        public void AnimateGrappling(CombatAnimation grapplingAnimation)
        {
            gameObject.SetActive(true);
            if (_renderer.AssertSome(out ICharacterRenderer selfRenderer) == false)
                return;

            if (AnimationStatus is AnimationStatus.Grappling && selfRenderer.LastAnimationSent == grapplingAnimation)
                return;

            AnimationStatus = AnimationStatus.Grappling;
            AllowBarsInternally(true);
            AllowRendering(true);
            AllowIndicatorsInternally(true);
            SetAnimationWithoutNotifyStatus(grapplingAnimation);
            FadeRenderer(endValue: 1f, ICharacterRenderer.FadeToCorpseHalfDuration);
        }

        /// <summary> This doesn't fade the renderer as the tempt action sequence is responsible for this </summary>
        public void AnimateGrapplingInsideTemptSkill(CombatAnimation grapplingAnimation)
        {
            gameObject.SetActive(true);
            if (_renderer.AssertSome(out ICharacterRenderer selfRenderer) == false)
                return;

            if (AnimationStatus is AnimationStatus.Grappling && selfRenderer.LastAnimationSent == grapplingAnimation)
                return;

            AnimationStatus = AnimationStatus.Grappling;
            AllowBarsInternally(true);
            AllowRendering(true);
            AllowIndicatorsInternally(true);
            SetAnimationWithoutNotifyStatus(grapplingAnimation);
        }

        public void AnimateGrappled() // the grappler is responsible for rendering the animation
        {
            AnimationStatus = AnimationStatus.Grappled;
            gameObject.SetActive(false);
        }

        public void DeAnimateGrappled(CharacterStateMachine restrainer)
        {
            if (StateMachine.IsNone)
                return;

            CharacterStateMachine owner = StateMachine.Value;
            DisplayModule self = this;
            Action underTheMist = () =>
            {
                if (self != null)
                    self.MatchAnimationWithState(owner.StateEvaluator.PureEvaluate());
                
                if (restrainer.Display.TrySome(out DisplayModule restrainerDisplay))
                    restrainerDisplay.MatchAnimationWithState(restrainer.StateEvaluator.PureEvaluate());
            };
            
            CombatManager.ActionAnimator.AnimateOverlayMist(underTheMist, Option<CharacterStateMachine>.Some(owner));
        }

        /// <summary>We will try to make a fancy animation, otherwise default to the normal one</summary>
        public void DeAnimateGrappledFromStunnedRestrainer(CharacterStateMachine restrainer)
        {
            if (StateMachine.IsNone)
                return;

            if (StateMachine.Value.StateEvaluator.PureEvaluate() is not CharacterState.Downed)
            {
                DeAnimateGrappled(restrainer);
                return;
            }

            if (restrainer.Display.TrySome(out DisplayModule restrainerDisplay) == false || _renderer.TrySome(out ICharacterRenderer selfRenderer) == false)
            {
                DeAnimateGrappled(restrainer);
                return;
            }

            transform.position = restrainerDisplay.transform.position;
            gameObject.SetActive(true);
            selfRenderer.SetAlpha(0f);
            AnimationStatus = AnimationStatus.Grappled;
            AllowBarsInternally(true);
            AllowRendering(true);
            AllowIndicatorsInternally(true);
            SetAnimationWithoutNotifyStatus(CombatAnimation.Downed);
            FadeRenderer(endValue: 1f, ICharacterRenderer.FadeToCorpseHalfDuration * 2f);
            MoveToDefaultPosition(baseDuration: Option<float>.None);
        }

        public bool Equals(DisplayModule other) => other == this;

        public CharacterPositioning ComputePosition() => StateMachine.TrySome(out CharacterStateMachine stateMachine) ? CombatManager.PositionManager.ComputePositioning(stateMachine) : CharacterPositioning.None;
        
        #if UNITY_EDITOR
    #region Debug Buttons
        [Button]
        private void InstantMaxLust()
        {
            if (StateMachine.TrySome(out CharacterStateMachine owner) && owner.LustModule.TrySome(out ILustModule lustModule))
                lustModule.SetLust(200);
        }

        [Button]
        private void InstantDowned()
        {
            if (StateMachine.TrySome(out CharacterStateMachine owner) && owner.DownedModule.TrySome(out IDownedModule downedModule))
                downedModule.SetInitial(IDownedModule.DefaultDownedDurationOnZeroStamina);
        }

        [Button]
        private void InstantZeroStamina()
        {
            if (StateMachine.TrySome(out CharacterStateMachine owner) && owner.StaminaModule.TrySome(out IStaminaModule staminaModule))
                staminaModule.SetCurrent(0);
        }
    #endregion
        #endif
    }
}
