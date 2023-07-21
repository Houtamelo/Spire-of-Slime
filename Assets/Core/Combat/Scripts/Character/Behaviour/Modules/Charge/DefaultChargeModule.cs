using System.Text;
using Core.Combat.Scripts.Enums;
using Core.Utils.Math;
using JetBrains.Annotations;
using static Core.Combat.Scripts.Behaviour.Modules.IChargeModule;

namespace Core.Combat.Scripts.Behaviour.Modules
{
    public record DefaultChargeRecord(TSpan InitialDuration, TSpan Remaining) : ChargeRecord
    {
        [NotNull]
        public override IChargeModule Deserialize(CharacterStateMachine owner) => DefaultChargeModule.FromRecord(owner, record: this);
        public override bool IsDataValid(in StringBuilder errors, CharacterRecord[] allCharacters) => true;
    }
    
    public class DefaultChargeModule : IChargeModule
    {
        private readonly CharacterStateMachine _owner;

        private DefaultChargeModule(CharacterStateMachine owner) => _owner = owner;
        
        [NotNull]
        public static DefaultChargeModule FromInitialSetup(CharacterStateMachine owner) => new(owner);

        [NotNull]
        public static DefaultChargeModule FromRecord(CharacterStateMachine owner, [NotNull] DefaultChargeRecord record)
        {
            TSpan initialDuration = ClampInitialDuration(record.InitialDuration);
            return new DefaultChargeModule(owner)
            {
                _initialDuration = initialDuration,
                _remaining = ClampRemaining(record.Remaining, initialDuration)
            };
        }
        
        [NotNull]
        public ChargeRecord GetRecord() => new DefaultChargeRecord(_initialDuration, _remaining);

        private TSpan _initialDuration;
        TSpan IChargeModule.InitialDuration => _initialDuration;

        private TSpan _remaining;
        TSpan IChargeModule.Remaining => _remaining;

        public TSpan GetEstimatedRemaining() => TSpan.FromTicks((100 * _remaining.Ticks) / _owner.StatsModule.GetSpeed());
        
        public TSpan EstimateCharge(TSpan input) => TSpan.FromTicks((100 * input.Ticks) / _owner.StatsModule.GetSpeed());

        void IChargeModule.SetInitialInternal(TSpan clampedDuration)
        {
            _initialDuration = clampedDuration;
            _remaining = clampedDuration;
        }

        void IChargeModule.SetBothInternal(TSpan clampedInitialDuration, TSpan clampedRemaining)
        {
            _initialDuration = clampedInitialDuration;
            _remaining = clampedRemaining;
        }

        public void Reset() => (this as IChargeModule).SetInitial(duration: Zero);

        public bool Tick(ref TSpan timeStep)
        {
            if (_remaining.Ticks <= 0)
                return false;

            int speed = _owner.StatsModule.GetSpeed();
            TSpan actualStep = new(ticks: (timeStep.Ticks * speed) / 100); // divide it by 100 to turn it into a percentage. 100 speed = 1x
            _remaining -= actualStep;
            if (_remaining.Ticks <= 0)
            {
                timeStep.Ticks = (-100 * _remaining.Ticks) / speed;
                _remaining.Ticks = 0;
                return false;
            }

            _remaining = ClampRemaining(_remaining, _initialDuration);
            timeStep = Zero;
            return true;
        }

        public void AfterTickUpdate(in TSpan timeStep, in CharacterState previousState, in CharacterState currentState)
        {
            if (_owner.Display.TrySome(out DisplayModule display) == false)
                return;

            if (currentState is not (CharacterState.Downed or CharacterState.Defeated or CharacterState.Grappled or CharacterState.Grappling or CharacterState.Corpse))
                display.SetChargeBar(active: true, _remaining, total: _initialDuration);
            else
                display.SetChargeBar(active: false, remaining: Zero, total: Zero);
        }

        public void ForceUpdateDisplay([NotNull] in DisplayModule display)
        {
            if (_owner.StateEvaluator.PureEvaluate() is not (CharacterState.Downed or CharacterState.Defeated or CharacterState.Grappled or CharacterState.Grappling or CharacterState.Corpse))
                display.SetChargeBar(active: true, _remaining, total: _initialDuration);
            else
                display.SetChargeBar(active: false, remaining: Zero, total: Zero);
        }
    }
}