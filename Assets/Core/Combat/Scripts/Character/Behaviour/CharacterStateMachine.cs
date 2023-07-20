using System;
using System.Collections.Generic;
using System.Linq;
using Core.Combat.Scripts.Animations;
using Core.Combat.Scripts.Barks;
using Core.Combat.Scripts.Behaviour.Modules;
using Core.Combat.Scripts.Effects;
using Core.Combat.Scripts.Effects.Types.Mist;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Interfaces;
using Core.Combat.Scripts.Managers;
using Core.Combat.Scripts.Perks;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Timeline;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Collections;
using Core.Utils.Collections.Extensions;
using Core.Utils.Extensions;
using Core.Utils.Math;
using Core.Utils.Patterns;
using JetBrains.Annotations;
using UnityEngine;

// ReSharper disable Unity.NoNullPropagation

namespace Core.Combat.Scripts.Behaviour
{
    public class CharacterStateMachine : IEquatable<CharacterStateMachine>
    {
        public CharacterStateMachine([NotNull] ICharacterScript script, DisplayModule display, Guid guid, bool isLeftSide, bool mistExists, CombatSetupInfo.RecoveryInfo recoveryInfo)
        {
            Script = script;
            Guid = guid;

            PositionHandler = DefaultPositionHandler.FromInitialSetup(owner: this, isLeftSide);
            Events = new DefaultEventsHandler();
            SkillModule = new DefaultSkillModule(owner: this);
            StaminaModule = DefaultStaminaModule.FromInitialSetup(this);
            StatsModule = DefaultStatsModule.FromInitialSetup(this);
            ResistancesModule = DefaultResistancesModule.FromInitialSetup(this);
            RecoveryModule = DefaultRecoveryModule.FromInitialSetup(owner: this, recoveryInfo);
            ChargeModule = DefaultChargeModule.FromInitialSetup(this);
            StunModule = DefaultStunModule.FromInitialSetup(this);
            StatusReceiverModule = new DefaultStatusReceiverModule(owner:this);
            StatusApplierModule = DefaultStatusApplierModule.FromInitialSetup(this);
            PerksModule = new DefaultPerksModule(owner: this);
            AIModule = new DefaultAIModule(owner: this);

            if (Script.CanActAsGirl)
            {
                LustModule = DefaultLustModule.FromInitialSetup(this);
                DownedModule = DefaultDownedModule.FromInitialSetup(this);
                if (mistExists)
                    MistStatus.CreateInstance(duration: TSpan.MaxValue, isPermanent: true, owner: this);
            }
            else
            {
                LustModule = Option.None;
                DownedModule = Option.None;
            }
            
            if (display != null)
            {
                Display = Option<DisplayModule>.Some(display);
                display.SetStateMachine(this);
            }
            else
            {
                Debug.LogWarning("Character display is null, this will likely lead to many bugs");
            }

            ReadOnlySpan<IPerk> perkScriptables = script.GetStartingPerks;
            foreach (IPerk perk in perkScriptables)
                perk.CreateInstance(character: this);

            StateEvaluator = DefaultStateEvaluator.FromInitialSetup(this);
            if (Display.IsSome)
            {
                CharacterState state = StateEvaluator.PureEvaluate();
                Display.Value.MatchAnimationWithState(state);
            }

            ForceUpdateDisplay();
        }

        private CharacterStateMachine([NotNull] CharacterRecord record, ICharacterScript script, DisplayModule display)
        {
            Script = script;
            Guid = record.Guid;
            AIModule = record.AIModule.Deserialize(owner: this);
            Events = record.EventsModule.Deserialize(owner: this);
            SkillModule = record.SkillModule.Deserialize(owner: this);
            PositionHandler = record.PositionModule.Deserialize(owner: this);
            StaminaModule = StaminaModule != null ? Option<IStaminaModule>.Some(record.StaminaModule.Deserialize(owner: this)) : Option.None;
            StatsModule = record.StatsModule.Deserialize(owner: this);
            ResistancesModule = record.ResistancesModule.Deserialize(owner: this);
            RecoveryModule = record.RecoveryModule.Deserialize(owner: this);
            ChargeModule = record.ChargeModule.Deserialize(owner: this);
            StunModule = record.StunModule.Deserialize(owner: this);

            if (display != null)
            {
                Display = Option<DisplayModule>.Some(display);
                display.SetStateMachine(this);
            }
            else
            {
                Debug.LogWarning("Character display is null");
            }

            StatusReceiverModule = record.StatusReceiverModule.Deserialize(owner: this);
            StatusApplierModule = record.StatusApplierModule.Deserialize(owner: this);
            PerksModule = record.PerksModule.Deserialize(owner: this);
            LustModule = record.LustModule != null ? Option<ILustModule>.Some(record.LustModule.Deserialize(owner: this)) : Option.None;
            DownedModule = record.DownedModule != null ? Option<IDownedModule>.Some(record.DownedModule.Deserialize(owner: this)) : Option.None;
            StateEvaluator = record.StateEvaluatorModule.Deserialize(owner: this);

            if (Display.IsSome)
            {
                CharacterState state = StateEvaluator.PureEvaluate();
                Display.Value.MatchAnimationWithState(state);
            }

            ForceUpdateDisplay();
        }

        public static Option<CharacterStateMachine> FromRecord([NotNull] CharacterRecord record, DisplayModule characterDisplay)
        {
            Option<CharacterScriptable> script = CharacterDatabase.GetCharacter(record.ScriptKey);
            if (script.IsNone)
                return Option.None;
            
            return new CharacterStateMachine(record, script.Value, characterDisplay);
        }

        public Option<DisplayModule> Display { get; private set; }
        public Guid Guid { get; }
        public ICharacterScript Script { get; }
        public IStateEvaluator StateEvaluator { get; }
        public IEventsHandler Events { get; }

        public IPositionHandler PositionHandler { get; }

        public Option<IStaminaModule> StaminaModule { get; }
        public Option<ILustModule> LustModule { get; }
        public Option<IDownedModule> DownedModule { get; }
        
        public IStatsModule StatsModule { get; }
        public IResistancesModule ResistancesModule { get; }

        public ISkillModule SkillModule { get; }
        public IChargeModule ChargeModule { get; }
        public IRecoveryModule RecoveryModule { get; }
        public IStunModule StunModule { get; }

        public IPerksModule PerksModule { get; }
        public IStatusReceiverModule StatusReceiverModule { get; }
        public IStatusApplierModule StatusApplierModule { get; }

        public IAIModule AIModule { get; }
        public List<ITick> SubscribedTickers { get; } = new();
        
        /// <summary> If this is some, the character's stats (such as lust) need to be synced with the Save system whenever they change. </summary>
        public Option<IReadonlyCharacterStats> OutsideCombatSyncedStats { get; private set; }

        public void SyncStats(IReadonlyCharacterStats source)
        {
            OutsideCombatSyncedStats = Option<IReadonlyCharacterStats>.Some(source);
            if (LustModule.TrySome(out ILustModule lustModule))
                lustModule.SetLust(source.Lust);
        }

        public void PrimaryTick(TSpan timeStep)
        {
            CharacterState characterState = StateEvaluator.PureEvaluate();
            if (characterState is CharacterState.Defeated)
            {
                Debug.LogWarning($"Character {Script.CharacterName} is Defeated, should not try to tick");
                return;
            }
            
            if (characterState is CharacterState.Corpse or CharacterState.Grappled or CharacterState.Grappling)
                return;
            
            if (Display.AssertSome(out DisplayModule display) == false)
                return;
            
            if (DownedModule.TrySome(out IDownedModule downedModule) && downedModule.Tick(ref timeStep))
                return;

            if (StunModule.Tick(ref timeStep))
                return;

            if (LustModule.IsSome)
                LustModule.Value.Tick(timeStep); // NOT Ref lust module does not consume timeStep

            if (RecoveryModule.Tick(ref timeStep))
                return;

            if (ChargeModule.Tick(ref timeStep))
                return;

            if (SkillModule.PlannedSkill.TrySome(out PlannedSkill plannedSkill) && plannedSkill is { Enqueued: false })
            {
                plannedSkill.Enqueue();
            }
            else if ((SkillModule.PlannedSkill.IsNone || SkillModule.PlannedSkill.Value.IsDoneOrCancelled) && Display.IsSome)
            {
                if (CombatManager.DEBUGMODE || Script.IsControlledByPlayer)
                    Display.Value.CombatManager.InputHandler.PlayerControlledCharacterIdle(this);
                else
                    AIModule.Heuristic();
            }
        }

        public void SecondaryTick(in TSpan timeStep)
        {
            StatusReceiverModule.Tick(timeStep);
            foreach (ITick tick in SubscribedTickers.FixedEnumerate())
                tick.Tick(timeStep);
        }

        public void AfterTickUpdate(in TSpan timeStep)
        {
            (CharacterState previous, CharacterState current) = StateEvaluator.OncePerTickStateEvaluation();
            foreach (IModule module in new ModulesEnumerator(this))
                module.AfterTickUpdate(timeStep, previous, current);
        }

        public void ForceUpdateDisplay()
        {
            if (Display.AssertSome(out DisplayModule display) == false)
                return;

            if (StaminaModule.IsSome)
                StaminaModule.Value.ForceUpdateDisplay(display);
            
            if (LustModule.IsSome)
                LustModule.Value.ForceUpdateDisplay(display);
            
            if (DownedModule.IsSome)
                DownedModule.Value.ForceUpdateDisplay(display);

            StunModule.ForceUpdateDisplay(display);
            RecoveryModule.ForceUpdateDisplay(display);
            ChargeModule.ForceUpdateDisplay(display);
            ForceUpdateTimelineCue();

            display.CheckIndicators();
        }

        public void AfterSkillDisplayUpdate()
        {
            if (Display.TrySome(out DisplayModule display) == false)
                return;
            
            if (StaminaModule.IsSome)
                StaminaModule.Value.ForceUpdateDisplay(display);
            
            if (LustModule.IsSome)
                LustModule.Value.ForceUpdateDisplay(display);
            
            if (DownedModule.IsSome)
                DownedModule.Value.ForceUpdateDisplay(display);
            
            RecoveryModule.ForceUpdateDisplay(display);
            ChargeModule.ForceUpdateDisplay(display);
            ForceUpdateTimelineCue();
            
            display.CheckIndicators();
        }

        public void FillTimelineEvents(in SelfSortingList<CombatEvent> events)
        {
            TSpan currentTime = TSpan.FromTicks(0);
            
            if (DownedModule.TrySome(out IDownedModule downedModule) && downedModule.GetRemaining().Ticks > 0)
            {
                currentTime += downedModule.GetEstimatedRemaining();
                events.Add(CombatEvent.FromDownedEnd(owner: this, currentTime));
            }
            
            if (StunModule.GetRemaining().Ticks > 0)
            {
                currentTime += StunModule.GetEstimatedRemaining();
                events.Add(CombatEvent.FromStunEnd(owner: this, currentTime));
            }
            
            if (RecoveryModule.GetRemaining().Ticks > 0)
            {
                currentTime += RecoveryModule.GetEstimatedRemaining();
                events.Add(CombatEvent.FromTurn(owner: this, currentTime));
            }
            
            if (ChargeModule.GetRemaining().Ticks > 0 && SkillModule.PlannedSkill.AssertSome(out PlannedSkill plannedSkill))
            {
                currentTime += ChargeModule.GetEstimatedRemaining();
                events.Add(CombatEvent.FromAction(owner: this, currentTime, plannedSkill));
            }

            StatusReceiverModule.FillTimelineEvents(events);
        }

        public void ForceUpdateTimelineCue()
        {
            //!todo fix this mess
            /*if (Display.IsNone)
                return;

            CharacterDisplay display = Display.Value;
            if (DownedModule.TrySome(out IDownedModule downedModule) && downedModule.GetRemaining() > 0)
                display.SetTimelineCuePosition(downedModule.GetEstimatedRemaining(), "Gets up", ColorReferences.Heal);
            else if (StunModule.GetRemaining() > 0)
                display.SetTimelineCuePosition(StunModule.GetEstimatedRemaining(), "Stun ends", ColorReferences.Stun);
            else if (RecoveryModule.GetRemaining() > 0)
                display.SetTimelineCuePosition(RecoveryModule.GetEstimatedRemaining(), "Recovery ends", ColorReferences.Recovery);
            else if (ChargeModule.GetRemaining() > 0)
            {
                string label;
                Color color;
                if ((CombatManager.DEBUGMODE || PositionHandler.IsLeftSide) && SkillModule.PlannedSkill.TrySome(out PlannedSkill plannedSkill))
                {
                    color = plannedSkill.Skill.AllowAllies ? ColorReferences.Buff : ColorReferences.Debuff;
                    label = $"{plannedSkill.Skill.DisplayName}=>{plannedSkill.Target.Script.CharacterName}";
                }
                else
                {
                    label = "Action";
                    color = ColorReferences.Damage;
                }
                
                display.SetTimelineCuePosition(ChargeModule.GetEstimatedRemaining(), label, color);
            }
            else
            {
                display.AllowTimelineIcon(false);
            }*/
        }

        public void OnZeroStamina()
        {
            if (DownedModule.IsSome && DownedModule.Value.HandleZeroStamina())
                return;

            StateEvaluator.OutOfForces();
        }

        public void WasTargetedDuringSkillAnimation(ActionResult result, bool eligibleForHitAnimation, CharacterState stateBeforeAction)
        {
#if UNITY_EDITOR
            if (Display.TrySome(out DisplayModule self) && self.hasWire)
                Debug.Log("Wired!");
#endif
            
            if (Display.IsNone)
                return;

            DisplayModule display = Display.Value;
            if (display.AnimationStatus is not AnimationStatus.Common) // something else is controlling the animation so we don't want to override it
                return;

            if (stateBeforeAction is CharacterState.Defeated)
                return;
            
            if (StaminaModule.IsNone || StaminaModule.Value.GetCurrent() > 0 || result.Missed)
            {
                CheckHitAnimation();
                return;
            }

            display.CombatManager.Animations.CancelActionsOfCharacter(character: this);
            CombatAnimation corpseAnimation = default;
            AnimationStatus desiredStatus;
            if (DownedModule.IsSome && DownedModule.Value.CanHandleNextZeroStamina())
                desiredStatus = AnimationStatus.Downed;
            else if (Script.BecomesCorpseOnDefeat(out corpseAnimation))
                desiredStatus = AnimationStatus.Corpse;
            else
                desiredStatus = AnimationStatus.Defeated;
            
            IActionSequence currentAction = display.CombatManager.Animations.CurrentAction;
            bool willRiposteActivate = currentAction is { IsPlaying: true } && currentAction.Caster != this && currentAction.Targets != null && currentAction.Targets.Contains(this) && StatusReceiverModule.HasActiveStatusOfType(EffectType.Riposte);

            switch (desiredStatus)
            {
                case AnimationStatus.Downed:
                {
                    CheckHitAnimation();
                    if (willRiposteActivate)
                        display.AnimateDownedAfterRiposte();
                    else
                        display.AnimateDowned();
                    break;
                }
                case AnimationStatus.Corpse:
                {
                    CheckHitAnimation();
                    if (willRiposteActivate)
                        display.AnimateCorpseAfterRiposte(corpseAnimation);
                    else
                        display.AnimateCorpse(corpseAnimation);
                    break;
                }
                case AnimationStatus.Defeated when willRiposteActivate:
                {
                    CheckHitAnimation();
                    display.AnimateDefeatedAfterRiposte();
                    break;
                }
                case AnimationStatus.Defeated:
                    display.AnimateDefeated(onFinish: null);
                    break;
            }

            void CheckHitAnimation()
            {
                if (eligibleForHitAnimation)
                {
                    TargetContext targetContext = new(result);
                    CombatAnimation hitAnimation = new(CombatAnimation.Param_Hit, Option<CasterContext>.None, Option<TargetContext>.Some(targetContext));
                    display.SetAnimationWithoutNotifyStatus(hitAnimation);
                }
            }
        }

        public void PlayBark(BarkType barkType, CharacterStateMachine otherCharacter, bool calculateProbability)
        {
            if (Display.IsNone)
                return;
            
            if (Script.GetBark(barkType, otherCharacter).TrySome(out string bark))
                Display.Value.BarkPlayer.EnqueueBark(barkType, bark, calculateProbability);
        }

        public void PlayBark(BarkType barkType)
        {
            PlayBark(barkType, otherCharacter: this, calculateProbability: true);
        }

        public void Unsubscribe()
        {
            StatusReceiverModule.RemoveAll();
            PerksModule.RemoveAll();
        }

        public bool Equals(CharacterStateMachine other) => EqualityComparer<CharacterStateMachine>.Default.Equals(this, other);


        public void DisplayDestroyed()
        {
            Display = Option<DisplayModule>.None;
        }
    }
}