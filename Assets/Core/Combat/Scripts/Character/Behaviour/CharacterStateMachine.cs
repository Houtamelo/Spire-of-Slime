using System;
using System.Collections.Generic;
using System.Linq;
using Core.Combat.Scripts.Animations;
using Core.Combat.Scripts.Barks;
using Core.Combat.Scripts.DefaultModules;
using Core.Combat.Scripts.Effects;
using Core.Combat.Scripts.Effects.Types.Mist;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Interfaces;
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Combat.Scripts.Managers;
using Core.Combat.Scripts.Perks;
using Core.Combat.Scripts.Skills.Action;
using Main_Database.Combat;
using Save_Management;
using UnityEngine;
using Utils.Extensions;
using Utils.Patterns;

// ReSharper disable Unity.NoNullPropagation

namespace Core.Combat.Scripts.Behaviour
{
    public class CharacterStateMachine : IEquatable<CharacterStateMachine>
    {
        public CharacterStateMachine(ICharacterScript script, CharacterDisplay display, Guid guid, bool isLeftSide, bool mistExists, CombatSetupInfo.RecoveryInfo recoveryInfo)
        {
            Script = script;
            Guid = guid;

            PositionHandler = DefaultPositionHandler.FromInitialSetup(this, isLeftSide);
            Events = new DefaultEventsHandler();
            SkillModule = new DefaultSkillModule(this);
            
            StaminaModule = DefaultStaminaModule.FromInitialSetup(this);

            StatsModule = DefaultStatsModule.FromInitialSetup(this);
            ResistancesModule = DefaultResistancesModule.FromInitialSetup(this);
            RecoveryModule = DefaultRecoveryModule.FromInitialSetup(this, recoveryInfo);
            ChargeModule = DefaultChargeModule.FromInitialSetup(this);
            StunModule = DefaultStunModule.FromInitialSetup(this);
            StatusModule = new DefaultStatusModule(this);
            StatusApplierModule = DefaultStatusApplierModule.FromInitialSetup(this);
            PerksModule = new DefaultPerksModule(this);
            AIModule = new DefaultAIModule(this);

            if (Script.CanActAsGirl)
            {
                LustModule = DefaultLustModule.FromInitialSetup(this);
                DownedModule = DefaultDownedModule.FromInitialSetup(this);
                if (mistExists)
                    MistStatus.CreateInstance(duration: 999999, isPermanent: true, owner: this);
            }
            else
            {
                LustModule = Option.None;
                DownedModule = Option.None;
            }
            
            if (display != null)
            {
                Display = Option<CharacterDisplay>.Some(display);
                display.SetStateMachine(this);
            }
            else
            {
                Debug.LogWarning("Character display is null, this will likely lead to many bugs");
            }

            ReadOnlySpan<IPerk> perkScriptables = script.GetStartingPerks;
            foreach (IPerk perk in perkScriptables)
                perk.CreateInstance(character: this);

            StateEvaluator = new DefaultStateEvaluator(this);
            if (Display.IsSome)
            {
                CharacterState state = StateEvaluator.PureEvaluate();
                Display.Value.MatchAnimationWithState(state);
            }

            ForceUpdateDisplay();
        }

        private CharacterStateMachine(CharacterRecord record, ICharacterScript script, CharacterDisplay display)
        {
            Script = script;
            Guid = record.Guid;
            AIModule = new DefaultAIModule(this);
            Events = new DefaultEventsHandler();
            SkillModule = new DefaultSkillModule(this);

            PositionHandler = DefaultPositionHandler.FromRecord(this, record);
            StaminaModule = DefaultStaminaModule.FromRecord(this, record);
            StatsModule = DefaultStatsModule.FromRecord(this, record);
            ResistancesModule = DefaultResistancesModule.FromRecord(this, record);
            RecoveryModule = DefaultRecoveryModule.FromRecord(this, record);
            ChargeModule = DefaultChargeModule.FromRecord(this, record);
            StunModule = DefaultStunModule.FromRecord(this, record);

            if (display != null)
            {
                Display = Option<CharacterDisplay>.Some(display);
                display.SetStateMachine(this);
            }
            else
            {
                Debug.LogWarning("Character display is null");
            }
            
            StatusModule = new DefaultStatusModule(this);
            StatusApplierModule = DefaultStatusApplierModule.FromRecord(this, record);
            PerksModule = new DefaultPerksModule(this);

            if (record.IsGirl)
            {
                LustModule = DefaultLustModule.FromRecord(this, record);
                DownedModule = DefaultDownedModule.FromRecord(this, record);
                DownedModule.Value.SetBoth(record.DownedInitialDuration, record.DownedRemaining);
            }
            else
            {
                LustModule = Option<ILustModule>.None;
                DownedModule = Option<IDownedModule>.None;
            }

            StateEvaluator = new DefaultStateEvaluator(this, record.IsDefeated, record.IsCorpse);

            if (Display.IsSome)
            {
                CharacterState state = StateEvaluator.PureEvaluate();
                Display.Value.MatchAnimationWithState(state);
            }

            ForceUpdateDisplay();
        }

        public static Option<CharacterStateMachine> FromSave(CharacterRecord record, CharacterDisplay characterDisplay)
        {
            Option<CharacterScriptable> script = CharacterDatabase.GetCharacter(record.ScriptKey);
            if (script.IsNone)
                return Option.None;
            
            return new CharacterStateMachine(record, script.Value, characterDisplay);
        }

        public Option<CharacterDisplay> Display { get; private set; }
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
        public IStatusModule StatusModule { get; }
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

        public void PrimaryTick(float timeStep)
        {
            CharacterState characterState = StateEvaluator.PureEvaluate();
            if (characterState is CharacterState.Defeated)
            {
                Debug.LogWarning($"Character {Script.CharacterName} is Defeated, should not try to tick");
                return;
            }
            
            if (characterState is CharacterState.Corpse or CharacterState.Grappled or CharacterState.Grappling)
                return;
            
            if (Display.AssertSome(out CharacterDisplay display) == false)
                return;
            
            if (DownedModule.TrySome(out IDownedModule downedModule) && downedModule.Tick(ref timeStep))
            {
                display.AllowTimelineIcon(true);
                display.SetTimelineCuePosition(downedModule.GetEstimatedRealRemaining(), "Gets up", ColorReferences.Heal);
                return;
            }

            if (StunModule.Tick(ref timeStep))
            {
                display.AllowTimelineIcon(true);
                display.SetTimelineCuePosition(StunModule.GetEstimatedRealRemaining(), "Stun ends", ColorReferences.Stun);
                return;
            }
            
            if (LustModule.IsSome)
            {
                LustModule.Value.Tick(timeStep); // lust module does not consume timeStep
            }

            if (RecoveryModule.Tick(timeStep: ref timeStep))
            {
                display.AllowTimelineIcon(true);
                display.SetTimelineCuePosition(RecoveryModule.GetEstimatedRealRemaining(), "Recovery ends", ColorReferences.Recovery);
                return;
            }

            if (ChargeModule.Tick(timeStep: ref timeStep))
            {
                string label;
                Color color;
                if ((CombatManager.DEBUGMODE || PositionHandler.IsLeftSide) && SkillModule.PlannedSkill.TrySome(out PlannedSkill plan))
                {
                    color = plan.Skill.AllowAllies ? ColorReferences.Buff : ColorReferences.Debuff;
                    label = $"{plan.Skill.DisplayName}=>{plan.Target.Script.CharacterName}";
                }
                else
                {
                    color = ColorReferences.Damage;
                    label = "Action";
                }

                display.AllowTimelineIcon(true);
                display.SetTimelineCuePosition(ChargeModule.GetEstimatedRealRemaining(), label, color);
                return;
            }

            display.AllowTimelineIcon(false);

            if (SkillModule.PlannedSkill.TrySome(out PlannedSkill plannedSkill) && plannedSkill is { Enqueued: false })
                plannedSkill.Enqueue();
            else if ((SkillModule.PlannedSkill.IsNone || SkillModule.PlannedSkill.Value.IsDoneOrCancelled) && Display.IsSome)
            {
                if (CombatManager.DEBUGMODE || Script.IsControlledByPlayer)
                    Display.Value.CombatManager.InputHandler.PlayerControlledCharacterIdle(this);
                else
                    AIModule.Heuristic();
            }
        }

        public void SecondaryTick(in float timeStep)
        {
            StatusModule.Tick(timeStep);
            foreach (ITick tick in SubscribedTickers.FixedEnumerate())
                tick.Tick(timeStep);
        }

        public void AfterTickUpdate(in float timeStep)
        {
            (CharacterState previous, CharacterState current) = StateEvaluator.OncePerTickStateEvaluation();
            foreach (IModule module in new ModulesEnumerator(this))
                module.AfterTickUpdate(timeStep, previous, current);
        }

        public void ForceUpdateDisplay()
        {
            if (Display.AssertSome(out CharacterDisplay display) == false)
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
            if (Display.TrySome(out CharacterDisplay display) == false)
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

        public void ForceUpdateTimelineCue()
        {
            if (Display.IsNone)
                return;

            CharacterDisplay display = Display.Value;
            display.AllowTimelineIcon(true);
            if (DownedModule.TrySome(out IDownedModule downedModule) && downedModule.GetRemaining() > 0)
                display.SetTimelineCuePosition(downedModule.GetEstimatedRealRemaining(), "Gets up", ColorReferences.Heal);
            else if (StunModule.GetRemaining() > 0)
                display.SetTimelineCuePosition(StunModule.GetEstimatedRealRemaining(), "Stun ends", ColorReferences.Stun);
            else if (RecoveryModule.GetRemaining() > 0)
                display.SetTimelineCuePosition(RecoveryModule.GetEstimatedRealRemaining(), "Recovery ends", ColorReferences.Recovery);
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
                
                display.SetTimelineCuePosition(ChargeModule.GetEstimatedRealRemaining(), label, color);
            }
            else
            {
                display.AllowTimelineIcon(false);
            }
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
            if (Display.TrySome(out CharacterDisplay self) && self.hasWire)
            {
                Debug.Log("Wired!");
            }
#endif
            
            if (Display.IsNone)
                return;

            CharacterDisplay display = Display.Value;
            if (display.AnimationStatus is not AnimationStatus.Common) // something else is controlling the animation so we don't want to override it
                return;

            if (stateBeforeAction is CharacterState.Defeated)
                return;
            
            if (StaminaModule.IsNone || StaminaModule.Value.GetCurrent() > 0 || result.Missed)
            {
                CheckHitAnimation();
                return;
            }

            display.CombatManager.Animations.CancelActionsOfCharacter(character: this, compensateChargeLost: false);
            CombatAnimation corpseAnimation = default;
            AnimationStatus desiredStatus;
            if (DownedModule.IsSome && DownedModule.Value.CanHandleNextZeroStamina())
                desiredStatus = AnimationStatus.Downed;
            else if (Script.BecomesCorpseOnDefeat(out corpseAnimation))
                desiredStatus = AnimationStatus.Corpse;
            else
                desiredStatus = AnimationStatus.Defeated;
            
            IActionSequence currentAction = display.CombatManager.Animations.CurrentAction;
            bool willRiposteActivate = currentAction is { IsPlaying: true } && currentAction.Caster != this && currentAction.Targets != null && currentAction.Targets.Contains(this) && StatusModule.HasActiveStatusOfType(EffectType.Riposte);

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
            StatusModule.RemoveAll();
            PerksModule.RemoveAll();
        }
        
        public bool Equals(CharacterStateMachine other) => EqualityComparer<CharacterStateMachine>.Default.Equals(this, other);

        public void DisplayDestroyed()
        {
            Display = Option<CharacterDisplay>.None;
        }
    }
}