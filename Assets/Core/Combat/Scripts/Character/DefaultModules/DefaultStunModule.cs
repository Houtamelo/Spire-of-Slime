using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Cues;
using Core.Combat.Scripts.Effects.Types.BuffOrDebuff;
using Core.Combat.Scripts.Effects.Types.Grappled;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Interfaces.Modules;
using UnityEngine;
using static Core.Combat.Scripts.Interfaces.Modules.IStunModule;

namespace Core.Combat.Scripts.DefaultModules
{
    public class DefaultStunModule : IStunModule
    {
        private IStunModule Interface => this;
        
        private readonly CharacterStateMachine _owner;

        private DefaultStunModule(CharacterStateMachine owner) => _owner = owner;
        
        public static DefaultStunModule FromInitialSetup(CharacterStateMachine owner) => new(owner);
        
        public static DefaultStunModule FromRecord(CharacterStateMachine owner, CharacterRecord record)
        {
            float initialDuration = ClampInitialDuration(record.StunInitialDuration);
            return new DefaultStunModule(owner)
            {
                _initialDuration = initialDuration,
                _remaining = ClampRemaining(record.StunRemaining, initialDuration),
                _timeSinceStunStarted = ClampTimeSinceStart(record.TimeSinceStunStarted),
                StunRecoverySteps = record.StunRecoverySteps
            };
        }

        private float _initialDuration;
        float IStunModule.InitialDuration => _initialDuration;

        private float _remaining;
        float IStunModule.Remaining => _remaining;
        
        public float GetEstimatedRealRemaining() => _remaining / _owner.ResistancesModule.GetStunRecoverySpeed();

        private float _timeSinceStunStarted;
        float IStunModule.TimeSinceStunStarted => _timeSinceStunStarted;

        public uint StunRecoverySteps { get; private set; }

        void IStunModule.SetInitialInternal(float initialDuration)
        {
            _initialDuration = initialDuration;
            _remaining = initialDuration;

            if (initialDuration > 0f && _owner.StatusModule.GetAllRelated.FindType<LustGrappled>().TrySome(out LustGrappled lustGrappled) && lustGrappled.Restrainer == _owner)
                lustGrappled.RequestDeactivation();
        }

        void IStunModule.SetBothInternal(float initialDuration, float remaining)
        {
            _initialDuration = initialDuration;
            _remaining = remaining;

            if (initialDuration > 0f && _owner.StatusModule.GetAllRelated.FindType<LustGrappled>().TrySome(out LustGrappled lustGrappled) && lustGrappled.Restrainer == _owner)
                lustGrappled.RequestDeactivation();
        }

        public bool Tick(ref float timeStep)
        {
            if (_remaining <= 0)
                return false;
            
            float speed = _owner.ResistancesModule.GetStunRecoverySpeed();
            float step = timeStep * speed;
            _remaining -= step;
            if (_remaining < 0)
            {
                timeStep = -1 * _remaining / speed;
                _remaining = 0;
                _timeSinceStunStarted = 0;
                return false;
            }
            
            _remaining = ClampRemaining(_remaining, _initialDuration);
            _timeSinceStunStarted += timeStep;
            uint floorTime = (uint) Mathf.FloorToInt(_timeSinceStunStarted / StunRecoveryStepTime);
            if (floorTime > StunRecoverySteps)
            {
                StunRecoverySteps = floorTime;
                AddStunResistBuff(floorTime - StunRecoverySteps);
            }
            
            timeStep = 0;
            return true;
        }

        public void Reset() => Interface.SetInitial(initialDuration: 0);

        private void AddStunResistBuff(uint deltaSteps)
        {
            float resistAmount = deltaSteps * StunResistPerStep;
            float duration = 3f + (deltaSteps - 1) * StunResistPerStep;

            BuffOrDebuff.CreateInstance(duration, isPermanent: false, _owner, caster: _owner,CombatStat.StunSpeed, resistAmount);
            if (_owner.Display.AssertSome(out CharacterDisplay display) && CombatTextCueManager.AssertInstance(out CombatTextCueManager combatTextCueManager))
            {
                CombatCueOptions options = CombatCueOptions.Default("Enduring Stun!", ColorReferences.Stun, display);
                combatTextCueManager.EnqueueAboveCharacter(ref options, display);
            }
        }
        
        public void AfterTickUpdate(in float timeStep, in CharacterState previousState, in CharacterState currentState)
        {
            if (_owner.Display.TrySome(out CharacterDisplay display) == false)
                return;

            if (previousState is not CharacterState.Stunned && currentState is CharacterState.Stunned)
            {
                _timeSinceStunStarted = 0;
                StunRecoverySteps = 0;
                display.CombatManager.Characters.NotifyStunned(character: _owner);
            }

            if (currentState is CharacterState.Stunned)
                display.SetStunBar(active: true, _remaining, total: _initialDuration);
            else
                display.SetStunBar(active: false, remaining: 0f, total: 0f);
        }

        public void ForceUpdateDisplay(in CharacterDisplay display)
        {
            if (_owner.StateEvaluator.PureEvaluate() is CharacterState.Stunned)
                display.SetStunBar(active: true, _remaining, total: _initialDuration);
            else
                display.SetStunBar(active: false, remaining: 0f, total: 0f);
        }
    }
}