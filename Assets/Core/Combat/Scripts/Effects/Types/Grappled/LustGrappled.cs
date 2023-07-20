using System;
using System.Collections;
using Core.Combat.Scripts.Animations;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Behaviour.Modules;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Managers;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Skills.Action;
using Core.Utils.Async;
using Core.Utils.Math;
using JetBrains.Annotations;
using UnityEngine;
using Option = Core.Utils.Patterns.Option;

namespace Core.Combat.Scripts.Effects.Types.Grappled
{
    public class LustGrappled : StatusInstance
    {
        public override bool IsPositive => false;
        
        public CharacterStateMachine Restrainer { get; }
        public readonly string TriggerName;
        // ReSharper disable once NotAccessedField.Local
        private readonly string _cumTriggerName;
        public readonly float GraphicalX;
        
        private readonly int _lustPerSecond;
        private readonly int _temptationDeltaPerSecond;
        
        private TSpan _accumulatedTime;
        private bool _cumAnimationQueued;

        private LustGrappled(TSpan duration, bool isPermanent, CharacterStateMachine owner, CharacterStateMachine restrainer, int lustPerTime,
                             int temptationDeltaPerTime, string triggerName, float graphicalX, string cumTriggerName)
            : base(duration, isPermanent, owner)
        {
            Restrainer = restrainer;
            _lustPerSecond = lustPerTime;
            _temptationDeltaPerSecond = temptationDeltaPerTime;
            TriggerName = triggerName;
            GraphicalX = graphicalX;
            _cumTriggerName = cumTriggerName;
        }

        public static Utils.Patterns.Option<StatusInstance> CreateInstance(TSpan duration, bool isPermanent, CharacterStateMachine owner, CharacterStateMachine restrainer,
                                                                           int lustPerTime, int temptationDeltaPerTime, string triggerName, float graphicalX)
        {
            if ((duration.Ticks <= 0 && !isPermanent) || lustPerTime <= 0)
            {
                Debug.LogWarning($"Invalid parameters for creating a {nameof(LustGrappled)} effect, duration: {duration.Seconds.ToString()}, isPermanent: {isPermanent.ToString()}, lustPerTime: {lustPerTime.ToString()}");
                return Option.None;
            }

            if (owner.Display.AssertSome(out DisplayModule ownerDisplay) == false || restrainer.Display.AssertSome(out DisplayModule restrainerDisplay) == false)
                return Option.None;

            LustGrappled instance = new(duration, isPermanent, owner, restrainer, lustPerTime, temptationDeltaPerTime, triggerName, graphicalX, cumTriggerName: $"{triggerName}_cum");
            owner.StatusReceiverModule.AddStatus(instance, restrainer);
            restrainer.StatusReceiverModule.TrackRelatedStatus(instance);
            
            ownerDisplay.CombatManager.Characters.NotifyGrappled(owner);
            ownerDisplay.CombatManager.Characters.NotifyGrappling(restrainer, target: owner);

            if (ownerDisplay.CombatManager.Animations.CurrentAction is { IsPlaying: true }) // grappled was likely the result of a temptation
            {
                ownerDisplay.AnimateGrappled();
                CombatAnimation animation = new(triggerName, Utils.Patterns.Option<CasterContext>.None, Utils.Patterns.Option<TargetContext>.None);
                restrainerDisplay.AnimateGrapplingInsideTemptSkill(animation);
            }
            else
            {
                Action underTheMist = () =>
                {
                    ownerDisplay.AnimateGrappled();
                    CombatAnimation animation = new(triggerName, Utils.Patterns.Option<CasterContext>.None, Utils.Patterns.Option<TargetContext>.None);
                    restrainerDisplay.AnimateGrappling(animation);
                };

                ownerDisplay.CombatManager.ActionAnimator.AnimateOverlayMist(underTheMist, Utils.Patterns.Option<CharacterStateMachine>.Some(owner));
            }
            
            return Utils.Patterns.Option<StatusInstance>.Some(instance);
        }

        public LustGrappled([NotNull] LustGrappledRecord record, float graphicalX, CharacterStateMachine owner, CharacterStateMachine restrainer) : base(record, owner)
        {
            Restrainer = restrainer;
            _lustPerSecond = record.LustPerTime;
            _temptationDeltaPerSecond = record.TemptationDeltaPerTime;
            _accumulatedTime = record.AccumulatedTime;
            TriggerName = record.TriggerName;
            GraphicalX = graphicalX;
            _cumTriggerName = $"{TriggerName}_cum";
        }

        public override void Tick(TSpan timeStep)
        {
            if (Duration > timeStep)
            {
                ILustModule lustModule;
                _accumulatedTime += timeStep;
                if (_accumulatedTime.Seconds >= 1)
                {
                    int roundSeconds = _accumulatedTime.Seconds.FloorToInt();
                    _accumulatedTime.SubtractSeconds(roundSeconds);
                    if (Owner.LustModule.TrySome(out lustModule))
                    {
                        lustModule.ChangeLust(roundSeconds * _lustPerSecond);
                        lustModule.ChangeTemptation(roundSeconds * _temptationDeltaPerSecond);
                        lustModule.IncrementSexualExp(Restrainer.Script.Race, ILustModule.SexualExpDeltaPerSexSecond * roundSeconds);
                    }
                }

                if (Owner.LustModule.TrySome(out lustModule) && lustModule.GetLust() >= ILustModule.MaxLust)
                    lustModule.Orgasm();

                base.Tick(timeStep);
                return;
            }

            // Duration is over so the monster should cum
            if (Owner.Display.AssertSome(out DisplayModule display) == false)
            {
                RequestDeactivation();
                return;
            }

            _accumulatedTime += Duration;
            if (Owner.LustModule.IsSome)
            {
                Owner.LustModule.Value.ChangeLust((_accumulatedTime.Seconds * _lustPerSecond).CeilToInt());
                Owner.LustModule.Value.ChangeTemptation((_accumulatedTime.Seconds * _temptationDeltaPerSecond).CeilToInt());
            }

            _accumulatedTime.Ticks = 0;
            if (_cumAnimationQueued)
            {
                Debug.LogWarning("Cum animation already queued");
                RequestDeactivation();
                return;
            }
            
            if (Restrainer.Display.AssertSome(out DisplayModule restrainer) == false)
                return;

            _cumAnimationQueued = true;
            CombatManager combatManager = display.CombatManager;
            CoroutineWrapper cumRoutine = new(CumAnimationRoutine(restrainer), nameof(CumAnimationRoutine), context: restrainer, autoStart: false);
            AnimationRoutineInfo animationRoutineInfo = AnimationRoutineInfo.WithCharacter(cumRoutine, Owner, AnimationRoutineInfo.NoValidation);
            combatManager.Animations.PriorityEnqueue(animationRoutineInfo);
        }

        private IEnumerator CumAnimationRoutine(DisplayModule restrainer)
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
                Restrainer.StatusReceiverModule.UntrackRelatedStatus(this);
                if (Owner.Display.TrySome(out DisplayModule casterDisplay))
                    casterDisplay.MatchAnimationWithState(Owner.StateEvaluator.PureEvaluate());

                if (Restrainer.Display.TrySome(out DisplayModule restrainerDisplay))
                    restrainerDisplay.MatchAnimationWithState(Restrainer.StateEvaluator.PureEvaluate());
            };

            AnimationRoutineInfo info = restrainer.CombatManager.ActionAnimator.AnimateOverlayMist(underTheMist, Utils.Patterns.Option<CharacterStateMachine>.Some(Restrainer));
            while (info.IsFinished == false)
                yield return null;
        }

        public override void RequestDeactivation()
        {
            if (IsDeactivated)
                return;
            
            base.RequestDeactivation();
            Restrainer.StatusReceiverModule.UntrackRelatedStatus(this);
            if (Owner.Display.TrySome(out DisplayModule casterDisplay))
                casterDisplay.DeAnimateGrappled(Restrainer);
            else if (Restrainer.Display.TrySome(out DisplayModule restrainerDisplay))
                restrainerDisplay.MatchAnimationWithState(Restrainer.StateEvaluator.PureEvaluate());
        }

        public override void CharacterDefeated(CharacterStateMachine character, bool becomesCorpseOnDefeat)
        {
            if (character == Owner)
            {
                Duration = new TSpan(ticks: long.MaxValue / 2);
                Permanent = true;
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
            Restrainer.StatusReceiverModule.UntrackRelatedStatus(this);
            
            if (Owner.DownedModule.TrySome(out IDownedModule downedModule))
                downedModule.SetInitial(IDownedModule.DefaultDownedDurationOnGrappleRelease);
            
            if (Owner.Display.TrySome(out DisplayModule casterDisplay))
                casterDisplay.DeAnimateGrappledFromStunnedRestrainer(Restrainer);
            else if (Restrainer.Display.TrySome(out DisplayModule restrainerDisplay))
                restrainerDisplay.MatchAnimationWithState(Restrainer.StateEvaluator.PureEvaluate());
        }

        [NotNull]
        public override StatusRecord GetRecord() => new LustGrappledRecord(Duration, Permanent, _lustPerSecond, _temptationDeltaPerSecond, _accumulatedTime, TriggerName, Restrainer.Guid);

        public override Utils.Patterns.Option<string> GetDescription() => StatusInstanceDescriptions.Get(this);
        public override EffectType EffectType => EffectType.LustGrappled;
        public const int GlobalId = Riposte.Riposte.GlobalId + 2; // +2 because stealth was removed
    }
}