using Core.Combat.Scripts.Barks;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Cues;
using Core.Combat.Scripts.Effects.Types.BuffOrDebuff;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Utils.Math;
using UnityEngine;
using static Core.Combat.Scripts.Interfaces.Modules.IDownedModule;

namespace Core.Combat.Scripts.DefaultModules
{
    public class DefaultDownedModule : IDownedModule
    {
        private static readonly BuffOrDebuffScript ComposureDebuff = new(Permanent: false, DefaultDownedDurationOnZeroStamina, BaseApplyChance: 2f, CombatStat.Composure, BaseDelta: -0.4f);

        private IDownedModule Interface => this;

        private readonly CharacterStateMachine _owner;

        private DefaultDownedModule(CharacterStateMachine owner) => _owner = owner;
        
        public static DefaultDownedModule FromInitialSetup(CharacterStateMachine owner) => new(owner);

        public static DefaultDownedModule FromRecord(CharacterStateMachine owner, CharacterRecord record)
        {
            float initialDuration = ClampInitialDuration(record.DownedInitialDuration);
            return new DefaultDownedModule(owner)
            {
                _initialDuration = initialDuration,
                _remaining = ClampRemaining(record.DownedRemaining, initialDuration)
            };
        }

        private float _initialDuration;
        float IDownedModule.InitialDuration => _initialDuration;

        private float _remaining;
        float IDownedModule.Remaining => _remaining;
        
        public float GetEstimatedRealRemaining() => _remaining / _owner.ResistancesModule.GetStunRecoverySpeed();

        void IDownedModule.SetInitialInternal(float initialDuration)
        {
            _initialDuration = Mathf.Max(_initialDuration, initialDuration);
            _remaining = initialDuration;
        }

        void IDownedModule.SetBothInternal(float initialDuration, float remaining)
        {
            _initialDuration = initialDuration;
            _remaining = remaining;
        }

        public bool HandleZeroStamina()
        {
            if (_owner.LustModule.IsNone)
                return false;
            
            Interface.SetInitial(DefaultDownedDurationOnZeroStamina);
            return true;
        }

        public bool CanHandleNextZeroStamina() => _owner.LustModule.IsSome;

        public void Reset() => Interface.SetInitial(initialDuration: 0f);

        public bool Tick(ref float timeStep)
        {
            if (_remaining <= 0)
                return false;

            float stunRecoverySpeed = _owner.ResistancesModule.GetStunRecoverySpeed();
            float step = timeStep * stunRecoverySpeed;
            _remaining -= step;
            if (_remaining < 0)
            {
                timeStep = -1 * _remaining / stunRecoverySpeed;
                _remaining = 0;
                return false;
            }

            _remaining = ClampRemaining(_remaining, _initialDuration);
            timeStep = 0;
            return true;
        }

        public void AfterTickUpdate(in float timeStep, in CharacterState previousState, in CharacterState currentState)
        {
            if (_owner.Display.TrySome(out CharacterDisplay display) == false)
                return;
            
            if (currentState is CharacterState.Downed)
                display.SetDownedBar(active: true, remaining: _remaining, total: _initialDuration);
            else
                display.SetDownedBar(active: false, remaining: 0f, total: 0f);

            if (previousState is not CharacterState.Downed && currentState is CharacterState.Downed)
            {
                _owner.PlayBark(BarkType.KnockedDown);
                _owner.StunModule.Reset();
                _owner.ChargeModule.Reset();
                _owner.RecoveryModule.Reset();

                if (_owner.LustModule.IsSome)
                {
                    ComposureDebuff.ApplyEffect(caster: _owner, target: _owner, crit: false, skill: null);
                }

                display.CombatManager.Characters.NotifyDowned(_owner);
                foreach (CharacterStateMachine ally in display.CombatManager.Characters.GetOnSide(_owner))
                    if (ally != _owner)
                        ally.PlayBark(BarkType.AlyKnockedDown, _owner, calculateProbability: true);

                if (CombatTextCueManager.AssertInstance(out CombatTextCueManager cueManager))
                {
                    CombatCueOptions options = CombatCueOptions.Default("Knocked Down!", ColorReferences.KnockedDown, display: display);
                    cueManager.EnqueueAboveCharacter(ref options, character: display);
                }

                display.AnimateDowned();
            }
            else if (previousState is CharacterState.Downed && currentState is not CharacterState.Downed and not CharacterState.Defeated and not CharacterState.Corpse)
            {
                _remaining = 0;
                if (_owner.StaminaModule.TrySome(out IStaminaModule staminaModule))
                    staminaModule.SetCurrent((staminaModule.ActualMax * 0.75f).CeilToUInt());
                
                display.MatchAnimationWithState(currentState);

                if (CombatTextCueManager.AssertInstance(out CombatTextCueManager cueManager))
                {
                    CombatCueOptions options = CombatCueOptions.Default("Recovered!", ColorReferences.Heal, display: display);
                    cueManager.EnqueueAboveCharacter(ref options, character: display);
                }
            }
        }

        public void ForceUpdateDisplay(in CharacterDisplay display)
        {
            if (_owner.StateEvaluator.PureEvaluate() is CharacterState.Downed)
                display.SetDownedBar(active: true, remaining: _remaining, total: _initialDuration);
            else
                display.SetDownedBar(active: false, remaining: 0f, total: 0f);
        }
    }
}