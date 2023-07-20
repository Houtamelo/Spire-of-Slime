using System.Text;
using Core.Combat.Scripts.Barks;
using Core.Combat.Scripts.Cues;
using Core.Combat.Scripts.Effects.Interfaces;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Skills.Action;
using Core.Utils.Collections;
using Core.Utils.Math;
using Core.Utils.Patterns;
using JetBrains.Annotations;
using UnityEngine;
using static Core.Combat.Scripts.Behaviour.Modules.ILustModule;

namespace Core.Combat.Scripts.Behaviour.Modules
{
    public record DefaultLustRecord(int Lust, int BaseComposure, int Temptation, int OrgasmLimit, int OrgasmCount, TSpan AccumulatedMaxLustTime) : LustRecord
    {
        [NotNull]
        public override ILustModule Deserialize(CharacterStateMachine owner) => DefaultLustModule.FromRecord(owner, record: this);
        public override bool IsDataValid(in StringBuilder errors, CharacterRecord[] allCharacters) => true;
    }
    
    public class DefaultLustModule : ILustModule
    {
        private readonly ILustModule _interface;
        
        private readonly CharacterStateMachine _owner;
        CharacterStateMachine ILustModule.Owner => _owner;

        private DefaultLustModule(CharacterStateMachine owner)
        {
            _owner = owner;
            _interface = this;
        }

        [NotNull]
        public static DefaultLustModule FromInitialSetup([NotNull] CharacterStateMachine owner)
        {
            int orgasmLimit = owner.Script.OrgasmLimit;
            int orgasmCount = owner.Script.OrgasmCount;

            if (orgasmLimit == 0)
            {
                Debug.LogWarning("Character has no orgasm limit but has lust module, setting limit to 1");
                orgasmLimit = 1;
            }
            
            if (orgasmCount >= orgasmLimit)
            {
                Debug.LogWarning($"Character has more or equal orgasms({orgasmCount}) than limit({orgasmLimit}), setting orgasms to limit minus 1");
                orgasmCount = orgasmLimit - 1;
            }

            int clampedOrgasmLimit = ClampOrgasmLimit(orgasmLimit);

            return new DefaultLustModule(owner)
            {
                _lust = ClampLust(owner.Script.Lust),
                BaseComposure = owner.Script.Composure,
                _temptation = ClampTemptation(owner.Script.Temptation),
                _orgasmLimit = clampedOrgasmLimit,
                _orgasmCount = ClampOrgasmCount(orgasmCount, clampedOrgasmLimit)
            };
        }

        [NotNull]
        public static DefaultLustModule FromRecord(CharacterStateMachine owner, [NotNull] DefaultLustRecord record)
        {
            int clampedTemptation = ClampTemptation(record.Temptation);
            int clampedOrgasmLimit = ClampOrgasmLimit(record.OrgasmLimit);

            return new DefaultLustModule(owner)
            {
                _lust = ClampLust(record.Lust),
                BaseComposure = record.BaseComposure,
                _temptation = clampedTemptation,
                _orgasmLimit = clampedOrgasmLimit,
                _orgasmCount = ClampOrgasmCount(record.OrgasmCount, clampedOrgasmLimit),
                _accumulatedMaxLustTime = record.AccumulatedMaxLustTime
            };
        }

        [NotNull]
        public LustRecord GetRecord() => new DefaultLustRecord(_lust, BaseComposure, _temptation, _orgasmLimit, _orgasmCount, _accumulatedMaxLustTime);

        private int _lust;
        int ILustModule.Lust => _lust;

        private int _accumulatedLustDeltaForVisualCue;
        
        void ILustModule.SetLustInternal(int clampedValue) => _lust = clampedValue;
        
        void ILustModule.ChangeLustInternal(int clampedDelta)
        {
            _lust += clampedDelta;
            _accumulatedLustDeltaForVisualCue += clampedDelta;
        }
        
        /// <summary> The difference is that this shows a visual cue immediately </summary>
        void ILustModule.ChangeLustViaActionInternal(int value, int clampedDelta)
        {
            if (_orgasmLimitAnimationQueued || value == 0)
                return;
            
            _lust += clampedDelta;
            //regardless if clamped we want to show how much lust would have been potentially gained/lost
            if (StatusVFXManager.AssertInstance(out StatusVFXManager statusVFXManager) == false ||
                CombatTextCueManager.AssertInstance(out CombatTextCueManager textCueManager) == false ||
                _owner.Display.AssertSome(out DisplayModule display) == false)
                return;
            
            CombatCueOptions options = CombatCueOptions.Default(value.ToString("0"), ColorReferences.Lust, display);
            options.FontSize *= 1.3f;
            options.Duration = IActionSequence.AnimationDuration / 2f;

            if (value > 0)
                options.OnPlay += statusVFXManager.LustIncreaseSource.Play;
            else
                options.OnPlay += statusVFXManager.LustDecreaseSource.Play;

            textCueManager.EnqueueAboveCharacter(ref options, display);
        }

        private int _orgasmCount;
        int ILustModule.OrgasmCount => _orgasmCount;
        void ILustModule.SetOrgasmCountInternal(int clampedValue) => _orgasmCount = clampedValue;

        private int _orgasmLimit;
        int ILustModule.OrgasmLimit => _orgasmLimit;
        void ILustModule.SetOrgasmLimitInternal(int clampedValue) => _orgasmLimit = clampedValue;

#region Composure

        public int BaseComposure { get; set; }
        private readonly SelfSortingList<IBaseAttributeModifier> _baseComposureModifiers = new(ModifierComparer.Instance);

        public void SubscribeComposure(IBaseAttributeModifier modifier, bool allowDuplicates)
        {
            if (allowDuplicates)
            {
                _baseComposureModifiers.Add(modifier);
                return;
            }

            foreach (IBaseAttributeModifier element in _baseComposureModifiers)
            {
                if (element.SharedId == modifier.SharedId)
                    return;
            }

            _baseComposureModifiers.Add(modifier);
        }

        public void UnsubscribeComposure(IBaseAttributeModifier modifier) => _baseComposureModifiers.Remove(modifier);

        int ILustModule.GetComposureInternal()
        {
            int composure = BaseComposure;
            foreach (IBaseAttributeModifier modifier in _baseComposureModifiers)
                modifier.Modify(ref composure, _owner);

            return composure;
        }

#endregion

        private int _temptation;
        int ILustModule.Temptation => _temptation;

        private static readonly TSpan OneSecond = TSpan.FromSeconds(1.0);
        
        private TSpan _accumulatedMaxLustTime;
        private int _accumulatedTemptationDeltaForVisualCue;

        void ILustModule.SetTemptationInternal(int clampedValue) => _temptation = clampedValue;

        void ILustModule.ChangeTemptationInternal(int clampedDelta)
        {
            _temptation += clampedDelta;
            _accumulatedTemptationDeltaForVisualCue += clampedDelta;
        }
        
        public void Tick(TSpan timeStep)
        {
            if (_orgasmLimitAnimationQueued || _owner.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Corpse or CharacterState.Grappled)
                return;

            if (_lust < MaxLust)
                return;

            _accumulatedMaxLustTime += timeStep;
            int delta = 0;

            while (_accumulatedMaxLustTime > OneSecond)
            {
                delta += TemptationDeltaPerSecondOnMaxLust;
                _accumulatedMaxLustTime -= OneSecond;
            }
            
            if (delta != 0)
                _interface.ChangeTemptation(delta);
        }

        public void AfterTickUpdate(in TSpan timeStep, in CharacterState previousState, in CharacterState currentState)
        {
            if (currentState is CharacterState.Defeated or CharacterState.Corpse)
                return;
            
            if (_owner.Display.TrySome(out DisplayModule display) == false)
                return;

            if (currentState is not CharacterState.Grappled)
            {
                display.SetLustBar(active: true, _lust);
                display.SetTemptationBar(active: true, _temptation);
            }
            else
            {
                display.SetLustBar(active: false, lust: 0);
                display.SetTemptationBar(active: false, temptation: 0);
            }

            DoCuesAndBarks(display);
        }

        public void ForceUpdateDisplay([NotNull] in DisplayModule display)
        {
            if (_owner.StateEvaluator.PureEvaluate() is not CharacterState.Defeated and not CharacterState.Corpse and not CharacterState.Grappled)
            {
                display.SetLustBar(active: true, _lust);
                display.SetTemptationBar(active: true, _temptation);
            }
            else
            {
                display.SetLustBar(active: false, lust: 0);
                display.SetTemptationBar(active: false, temptation: 0);
            }
        }

        private void DoCuesAndBarks(in DisplayModule display)
        {
            if (_orgasmLimitAnimationQueued)
                return;

            CheckAccumulatedLust(display);
            CheckAccumulatedTemptation();
        }

        private void CheckAccumulatedLust(DisplayModule display)
        {
            if (_accumulatedLustDeltaForVisualCue == 0)
                return;
            
            if (StatusVFXManager.AssertInstance(out StatusVFXManager statusVFXManager))
                statusVFXManager.Enqueue(StatusCueHandler.FromLustTick(_owner, StatusCueHandler.StandardValidator, _accumulatedLustDeltaForVisualCue));

            if (_accumulatedLustDeltaForVisualCue < 10)
            {
                _accumulatedLustDeltaForVisualCue = 0;
                return;
            }

            BarkType barkType = _lust switch
            {
                < 50  => BarkType.ReceivedLust0To50,
                < 100 => BarkType.ReceivedLust50To100,
                < 150 => BarkType.ReceivedLust100To150,
                _     => BarkType.ReceivedLust150To200
            };

            _owner.PlayBark(barkType);

            foreach (CharacterStateMachine ally in display.CombatManager.Characters.GetOnSide(_owner))
            {
                if (ally != _owner)
                    ally.PlayBark(_lust < 100 ? BarkType.AlyReceivedLust0To100 : BarkType.AlyReceivedLust100To200, _owner, calculateProbability: true);
            }

            _accumulatedLustDeltaForVisualCue = 0;
        }

        private void CheckAccumulatedTemptation()
        {
            if (_accumulatedTemptationDeltaForVisualCue <= 0)
                return;
            
            if (StatusVFXManager.AssertInstance(out StatusVFXManager statusVFXManager))
                statusVFXManager.Enqueue(StatusCueHandler.FromTemptationTick(_owner, StatusCueHandler.StandardValidator, _accumulatedTemptationDeltaForVisualCue));
            
            if (_accumulatedTemptationDeltaForVisualCue < 4)
            {
                _accumulatedTemptationDeltaForVisualCue = 0;
                return;
            }

            BarkType barkType = _accumulatedTemptationDeltaForVisualCue switch
            {
                < 25 => BarkType.Temptation0To25,
                < 50  => BarkType.Temptation25To50,
                < 75 => BarkType.Temptation50To75,
                _       => BarkType.Temptation75To100
            };
            
            _owner.PlayBark(barkType);
            _accumulatedTemptationDeltaForVisualCue = 0;
        }

        private bool _orgasmLimitAnimationQueued;

        void ILustModule.OrgasmInternal()
        {
            Debug.Assert(_orgasmLimitAnimationQueued == false, message: "Orgasm limit animation already queued.");

            int temptation = _interface.GetTemptation() + TemptationDeltaOnOrgasm;
            _interface.SetTemptation(temptation);
            _interface.SetLust(0);

            if (_owner.Display.AssertSome(out DisplayModule display) == false)
                return;

            _owner.PlayBark(_orgasmLimit - _orgasmCount > 1 ? BarkType.OrgasmWithMoreThanOneRemaining : BarkType.OrgasmWithOnlyOneRemaining);

            display.CombatManager.ActionAnimator.AnimateOrgasm(display);
            if (_orgasmCount == _orgasmLimit)
            {
                _orgasmLimitAnimationQueued = true;
                _owner.StateEvaluator.OutOfForces();
            }
        }
    }
}