using System.Text;
using Core.Combat.Scripts.Cues;
using Core.Combat.Scripts.Effects.Interfaces;
using Core.Combat.Scripts.Effects.Types.BuffOrDebuff;
using Core.Combat.Scripts.Effects.Types.Grappled;
using Core.Combat.Scripts.Enums;
using Core.Utils.Collections;
using Core.Utils.Math;
using JetBrains.Annotations;
using UnityEngine;
using static Core.Combat.Scripts.Behaviour.Modules.IStunModule;

namespace Core.Combat.Scripts.Behaviour.Modules
{
    public record DefaultStunRecord(TSpan InitialDuration, TSpan Remaining) : StunRecord
    {
        [NotNull]
        public override IStunModule Deserialize(CharacterStateMachine owner) => DefaultStunModule.FromRecord(owner, record: this);
        
        public override bool IsDataValid(in StringBuilder errors, CharacterRecord[] allCharacters) => true;
    }

    public class DefaultStunModule : IStunModule
    {
        [NotNull]
        private IStunModule Interface => this;
        
        private readonly CharacterStateMachine _owner;
        CharacterStateMachine IStunModule.Owner => _owner;

        private DefaultStunModule(CharacterStateMachine owner) => _owner = owner;
        
        [NotNull]
        public static DefaultStunModule FromInitialSetup(CharacterStateMachine owner) => new(owner);
        
        [NotNull]
        public static DefaultStunModule FromRecord(CharacterStateMachine owner, [NotNull] DefaultStunRecord record)
        {
            TSpan initialDuration = ClampInitialDuration(record.InitialDuration);
            return new DefaultStunModule(owner)
            {
                _initialDuration = initialDuration,
                _remaining = ClampRemaining(record.Remaining, initialDuration),
            };
        }
        
        [NotNull]
        public StunRecord GetRecord() => new DefaultStunRecord(_initialDuration, _remaining);

        private TSpan _initialDuration;
        TSpan IStunModule.InitialDuration => _initialDuration;

        private TSpan _remaining;
        TSpan IStunModule.Remaining => _remaining;
        
        public TSpan GetEstimatedRemaining() => Interface.GetRemaining();
        
#region StunMitigation
        public int BaseStunMitigation { get; set; }
        private readonly SelfSortingList<IBaseAttributeModifier> _baseStunMitigationModifiers = new(ModifierComparer.Instance);

        public void SubscribeStunMitigation(IBaseAttributeModifier modifier, bool allowDuplicates)
        {
            if (allowDuplicates)
            {
                _baseStunMitigationModifiers.Add(modifier);
                return;
            }

            foreach (IBaseAttributeModifier element in _baseStunMitigationModifiers)
            {
                if (element.SharedId == modifier.SharedId)
                    return;
            }

            _baseStunMitigationModifiers.Add(modifier);
        }

        public void UnsubscribeStunMitigation(IBaseAttributeModifier modifier)
        {
            _baseStunMitigationModifiers.Remove(modifier);
        }

        int IStunModule.GetStunMitigationInternal()
        {
            int recoverySpeed = BaseStunMitigation;
            foreach (IBaseAttributeModifier modifier in _baseStunMitigationModifiers)
                modifier.Modify(ref recoverySpeed, _owner);

            return recoverySpeed;
        }
#endregion

        void IStunModule.SetInitialIgnoringMitigationInternal(TSpan clampedInitialDuration)
        {
            _initialDuration = clampedInitialDuration;
            _remaining = clampedInitialDuration;

            if (clampedInitialDuration.Ticks > 0 && _owner.StatusReceiverModule.GetAllRelated.FindType<LustGrappled>().TrySome(out LustGrappled lustGrappled) && lustGrappled.Restrainer == _owner)
                lustGrappled.RequestDeactivation();
        }

        void IStunModule.SetBothInternal(TSpan clampedInitialDuration, TSpan clampedRemaining)
        {
            _initialDuration = clampedInitialDuration;
            _remaining = clampedRemaining;

            if (clampedInitialDuration.Ticks > 0 && _owner.StatusReceiverModule.GetAllRelated.FindType<LustGrappled>().TrySome(out LustGrappled lustGrappled) && lustGrappled.Restrainer == _owner)
                lustGrappled.RequestDeactivation();
        }

        public bool Tick(ref TSpan timeStep)
        {
            if (_remaining.Ticks <= 0)
                return false;
            
            _remaining -= timeStep;
            if (_remaining.Ticks <= 0)
            {
                timeStep = new TSpan(ticks: -1 * _remaining.Ticks);
                _remaining = Zero;
                return false;
            }
            
            _remaining = ClampRemaining(_remaining, _initialDuration);
            timeStep = Zero;
            return true;
        }

        public void Reset() => Interface.SetInitialIgnoringMitigation(initialDuration: Zero);
        
        public void AfterTickUpdate(in TSpan timeStep, in CharacterState previousState, in CharacterState currentState)
        {
            if (_owner.Display.TrySome(out DisplayModule display) == false)
                return;

            if (previousState is not CharacterState.Stunned && currentState is CharacterState.Stunned)
                display.CombatManager.Characters.NotifyStunned(character: _owner);

            if (currentState is CharacterState.Stunned)
                display.SetStunBar(active: true, _remaining, total: _initialDuration);
            else
                display.SetStunBar(active: false, remaining: Zero, total: Zero);
        }

        public void ForceUpdateDisplay([NotNull] in DisplayModule display)
        {
            if (_owner.StateEvaluator.PureEvaluate() is CharacterState.Stunned)
                display.SetStunBar(active: true, _remaining, total: _initialDuration);
            else
                display.SetStunBar(active: false, remaining: Zero, total: Zero);
        }
    }
}