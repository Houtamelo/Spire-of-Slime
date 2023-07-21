using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Collections.Pooled;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Managers;
using Core.Pause_Menu.Scripts;
using Core.Utils.Collections;
using Core.Utils.Extensions;
using Core.Utils.Math;
using Core.Utils.Patterns;
using DG.Tweening;
using JetBrains.Annotations;
using ListPool;
using UnityEngine;
using Utils.Patterns;
using static Core.Combat.Scripts.Skills.Action.IActionSequence;
using Object = UnityEngine.Object;

namespace Core.Combat.Scripts.Skills.Action.Overlay
{
    public class OverlayActionSequence : IActionSequence
    {
        private readonly CombatManager _combatManager;
        private readonly OverlayAnimator _overlayPrefab;
        
        private readonly Dictionary<CharacterStateMachine, Vector3> _endPositions = new();
        private readonly Dictionary<CharacterStateMachine, Vector3> _startPositions = new();
        
        private readonly ListPool<ActionResult> _results = new(9);

        public readonly PlannedSkill Plan;

        private readonly PooledSet<CharacterStateMachine> _targets = new(8);
        public IReadOnlyCollection<CharacterStateMachine> Targets => _targets;
        
        private readonly PooledSet<CharacterStateMachine> _outsiders = new(8);
        public IReadOnlyCollection<CharacterStateMachine> Outsiders => _outsiders;

        public CharacterStateMachine Caster => Plan.Caster;

        private TSpan _cachedRecovery; // set in ResolveSkill()
        
        private bool _isPlaying;
        public bool IsPlaying => _isPlaying && IsDone == false;

        private bool _isDone;
        public bool IsDone
        {
            get => _isDone;
            private set
            {
                _isDone = value;
                if (value)
                    _isPlaying = false;
            }
        }

        public OverlayActionSequence(PlannedSkill plan, CombatManager combatManager, OverlayAnimator overlayPrefab)
        {
            Plan = plan;
            _combatManager = combatManager;
            _overlayPrefab = overlayPrefab;
        }

        public void UpdateCharactersStartPosition()
        {
            Debug.Assert(_isPlaying);
            ReadOnlySpan<(CharacterStateMachine character, Vector3 position)> allPositions = _combatManager.PositionManager.ComputeAllDefaultPositions();
            foreach ((CharacterStateMachine character, Vector3 position) in allPositions)
                _startPositions[character] = position;
        }

        public void InstantMoveOutsideCharacters()
        {
            Debug.Assert(_isPlaying);
            foreach ((CharacterStateMachine character, Vector3 position) in _combatManager.PositionManager.ComputeAllDefaultPositions())
            {
                if (Outsiders.Contains(character) && character.Display.TrySome(out DisplayModule display))
                    display.MoveToPosition(position, baseDuration: Option.None);
            }
        }
        
        public void AddOutsider([NotNull] CharacterStateMachine outsider) => _outsiders.Add(outsider);

        public void Play(bool announce)
        {
            _isPlaying = true;
            
            if (ActionUtils.Validate(_combatManager, Plan, Caster) == false)
            {
                IsDone = true;
                return;
            }
            
            if (ActionUtils.TryFillTargetList(Plan, Caster, _outsiders, _targets) == false)
            {
                IsDone = true;
                return;
            }

            ActionUtils.IncrementSkillCounter(Plan, Caster);
            ActionUtils.FillEndPositionsForOverlay(_combatManager, Targets, Caster, _endPositions);

            Sequence sequence = DOTween.Sequence(target: this).SetUpdate(isIndependentUpdate: false);

            if (announce)
            {
                ActionUtils.AnimateIndicators(Caster, Targets, _startPositions);
                _combatManager.AnnouncePlan(Plan, StartDuration, PopDuration);
                sequence.AppendInterval(StartDuration - BarsFadeDuration);
                sequence.AppendCallback(() => ActionUtils.FadeDownAllBars(_combatManager, BarsFadeDuration));
            }
            else
            {
                ActionUtils.FadeDownAllBars(_combatManager, BarsFadeDuration);
            }
            
            sequence.AppendInterval(BarsFadeDuration * 0.75f);

            sequence.AppendCallback(() => ActionUtils.FadeDownUIAndBackground(_combatManager, PopDuration));
            sequence.AppendCallback(() => ActionUtils.FadeDownOutsideCharacters(_outsiders, PopDuration));
            sequence.AppendCallback(() => ActionUtils.AnimateCameraAndSplashScreen(_combatManager, Plan, Caster, _targets, PopDuration, AnimationDuration));
            
            sequence.AppendCallback(() => ActionUtils.LerpCharactersToAnimationPositions(Caster, _endPositions, _targets, CharacterScale, PopDuration * 2f));
            sequence.AppendCallback(() => ActionUtils.FadeDownCasterAndTargets(Caster, _targets, PopDuration * 2f));
            
            sequence.AppendInterval(PopDuration);

            Reference<OverlayAnimator> animatorInstance = new(null);
            
            sequence.AppendCallback(() =>
            {
                animatorInstance.Value = _overlayPrefab.InstantiateWithFixedLocalScale(_combatManager.OverlayAnimatorsParent);
                Vector3 center = new(0f, YPosition, 0f);
                animatorInstance.Value.transform.position = center;
                animatorInstance.Value.gameObject.SetActive(true);
                animatorInstance.Value.FadeUp(PopDuration, Plan);
            });
            
            sequence.AppendCallback(ResolveSkill);

            sequence.AppendInterval(PopDuration); // wait for caster and targets to fade down and for animator to fade up
            
            switch (PauseMenuManager.SkillOverlayModeHandler.Value)
            {
                case SkillOverlayMode.Auto:
                {
                    sequence.AppendCallback(() => _combatManager.StartCoroutine(WaitForInputOrTime(animatorInstance, PauseMenuManager.SkillOverlayAutoDurationHandler.Value)));
                    break;
                }
                case SkillOverlayMode.WaitForInput:
                {
                    sequence.AppendCallback(() => _combatManager.StartCoroutine(WaitForInput(animatorInstance)));
                    break;
                }
                default: throw new ArgumentOutOfRangeException(nameof(PauseMenuManager.SkillOverlayModeHandler), message: $"Unhandled type {PauseMenuManager.SkillOverlayModeHandler.Value}");
            }
        }
        
        private void ResolveSkill()
        {
            SkillStruct skillStruct = SkillStruct.CreateInstance(Plan.Skill, Caster, Plan.Target);
            skillStruct.ApplyCustomStats();
            Caster.SkillModule.ModifySkill(ref skillStruct);

            if (Caster.StateEvaluator.PureEvaluate() is not CharacterState.Defeated and not CharacterState.Corpse and not CharacterState.Grappled and not CharacterState.Downed)
            {
                ActionResult result = SkillCalculator.DoToCaster(ref skillStruct);
                _results.Add(result);
            }
            
            _combatManager.ActionAnimator.AnimateSpeedLines(Plan, AnimationDuration);

            ref CustomValuePooledList<TargetProperties> targetProperties = ref skillStruct.TargetProperties;
            int count = targetProperties.Count;
            
            if (Plan.Skill.IsPositive)
                for (int index = 0; index < count; index++)
                {
                    ReadOnlyProperties properties = targetProperties[index].ToReadOnly();
                    CharacterStateMachine target = properties.Target;
                    if (target.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Grappled)
                        continue;

                    ActionResult result = target.SkillModule.TakeSkillAsTarget(ref skillStruct, properties, isRiposte: false);
                    _results.Add(result);

                    if (target.Display.AssertSome(out DisplayModule targetDisplay) == false)
                        continue;
                    
                    targetDisplay.SetBaseSpeed(SpeedMultiplier);
                }
            else
                for (int index = 0; index < count; index++)
                {
                    ReadOnlyProperties properties = targetProperties[index].ToReadOnly();
                    CharacterStateMachine target = properties.Target;
                    if (target.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Grappled)
                        continue;

                    ActionResult result = target.SkillModule.TakeSkillAsTarget(ref skillStruct, properties, isRiposte: false);
                    _results.Add(result);

                    target.Events.OnSelfAttacked(ref result);
                    Caster.Events.OnTargetAttacked(ref result);
                    
                    if (target.Display.AssertSome(out DisplayModule targetDisplay) == false)
                        continue;
                    
                    targetDisplay.SetBaseSpeed(SpeedMultiplier);
                }
            
            _cachedRecovery = skillStruct.Recovery;
            skillStruct.Dispose();
        }

        private IEnumerator WaitForInputOrTime([NotNull] Reference<OverlayAnimator> overlayAnimator, float maximumTime)
        {
            float endTime = Time.time + maximumTime;
            while (Time.time < endTime && (InputManager.AssertInstance(out InputManager inputManager) == false || inputManager.AnyAdvanceInputThisFrame() == false))
                yield return null;
            
            OnProceed(overlayAnimator);
        }

        private IEnumerator WaitForInput([NotNull] Reference<OverlayAnimator> overlayAnimator)
        {
            while (InputManager.AssertInstance(out InputManager inputManager) == false || inputManager.AnyAdvanceInputThisFrame() == false)
                yield return null;
            
            OnProceed(overlayAnimator);
        }

        private void OnProceed([NotNull] Reference<OverlayAnimator> animatorInstance)
        {
            if (animatorInstance.Value != null)
                animatorInstance.Value.FadeDown(PopDuration);
            
            ActionUtils.LerpCharactersToOriginalPositions(_startPositions, Caster, _targets, PopDuration);
            ActionUtils.FadeUpOutsideCharacters(_outsiders, PopDuration);
            ActionUtils.FadeUpActionSplashScreenAndSpeedLines(_combatManager, PopDuration);
            ActionUtils.FadeUpUIAndBackground(_combatManager, PopDuration);
            ActionUtils.FadeUpCasterAndTargets(Caster, _targets, PopDuration);

            Sequence sequence = DOTween.Sequence(target: this).SetUpdate(isIndependentUpdate: false);
            sequence.AppendInterval(PopDuration);

            sequence.AppendCallback(() =>
            {
                if (animatorInstance.Value != null)
                    Object.Destroy(animatorInstance.Value.gameObject);
            });

            sequence.AppendCallback(() => ActionUtils.MoveAllToDefaultAnimationAndFadeUpBars(_combatManager, _targets, Caster, BarsFadeDuration));
            sequence.AppendCallback(() => ActionUtils.SetRecoveryAndNotifyFinished(_results, _cachedRecovery, Caster, Plan));
            sequence.AppendCallback(() => IsDone = true);
            sequence.AppendCallback(Dispose);
        }
        
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;
            
            _disposed = true;
            
            Span<ActionResult> span = _results.AsSpan(); // this seems unnecessary but you can't access items by ref directly from the listpool type, amusingly enough this actually access the original list pool
            for (int index = 0; index < _results.Count; index++)
            {
                ref ActionResult actionResult = ref span[index];
                actionResult.Dispose();
            }
            
            _targets?.Dispose();
            _results?.Dispose();
            _outsiders?.Dispose();
        }

        ~OverlayActionSequence()
        {
            Dispose();
        }

        public void ForceStop()
        {
            if (IsDone)
                return;

            IsDone = true;
            DOTween.Kill(targetOrId: this);
            Plan?.NotifyDone();
            Dispose();
        }
    }
}