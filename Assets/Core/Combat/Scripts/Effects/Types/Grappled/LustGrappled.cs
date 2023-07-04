using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts.Animations;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Combat.Scripts.Managers;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Skills.Action;
using UnityEngine;
using Utils.Async;
using Utils.Patterns;
using Utils.Extensions;
using Utils.Math;

namespace Core.Combat.Scripts.Effects.Types.Grappled
{
    public record LustGrappledRecord(float Duration, bool IsPermanent, uint LustPerTime, float TemptationDeltaPerTime, float AccumulatedTime, string TriggerName, Guid Restrainer) : StatusRecord(Duration, IsPermanent)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (string.IsNullOrEmpty(TriggerName))
            {
                errors.AppendLine("Invalid ", nameof(LustGrappledRecord), " data. ", nameof(TriggerName), " cannot be null or empty.");
                return false;
            }

            foreach (CharacterRecord character in allCharacters)
            {
                if (Restrainer == character.Guid)
                    return true;
            }
            
            errors.AppendLine("Invalid ", nameof(LustGrappledRecord), " data. ", nameof(Restrainer), "'s Guid: ", Restrainer.ToString(), " could not be mapped to a character.");
            return false;
        }
    }

    public class LustGrappled : StatusInstance
    {
        public override bool IsPositive => false;
        
        public CharacterStateMachine Restrainer { get; }
        public readonly string TriggerName;
        private readonly string _cumTriggerName;
        public readonly float GraphicalX;
        
        private readonly uint _lustPerTime;
        private readonly float _temptationDeltaPerTime;
        
        private float _accumulatedTime;
        private bool _cumAnimationQueued;

        private LustGrappled(float duration, bool isPermanent, CharacterStateMachine owner, CharacterStateMachine restrainer, uint lustPerTime, float temptationDeltaPerTime, string triggerName, float graphicalX, string cumTriggerName)
            : base(duration, isPermanent, owner)
        {
            Restrainer = restrainer;
            _lustPerTime = lustPerTime;
            _temptationDeltaPerTime = temptationDeltaPerTime;
            TriggerName = triggerName;
            GraphicalX = graphicalX;
            _cumTriggerName = cumTriggerName;
        }

        public static Option<StatusInstance> CreateInstance(float duration, bool isPermanent, CharacterStateMachine owner, CharacterStateMachine restrainer,
                                                            uint lustPerTime, float temptationDeltaPerTime, string triggerName, float graphicalX)
        {
            if ((duration <= 0 && !isPermanent) || lustPerTime <= 0)
            {
                Debug.LogWarning($"Invalid parameters for creating a {nameof(LustGrappled)} effect, duration: {duration.ToString()}, isPermanent: {isPermanent.ToString()}, lustPerTime: {lustPerTime.ToString()}");
                return Option.None;
            }

            if (owner.Display.AssertSome(out CharacterDisplay ownerDisplay) == false || restrainer.Display.AssertSome(out CharacterDisplay restrainerDisplay) == false)
                return Option.None;

            LustGrappled instance = new(duration, isPermanent, owner, restrainer, lustPerTime, temptationDeltaPerTime, triggerName, graphicalX, cumTriggerName: $"{triggerName}_cum");
            owner.StatusModule.AddStatus(instance, restrainer);
            restrainer.StatusModule.TrackRelatedStatus(instance);
            
            ownerDisplay.CombatManager.Characters.NotifyGrappled(owner);
            ownerDisplay.CombatManager.Characters.NotifyGrappling(restrainer, target: owner);

            if (ownerDisplay.CombatManager.Animations.CurrentAction is { IsPlaying: true }) // grappled was likely the result of a temptation
            {
                ownerDisplay.AnimateGrappled();
                CombatAnimation animation = new(triggerName, Option<CasterContext>.None, Option<TargetContext>.None);
                restrainerDisplay.AnimateGrapplingInsideTemptSkill(animation);
            }
            else
            {
                Action underTheMist = () =>
                {
                    ownerDisplay.AnimateGrappled();
                    CombatAnimation animation = new(triggerName, Option<CasterContext>.None, Option<TargetContext>.None);
                    restrainerDisplay.AnimateGrappling(animation);
                };

                ownerDisplay.CombatManager.ActionAnimator.AnimateOverlayMist(underTheMist, Option<CharacterStateMachine>.Some(owner));
            }
            
            return Option<StatusInstance>.Some(instance);
        }

        private LustGrappled(LustGrappledRecord record, float graphicalX, CharacterStateMachine owner, CharacterStateMachine restrainer) : base(record, owner)
        {
            Restrainer = restrainer;
            _lustPerTime = record.LustPerTime;
            _temptationDeltaPerTime = record.TemptationDeltaPerTime;
            _accumulatedTime = record.AccumulatedTime;
            TriggerName = record.TriggerName;
            GraphicalX = graphicalX;
            _cumTriggerName = $"{TriggerName}_cum";
        }

        public static Option<StatusInstance> CreateInstance(LustGrappledRecord record, CharacterStateMachine owner, ref CharacterEnumerator allCharacters)
        {
            CharacterStateMachine restrainer = null;
            foreach (CharacterStateMachine character in allCharacters)
            {
                if (character.Guid == record.Restrainer)
                {
                    restrainer = character;
                    break;
                }
            }
            
            if (restrainer == null)
                return Option.None;

            Option<float> graphicalX = owner.Script.GetSexGraphicalX(record.TriggerName);
            if (graphicalX.IsNone)
                return Option.None;

            LustGrappled instance = new(record, graphicalX.Value, owner, restrainer);
            owner.StatusModule.AddStatus(instance, restrainer);
            restrainer.StatusModule.TrackRelatedStatus(instance);
            
            if (owner.Display.TrySome(out CharacterDisplay ownerDisplay))
                ownerDisplay.AnimateGrappled();

            if (restrainer.Display.TrySome(out CharacterDisplay restrainerDisplay))
            {
                CombatAnimation animation = new(record.TriggerName, Option<CasterContext>.None, Option<TargetContext>.None);
                restrainerDisplay.AnimateGrappling(animation);
            }
            
            return Option<StatusInstance>.Some(instance);
        }

        public override void Tick(float timeStep)
        {
            if (Duration > timeStep)
            {
                _accumulatedTime += timeStep;
                if (_accumulatedTime >= 1f)
                {
                    uint roundTime = _accumulatedTime.FloorToUInt();
                    _accumulatedTime -= roundTime;
                    if (Owner.LustModule.TrySome(out ILustModule lustModule))
                    {
                        lustModule.ChangeLust((int)(roundTime * _lustPerTime));
                        lustModule.ChangeTemptation(roundTime * _temptationDeltaPerTime);
                        lustModule.IncrementSexualExp(Restrainer.Script.Race, ILustModule.SexualExpDeltaPerSexSecond * roundTime);
                    }
                }

                {
                    if (Owner.LustModule.TrySome(out ILustModule lustModule) && lustModule.GetLust() >= ILustModule.MaxLust)
                        lustModule.Orgasm();
                }

                base.Tick(timeStep);
                return;
            }

            // Duration is over so the monster should cum
            if (Owner.Display.AssertSome(out CharacterDisplay display) == false)
            {
                RequestDeactivation();
                return;
            }

            _accumulatedTime += Duration;
            if (Owner.LustModule.IsSome)
            {
                Owner.LustModule.Value.ChangeLust(Mathf.CeilToInt(_accumulatedTime * _lustPerTime));
                Owner.LustModule.Value.ChangeTemptation(_accumulatedTime * _temptationDeltaPerTime);
            }

            _accumulatedTime = 0f;
            if (_cumAnimationQueued)
            {
                Debug.LogWarning("Cum animation already queued");
                RequestDeactivation();
                return;
            }
            
            if (Restrainer.Display.AssertSome(out CharacterDisplay restrainer) == false)
                return;

            _cumAnimationQueued = true;
            CombatManager combatManager = display.CombatManager;
            CoroutineWrapper cumRoutine = new(CumAnimationRoutine(restrainer), nameof(CumAnimationRoutine), context: restrainer, autoStart: false);
            AnimationRoutineInfo animationRoutineInfo = AnimationRoutineInfo.WithCharacter(cumRoutine, Owner, AnimationRoutineInfo.NoValidation);
            combatManager.Animations.PriorityEnqueue(animationRoutineInfo);
        }

        private IEnumerator CumAnimationRoutine(CharacterDisplay restrainer)
        {
            if (restrainer == null)
                yield break;

            // commented because we don't have cum animations yet
            /*const float zoomDuration = 0.5f;
            Vector3 animatorPosition = restrainer.AnimatorTransform.TrySome(out Transform animTrans) ? animTrans.position : restrainer.transform.position;
            CoroutineWrapper cameraAnimation = restrainer.CombatManager.CameraZoomAt(zoomLevel: 1.5f, animatorPosition, zoomDuration, stayDuration: 5f);
            cameraAnimation.Start();
            
            float endTime = Time.time + zoomDuration;
            while (Time.time < endTime)
                yield return null;
            
            if (restrainer != null)
            {
                CombatAnimation animation = new(_cumTriggerName, Option<CasterContext>.None, Option<TargetContext>.None);
                restrainer.SetAnimationWithoutNotifyStatus(animation);
            }

            yield return cameraAnimation;*/

            Action underTheMist = () =>
            {
                if (IsDeactivated)
                    return;

                base.RequestDeactivation();
                Restrainer.StatusModule.UntrackRelatedStatus(this);
                if (Owner.Display.TrySome(out CharacterDisplay casterDisplay))
                    casterDisplay.MatchAnimationWithState(Owner.StateEvaluator.PureEvaluate());

                if (Restrainer.Display.TrySome(out CharacterDisplay restrainerDisplay))
                    restrainerDisplay.MatchAnimationWithState(Restrainer.StateEvaluator.PureEvaluate());
            };

            AnimationRoutineInfo info = restrainer.CombatManager.ActionAnimator.AnimateOverlayMist(underTheMist, Option<CharacterStateMachine>.Some(Restrainer));
            while (info.IsFinished == false)
                yield return null;
        }

        public override void RequestDeactivation()
        {
            if (IsDeactivated)
                return;
            
            base.RequestDeactivation();
            Restrainer.StatusModule.UntrackRelatedStatus(this);
            if (Owner.Display.TrySome(out CharacterDisplay casterDisplay))
                casterDisplay.DeAnimateGrappled(Restrainer);
            else if (Restrainer.Display.TrySome(out CharacterDisplay restrainerDisplay))
                restrainerDisplay.MatchAnimationWithState(Restrainer.StateEvaluator.PureEvaluate());
        }

        public override void CharacterDefeated(CharacterStateMachine character, bool becomesCorpseOnDefeat)
        {
            if (character == Owner)
            { 
                Duration = 9999999f;
                IsPermanent = true;
            }
            else if (character == Restrainer)
            {
                RequestDeactivation();
            }
        }

        public void RestrainerStunned()
        {
            Debug.Assert(IsDeactivated == false);

            base.RequestDeactivation();
            Restrainer.StatusModule.UntrackRelatedStatus(this);
            
            if (Owner.DownedModule.TrySome(out IDownedModule downedModule))
                downedModule.SetInitial(IDownedModule.DefaultDownedDurationOnGrappleRelease);
            
            if (Owner.Display.TrySome(out CharacterDisplay casterDisplay))
                casterDisplay.DeAnimateGrappledFromStunnedRestrainer(Restrainer);
            else if (Restrainer.Display.TrySome(out CharacterDisplay restrainerDisplay))
                restrainerDisplay.MatchAnimationWithState(Restrainer.StateEvaluator.PureEvaluate());
        }

        public override StatusRecord GetRecord() => new LustGrappledRecord(Duration, IsPermanent, _lustPerTime, _temptationDeltaPerTime, _accumulatedTime, TriggerName, Restrainer.Guid);

        public override Option<string> GetDescription() => StatusInstanceDescriptions.Get(this);
        public override EffectType EffectType => EffectType.LustGrappled;
        public const int GlobalId = Riposte.Riposte.GlobalId + 2; // +2 because stealth was removed
    }
}