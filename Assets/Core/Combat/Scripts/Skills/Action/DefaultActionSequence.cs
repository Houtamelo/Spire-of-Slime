using System;
using System.Collections.Generic;
using System.Linq;
using Collections.Pooled;
using Core.Combat.Scripts.Animations;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Managers;
using Core.Utils.Collections;
using Core.Utils.Math;
using Core.Utils.Patterns;
using DG.Tweening;
using JetBrains.Annotations;
using ListPool;
using UnityEngine;
using static Core.Combat.Scripts.Skills.Action.IActionSequence;

namespace Core.Combat.Scripts.Skills.Action
{
    public class DefaultActionSequence : IActionSequence, IDisposable
    {
        private readonly CombatManager _combatManager;
        
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

        public DefaultActionSequence(PlannedSkill plan, CombatManager combatManager)
        {
            Plan = plan;
            _combatManager = combatManager;
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

        public void Play()
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
            ActionUtils.AnimateIndicators(Caster, Targets, _startPositions);
            ActionUtils.FillDefaultEndPositions(_combatManager, Targets, Plan, Caster, _endPositions);
            _combatManager.AnnouncePlan(Plan,StartDuration, PopDuration);

            Sequence sequence = DOTween.Sequence(target: this).SetUpdate(isIndependentUpdate: false);

            sequence.AppendInterval(StartDuration - BarsFadeDuration);
            
            sequence.AppendCallback(() => ActionUtils.FadeDownAllBars(_combatManager, BarsFadeDuration));
            
            sequence.AppendInterval(BarsFadeDuration * 0.75f);

            sequence.AppendCallback(() => ActionUtils.FadeDownUIAndBackground(_combatManager, PopDuration));
            sequence.AppendCallback(() => ActionUtils.FadeDownOutsideCharacters(_outsiders, PopDuration));
            sequence.AppendCallback(() => ActionUtils.AnimateCameraAndSplashScreen(_combatManager, Plan, Caster, _targets, PopDuration, AnimationDuration));
            sequence.AppendCallback(() => ActionUtils.LerpCharactersToAnimationPositions(Caster, _endPositions, _targets, CharacterScale, PopDuration));
            
            sequence.AppendInterval(PopDuration);
            
            sequence.AppendCallback(ResolveSkill);
            
            sequence.AppendInterval(AnimationDuration);
            
            sequence.AppendCallback(() => ActionUtils.LerpCharactersToOriginalPositions(_startPositions, Caster, _targets, PopDuration));
            sequence.AppendCallback(() => ActionUtils.FadeUpOutsideCharacters(_outsiders, PopDuration));
            sequence.AppendCallback(() => ActionUtils.FadeUpActionSplashScreenAndSpeedLines(_combatManager, PopDuration));
            sequence.AppendCallback(() => ActionUtils.FadeUpUIAndBackground(_combatManager, PopDuration));
            
            sequence.AppendInterval(PopDuration);

            sequence.AppendCallback(() => ActionUtils.MoveAllToDefaultAnimationAndFadeUpBars(_combatManager, _targets, Caster, BarsFadeDuration));
            sequence.AppendCallback(() => ActionUtils.SetRecoveryAndNotifyFinished(_results, _cachedRecovery, Caster, Plan));
            sequence.AppendCallback(() => IsDone = true);
            sequence.AppendCallback(Dispose);
        }

        private void ResolveSkill()
        {
            SkillStruct skillStruct = SkillStruct.CreateInstance(Plan.Skill, Caster, Plan.Target);
            skillStruct.ApplyCustomStats();

            Caster.SkillModule.ModifySkill(ref skillStruct);
            bool anyCrit = false;

            if (Caster.StateEvaluator.PureEvaluate() is not CharacterState.Defeated and not CharacterState.Corpse and not CharacterState.Grappled and not CharacterState.Downed)
            {
                ActionResult result = SkillCalculator.DoToCaster(ref skillStruct);
                _results.Add(result);
                anyCrit |= result.Critical;
                
                if (Caster.Display.AssertSome(out DisplayModule casterDisplay))
                {
                    (float xMovement, AnimationCurve animationCurve) = Plan.Skill.GetCasterMovement(isLeftSide: Caster.PositionHandler.IsLeftSide);
                    casterDisplay.transform.DOMoveX(endValue: xMovement, AnimationDuration).SetRelative().SetEase(animationCurve);
                    _combatManager.ActionAnimator.AnimateSpeedLines(Plan, AnimationDuration);
                    casterDisplay.SetBaseSpeed(SpeedMultiplier);
                }
            }

            ref CustomValuePooledList<TargetProperties> targetProperties = ref skillStruct.TargetProperties;
            int count = targetProperties.Count;
            
            if (Plan.Skill.IsPositive)
            {
                for (int index = 0; index < count; index++)
                {
                    ReadOnlyProperties properties = targetProperties[index].ToReadOnly();
                    CharacterStateMachine target = properties.Target;
                    if (target.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Grappled)
                        continue;

                    ActionResult result = target.SkillModule.TakeSkillAsTarget(ref skillStruct, properties, isRiposte: false);
                    _results.Add(result);
                    anyCrit |= result.Critical;
                    
                    if (target.Display.AssertSome(out DisplayModule targetDisplay) == false)
                        continue;

                    (float xMovement, AnimationCurve animationCurve) = Plan.Skill.GetTargetMovement(isLeftSide: target.PositionHandler.IsLeftSide);
                    targetDisplay.transform.DOMoveX(endValue: xMovement, AnimationDuration).SetRelative().SetEase(animationCurve);
                    targetDisplay.SetBaseSpeed(SpeedMultiplier);
                }
            }
            else
            {
                for (int index = 0; index < count; index++)
                {
                    ReadOnlyProperties properties = targetProperties[index].ToReadOnly();
                    CharacterStateMachine target = properties.Target;
                    if (target.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Grappled)
                        continue;

                    ActionResult result = target.SkillModule.TakeSkillAsTarget(ref skillStruct, properties, isRiposte: false);
                    _results.Add(result);
                    anyCrit |= result.Critical;
                    
                    target.Events.OnSelfAttacked(ref result);
                    Caster.Events.OnTargetAttacked(ref result);
                    
                    if (target.Display.AssertSome(out DisplayModule targetDisplay) == false)
                        continue;

                    if (result.Hit && targetDisplay.AnimatorTransform.TrySome(out Transform targetAnimatorTransform))
                        targetAnimatorTransform.DOShakePosition(ShakeDuration, 0.2f, 60, fadeOut: false);

                    (float xMovement, AnimationCurve animationCurve) = Plan.Skill.GetTargetMovement(isLeftSide: target.PositionHandler.IsLeftSide);
                    targetDisplay.transform.DOMoveX(endValue: xMovement, duration: AnimationDuration).SetRelative().SetEase(animationCurve);
                    targetDisplay.SetBaseSpeed(SpeedMultiplier);
                }

                if (anyCrit)
                {
                    Debug.Log("Crit! Slowing time.");
                    DOVirtual.Float(from: 0.5f, to: 1f, duration: 0.5f * DurationMultiplier, value =>
                    {
                        Time.timeScale = value;
                        Debug.Log($"Time scale: {Time.timeScale}");
                    }).SetEase(Ease.InQuart).SetUpdate(isIndependentUpdate: true);
                }
            }

            if (Caster.Display.AssertSome(out DisplayModule display))
            {
                CasterContext casterContext = new(_results.ToArray());
                CombatAnimation animation = new(Plan.Skill.AnimationParameter, casterContext, Option<TargetContext>.None);
                display.SetAnimationWithoutNotifyStatus(animation);
            }

            _cachedRecovery = skillStruct.Recovery;
            skillStruct.Dispose();
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

        ~DefaultActionSequence()
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