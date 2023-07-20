using System.Text;
using Core.Combat.Scripts.Enums;
using Core.Utils.Math;
using JetBrains.Annotations;
using static Core.Combat.Scripts.Behaviour.Modules.IRecoveryModule;

namespace Core.Combat.Scripts.Behaviour.Modules
{
    public record DefaultRecoveryRecord(TSpan InitialDuration, TSpan Remaining) : RecoveryRecord
    {
        [NotNull]
        public override IRecoveryModule Deserialize(CharacterStateMachine owner) => DefaultRecoveryModule.FromRecord(owner, record: this);
        public override bool IsDataValid(in StringBuilder errors, CharacterRecord[] allCharacters) => true;
    }
    
    public class DefaultRecoveryModule : IRecoveryModule
    {
        [NotNull]
        private IRecoveryModule Interface => this;
        
        private readonly CharacterStateMachine _owner;

        private DefaultRecoveryModule(CharacterStateMachine owner) => _owner = owner;

        [NotNull]
        public static DefaultRecoveryModule FromInitialSetup(CharacterStateMachine owner, CombatSetupInfo.RecoveryInfo recoveryInfo)
        {
            TSpan initialDuration = ClampInitialDuration(recoveryInfo.GenerateValue());
            return new DefaultRecoveryModule(owner)
            {
                _initialDuration = initialDuration,
                _remaining = initialDuration
            };
        }
        
        [NotNull]
        public static DefaultRecoveryModule FromRecord(CharacterStateMachine owner, [NotNull] DefaultRecoveryRecord record) =>
            new(owner)
            {
                _initialDuration = record.InitialDuration,
                _remaining = record.Remaining
            };
        
        [NotNull]
        public RecoveryRecord GetRecord() => new DefaultRecoveryRecord(_initialDuration, _remaining);

        private TSpan _initialDuration;
        TSpan IRecoveryModule.InitialDuration => _initialDuration;

        private TSpan _remaining;
        TSpan IRecoveryModule.Remaining => _remaining;
        
        public TSpan GetEstimatedRemaining() => new(ticks: (_remaining.Ticks * 100) / _owner.StatsModule.GetSpeed());

        void IRecoveryModule.SetInitialInternal(TSpan clampedInitialDuration)
        {
            _initialDuration = clampedInitialDuration;
            _remaining = clampedInitialDuration;
        }

        void IRecoveryModule.SetBothInternal(TSpan clampedInitialDuration, TSpan clampedRemaining)
        {
            _initialDuration = clampedInitialDuration;
            _remaining = clampedRemaining;
        }

        public void Reset() => Interface.SetInitial(Zero);

        public bool Tick(ref TSpan timeStep)
        {
            if (_remaining.Ticks <= 0)
                return false;

            int speed = _owner.StatsModule.GetSpeed();
            TSpan actualStep = new(ticks: (timeStep.Ticks * speed) / 100); // divide by 100 to turn it into a percentage. 100 speed = 1x
            _remaining -= actualStep;
            if (_remaining.Ticks <= 0)
            {
                timeStep.Ticks = (-100 * _remaining.Ticks) / speed;
                _remaining = Zero;
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

            if (currentState is not (CharacterState.Defeated or CharacterState.Corpse or CharacterState.Downed or CharacterState.Grappled))
                display.SetRecoveryBar(active: true, _remaining, total: _initialDuration);
            else
                display.SetRecoveryBar(active: false, remaining: Zero, total: Zero);
        }

        public void ForceUpdateDisplay([NotNull] in DisplayModule display)
        {
            if (_owner.StateEvaluator.PureEvaluate() is not (CharacterState.Defeated or CharacterState.Corpse or CharacterState.Downed or CharacterState.Grappled))
                display.SetRecoveryBar(active: true, _remaining, total: _initialDuration);
            else
                display.SetRecoveryBar(active: false, remaining: Zero, total: Zero);
        }
    }
}