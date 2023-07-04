using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Interfaces.Modules;
using static Core.Combat.Scripts.Interfaces.Modules.IChargeModule;

namespace Core.Combat.Scripts.DefaultModules
{
    public class DefaultChargeModule : IChargeModule
    {
        private readonly CharacterStateMachine _owner;

        private DefaultChargeModule(CharacterStateMachine owner) => _owner = owner;
        
        public static DefaultChargeModule FromInitialSetup(CharacterStateMachine owner) => new(owner);

        public static DefaultChargeModule FromRecord(CharacterStateMachine owner, CharacterRecord record)
        {
            float initialDuration = ClampInitialDuration(record.ChargeInitialDuration);
            return new DefaultChargeModule(owner)
            {
                _initialDuration = initialDuration,
                _remaining = ClampRemaining(record.ChargeRemaining, initialDuration)
            };
        }

        private float _initialDuration;
        float IChargeModule.InitialDuration => _initialDuration;

        private float _remaining;
        float IChargeModule.Remaining => _remaining;

        public float GetEstimatedRealRemaining() => _remaining / _owner.StatsModule.GetSpeed();

        void IChargeModule.SetInitialInternal(float initialDuration)
        {
            _initialDuration = initialDuration;
            _remaining = initialDuration;
        }

        void IChargeModule.SetBothInternal(float initialDuration, float remaining)
        {
            _initialDuration = initialDuration;
            _remaining = remaining;
        }

        public void Reset() => (this as IChargeModule).SetInitial(duration: 0);

        public bool Tick(ref float timeStep)
        {
            if (_remaining <= 0f)
                return false;

            float speed = _owner.StatsModule.GetSpeed();
            float step = timeStep * speed;
            _remaining -= step;
            if (_remaining < 0f)
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

            if (currentState is not (CharacterState.Downed or CharacterState.Defeated or CharacterState.Grappled or CharacterState.Grappling or CharacterState.Corpse))
                display.SetChargeBar(active: true, _remaining, total: _initialDuration);
            else
                display.SetChargeBar(active: false, remaining: 0f, total: 0f);
        }

        public void ForceUpdateDisplay(in CharacterDisplay display)
        {
            if (_owner.StateEvaluator.PureEvaluate() is not (CharacterState.Downed or CharacterState.Defeated or CharacterState.Grappled or CharacterState.Grappling or CharacterState.Corpse))
                display.SetChargeBar(active: true, _remaining, total: _initialDuration);
            else
                display.SetChargeBar(active: false, remaining: 0f, total: 0f);
        }
    }
}