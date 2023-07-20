using System.Text;
using Core.Combat.Scripts.Barks;
using Core.Combat.Scripts.Cues;
using Core.Combat.Scripts.Effects.Types.BuffOrDebuff;
using Core.Combat.Scripts.Enums;
using Core.Utils.Math;
using JetBrains.Annotations;
using static Core.Combat.Scripts.Behaviour.Modules.IDownedModule;

namespace Core.Combat.Scripts.Behaviour.Modules
{
    public record DefaultDownedRecord(TSpan InitialDuration, TSpan Remaining) : DownedRecord
    {
        [NotNull]
        public override IDownedModule Deserialize(CharacterStateMachine owner) => DefaultDownedModule.FromRecord(owner, record: this);
        public override bool IsDataValid(in StringBuilder errors, CharacterRecord[] allCharacters) => true;
    }
    
    public class DefaultDownedModule : IDownedModule
    {
        private static readonly BuffOrDebuffScript ComposureDebuff = new(Permanent: false, DefaultDownedDurationOnZeroStamina, BaseApplyChance: 200, CombatStat.Composure, BaseDelta: -40);

        [NotNull]
        private IDownedModule Interface => this;

        private readonly CharacterStateMachine _owner;

        private DefaultDownedModule(CharacterStateMachine owner) => _owner = owner;
        
        [NotNull]
        public static DefaultDownedModule FromInitialSetup(CharacterStateMachine owner) => new(owner);

        [NotNull]
        public static DefaultDownedModule FromRecord(CharacterStateMachine owner, [NotNull] DefaultDownedRecord record)
        {
            TSpan initialDuration = ClampInitialDuration(record.InitialDuration);
            return new DefaultDownedModule(owner)
            {
                _initialDuration = initialDuration,
                _remaining = ClampRemaining(record.Remaining, initialDuration)
            };
        }
        
        [NotNull]
        public DownedRecord GetRecord() => new DefaultDownedRecord(_initialDuration, _remaining);

        private TSpan _initialDuration;
        TSpan IDownedModule.InitialDuration => _initialDuration;

        private TSpan _remaining;
        TSpan IDownedModule.Remaining => _remaining;

        public TSpan GetEstimatedRemaining() => Interface.GetRemaining();

        void IDownedModule.SetInitialInternal(TSpan clampedInitialDuration)
        {
            _initialDuration = TSpan.ChoseMax(_initialDuration, clampedInitialDuration);
            _remaining = clampedInitialDuration;
        }

        void IDownedModule.SetBothInternal(TSpan clampedInitialDuration, TSpan clampedRemaining)
        {
            _initialDuration = clampedInitialDuration;
            _remaining = clampedRemaining;
        }

        public bool HandleZeroStamina()
        {
            if (_owner.LustModule.IsNone)
                return false;
            
            Interface.SetInitial(DefaultDownedDurationOnZeroStamina);
            return true;
        }

        public bool CanHandleNextZeroStamina() => _owner.LustModule.IsSome;

        public void Reset() => Interface.SetInitial(initialDuration: Zero);

        public bool Tick(ref TSpan timeStep)
        {
            if (_remaining.Ticks <= 0)
                return false;
            
            _remaining -= timeStep;
            if (_remaining.Ticks <= 0)
            {
                timeStep.Ticks = -1 * _remaining.Ticks;
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
            
            if (currentState is CharacterState.Downed)
                display.SetDownedBar(active: true, _remaining, total: _initialDuration);
            else
                display.SetDownedBar(active: false, remaining: Zero, total: Zero);

            if (previousState is not CharacterState.Downed && currentState is CharacterState.Downed)
            {
                _owner.PlayBark(BarkType.KnockedDown);
                _owner.StunModule.Reset();
                _owner.ChargeModule.Reset();
                _owner.RecoveryModule.Reset();

                if (_owner.LustModule.IsSome)
                    ComposureDebuff.ApplyEffect(caster: _owner, target: _owner, crit: false, skill: null);

                display.CombatManager.Characters.NotifyDowned(_owner);
                foreach (CharacterStateMachine ally in display.CombatManager.Characters.GetOnSide(_owner))
                {
                    if (ally != _owner)
                        ally.PlayBark(BarkType.AlyKnockedDown, _owner, calculateProbability: true);
                }

                if (CombatTextCueManager.AssertInstance(out CombatTextCueManager cueManager))
                {
                    CombatCueOptions options = CombatCueOptions.Default("Knocked Down!", ColorReferences.KnockedDown, display: display);
                    cueManager.EnqueueAboveCharacter(ref options, character: display);
                }

                display.AnimateDowned();
            }
            else if (previousState is CharacterState.Downed && currentState is not CharacterState.Downed and not CharacterState.Defeated and not CharacterState.Corpse)
            {
                _remaining = Zero;
                if (_owner.StaminaModule.TrySome(out IStaminaModule staminaModule))
                    staminaModule.SetCurrent((staminaModule.ActualMax * 75) / 100);

                display.MatchAnimationWithState(currentState);

                if (CombatTextCueManager.AssertInstance(out CombatTextCueManager cueManager))
                {
                    CombatCueOptions options = CombatCueOptions.Default("Recovered!", ColorReferences.Heal, display: display);
                    cueManager.EnqueueAboveCharacter(ref options, character: display);
                }
            }
        }

        public void ForceUpdateDisplay([NotNull] in DisplayModule display)
        {
            if (_owner.StateEvaluator.PureEvaluate() is CharacterState.Downed)
                display.SetDownedBar(active: true, _remaining, total: _initialDuration);
            else
                display.SetDownedBar(active: false, remaining: Zero, total: Zero);
        }
    }
}