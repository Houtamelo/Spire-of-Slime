﻿using System.Diagnostics.Contracts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Cues;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Managers;
using Core.Combat.Scripts.Skills.Action;
using Core.Utils.Collections;
using Core.Utils.Patterns;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Combat.Scripts.Animations
{
    public class LazyAnimationQueue : MonoBehaviour
    {
        // [SerializeField, Required, SceneObjectsOnly]
        // private LustPromptDisplay lustPromptDisplay;
        
        [SerializeField, Required, SceneObjectsOnly]
        private CombatManager combatManager;
        
        [SerializeField, Required, SceneObjectsOnly]
        private StatusVFXManager statusVfxManager;
        
        private readonly ListQueue<AnimationRoutineInfo> _priorityQueue = new();
        private readonly ListQueue<IActionSequence> _queuedActions = new();
        private readonly IndexableHashSet<AnimationRoutineInfo> _currentVFXAnimations = new();
        
        public IActionSequence CurrentAction { get; private set; }

        private void OnDestroy()
        {
            CurrentAction?.ForceStop();
        }

        [Pure]
        public QueueState EvaluateState()
        {
            if (CurrentAction is { IsPlaying: true })
                return QueueState.Playing;

            // if (lustPromptDisplay.IsBusy)
            //     return QueueState.Playing;

            if (statusVfxManager.IsBusy())
                return QueueState.Playing;

            for (int i = 0; i < _currentVFXAnimations.Count; i++)
            {
                AnimationRoutineInfo info = _currentVFXAnimations[i];
                if (info.HasStarted == false)
                {
                    Debug.LogWarning("Animation in current list has not started, starting now...", context: this);
                    if (info.StartIfValid() == false)
                    {
                        _currentVFXAnimations.RemoveAt(i);
                        i--;
                    }
                }
                else if (info.IsFinished == false)
                {
                    return QueueState.Playing;
                }
            }
            
            foreach (CharacterStateMachine character in combatManager.Characters.GetAllFixed())
            {
                if (character.StateEvaluator.PureEvaluate() is not CharacterState.Defeated && character.Display.TrySome(out DisplayModule display) && display.IsBusy)
                    return QueueState.Playing;
            }

            if (_priorityQueue.Count > 0 || _queuedActions.Count > 0 || statusVfxManager.AnyPending())// || _queuedLustPrompts.Count > 0)
                return QueueState.Queued;
            
            return QueueState.Idle;
        }

        /// <returns>True if Busy</returns>
        public bool Tick()
        {
            QueueState state = EvaluateState();
            if (state == QueueState.Idle)
                return false;
            
            if (state == QueueState.Playing)
                return true;
            
            _currentVFXAnimations.Clear();
            Option<AnimationRoutineInfo> nextPriority = _priorityQueue.Dequeue();
            if (nextPriority.IsSome)
                while (true)
                {
                    if (nextPriority.Value.StartIfValid())
                    {
                        _currentVFXAnimations.Add(nextPriority.Value);
                        return true;
                    }
                    
                    nextPriority = _priorityQueue.Dequeue();
                    if (nextPriority.IsNone)
                        break;
                }

            if (statusVfxManager.PlayPendingCues())
                return true;

            Option<IActionSequence> nextAction = _queuedActions.Dequeue();
            if (nextAction.IsSome)
            {
                CurrentAction = nextAction.Value;
                CurrentAction.Play(announce: true);
                return true;
            }

            return false;
        }

        public void PlayQueuedActionImmediate(IActionSequence actionSequence)
        {
            if (_priorityQueue.Count > 0 
             || _currentVFXAnimations.Count > 0
             || _queuedActions.Peek().TrySome(out IActionSequence firstOnQueue) == false || firstOnQueue != actionSequence)
            {
                Debug.LogWarning("Trying to play an action that is not the first on queue, this is not allowed", context: this);
                return;
            }
            
            _queuedActions.Dequeue();
            
            CurrentAction = actionSequence;
            CurrentAction.Play(announce: false);
        }

        public void CancelActionsOfCharacter(CharacterStateMachine character)
        {
            for (int i = 0; i < _queuedActions.Count; i++)
            {
                IActionSequence action = _queuedActions[i];
                if (action.Caster != character)
                    continue;
                
                action.ForceStop();
                _queuedActions.RemoveAt(i);
                i--;
            }
        }

        public void Enqueue(IActionSequence action) => _queuedActions.Enqueue(action);

        public void PriorityEnqueue(AnimationRoutineInfo vfxAnimation) => _priorityQueue.Insert(index: 0, vfxAnimation);
    }
}