using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Interfaces.Modules;
using static Core.Combat.Scripts.Interfaces.Modules.IRecoveryModule;

namespace Core.Combat.Scripts.DefaultModules
{
    public class DefaultRecoveryModule : IRecoveryModule
    {
        private IRecoveryModule Interface => this;
        
        private readonly CharacterStateMachine _owner;

        private DefaultRecoveryModule(CharacterStateMachine owner) => _owner = owner;

        public static DefaultRecoveryModule FromInitialSetup(CharacterStateMachine owner, CombatSetupInfo.RecoveryInfo recoveryInfo)
        {
            float recovery = ClampInitialDuration(recoveryInfo.GenerateValue());
            return new DefaultRecoveryModule(owner)
            {
                _initialDuration = recovery,
                _remaining = recovery
            };
        }
        
        public static DefaultRecoveryModule FromRecord(CharacterStateMachine owner, CharacterRecord record) =>
            new(owner)
            {
                _initialDuration = record.RecoveryInitialDuration,
                _remaining = record.RecoveryRemaining
            };

        private float _initialDuration;
        float IRecoveryModule.InitialDuration => _initialDuration;

        private float _remaining;
        float IRecoveryModule.Remaining => _remaining;
        
        public float GetEstimatedRealRemaining() => _remaining / _owner.StatsModule.GetSpeed();

        void IRecoveryModule.SetInitialInternal(float initialDuration)
        {
            _initialDuration = initialDuration;
            _remaining = initialDuration;
        }

        void IRecoveryModule.SetBothInternal(float initialDuration, float remaining)
        {
            _initialDuration = initialDuration;
            _remaining = remaining;
        }

        public void Reset() => Interface.SetInitial(0);

        public bool Tick(ref float timeStep)
        {
            if (_remaining <= 0)
                return false;

            float speed = _owner.StatsModule.GetSpeed();
            float step = timeStep * speed;
            _remaining -= step;
            if (_remaining < 0)
            {
                timeStep = -1 * _remaining / speed;
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

            if (currentState is not (CharacterState.Defeated or CharacterState.Corpse or CharacterState.Downed or CharacterState.Grappled))
                display.SetRecoveryBar(active: true, _remaining, total: _initialDuration);
            else
                display.SetRecoveryBar(active: false, remaining: 0f, total: 0f);
        }

        public void ForceUpdateDisplay(in CharacterDisplay display)
        {
            if (_owner.StateEvaluator.PureEvaluate() is not (CharacterState.Defeated or CharacterState.Corpse or CharacterState.Downed or CharacterState.Grappled))
                display.SetRecoveryBar(active: true, _remaining, total: _initialDuration);
            else
                display.SetRecoveryBar(active: false, remaining: 0f, total: 0f);
        }
    }
}