using Core.Combat.Scripts.Barks;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Cues;
using Core.Combat.Scripts.Effects.Interfaces;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Combat.Scripts.Skills.Action;
using Save_Management;
using UnityEngine;
using Utils.Collections;
using Utils.Patterns;
using Random = UnityEngine.Random;
using static Core.Combat.Scripts.Interfaces.Modules.ILustModule;

namespace Core.Combat.Scripts.DefaultModules
{
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

        public static DefaultLustModule FromInitialSetup(CharacterStateMachine owner)
        {
            uint orgasmLimit = owner.Script.OrgasmLimit;
            uint orgasmCount = owner.Script.OrgasmCount;

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
            
            return new DefaultLustModule(owner)
            {
                _lust = ClampLust(owner.Script.Lust),
                BaseComposure = owner.Script.Composure,
                _temptation = owner.Script.Temptation,
                OrgasmLimit = orgasmLimit,
                _orgasmCount = ClampOrgasmCount(orgasmCount, orgasmLimit)
            };
        }

        public static DefaultLustModule FromRecord(CharacterStateMachine owner, CharacterRecord record)
        {
            return new DefaultLustModule(owner)
            {
                _lust = ClampLust(record.Lust),
                BaseComposure = record.BaseComposure,
                _temptation = record.Temptation,
                OrgasmLimit = record.OrgasmLimit,
                _orgasmCount = ClampOrgasmCount(record.OrgasmCount, record.OrgasmLimit)
            };
        }
        
        private uint _lust;
        uint ILustModule.Lust => _lust;
        void ILustModule.SetLustInternal(uint value) => _lust = value;
        private int _accumulatedLustDeltaForVisualCue;

        void ILustModule.ChangeLustInternal(int delta)
        {
            _lust = (uint)((int)_lust + delta);
            _accumulatedLustDeltaForVisualCue += delta;
        }
        
        /// <summary> The difference is that this shows a visual cue immediately </summary>
        void ILustModule.ChangeLustViaActionInternal(int rawDelta, int actualDelta)
        {
            if (_orgasmLimitAnimationQueued || rawDelta == 0)
                return;
            
            _lust = (uint)((int)_lust + actualDelta);
            //regardless if clamped we want to show how much lust would have been potentially gained/lost
            if (StatusVFXManager.AssertInstance(out StatusVFXManager statusVFXManager) == false ||
                CombatTextCueManager.AssertInstance(out CombatTextCueManager textCueManager) == false ||
                _owner.Display.AssertSome(out CharacterDisplay display) == false)
                return;
            
            CombatCueOptions options = CombatCueOptions.Default(rawDelta.ToString("0"), ColorReferences.Lust, display);
            options.FontSize *= 1.3f;
            options.Duration = IActionSequence.AnimationDuration / 2f;

            if (rawDelta > 0)
                options.OnPlay += statusVFXManager.LustIncreaseSource.Play;
            else
                options.OnPlay += statusVFXManager.LustDecreaseSource.Play;

            textCueManager.EnqueueAboveCharacter(ref options, display);
        }

        private uint _orgasmCount;
        uint ILustModule.OrgasmCount => _orgasmCount;
        void ILustModule.SetOrgasmCountInternal(uint value) => _orgasmCount = value;

        public uint OrgasmLimit { get; private set; }
        void ILustModule.SetOrgasmLimitInternal(uint value) => OrgasmLimit = value;
        
        public float BaseComposure { get; set; }
        private readonly SelfSortingList<IBaseFloatAttributeModifier> _baseComposureModifiers = new(ModifierComparer.Instance);

        public void SubscribeComposure(IBaseFloatAttributeModifier modifier, bool allowDuplicates)
        {
            if (allowDuplicates)
            {
                _baseComposureModifiers.Add(modifier);
                return;
            }

            foreach (IBaseFloatAttributeModifier element in _baseComposureModifiers)
                if (element.SharedId == modifier.SharedId)
                    return;

            _baseComposureModifiers.Add(modifier);
        }

        public void UnsubscribeComposure(IBaseFloatAttributeModifier modifier) => _baseComposureModifiers.Remove(modifier);

        float ILustModule.GetComposureInternal()
        {
            float composure = BaseComposure;
            foreach (IBaseFloatAttributeModifier modifier in _baseComposureModifiers)
                modifier.Modify(ref composure, _owner);

            return composure;
        }

        private ClampedPercentage _temptation;
        ClampedPercentage ILustModule.Temptation => _temptation;
        private float _accumulatedTemptationDeltaForVisualCue;

        void ILustModule.SetTemptationInternal(ClampedPercentage value) => _temptation = value;

        void ILustModule.ChangeTemptationInternal(float delta)
        {
            _temptation += delta;
            _accumulatedTemptationDeltaForVisualCue += delta;
        }
        
        public void Tick(float timeStep)
        {
            if (_orgasmLimitAnimationQueued || _owner.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Corpse or CharacterState.Grappled)
                return;

            if (_lust >= MaxLust)
            {
                float delta = timeStep * TemptationDeltaPerStepOnMaxLust;
                _interface.ChangeTemptation(delta);
            }
        }

        public void AfterTickUpdate(in float timeStep, in CharacterState previousState, in CharacterState currentState)
        {
            if (currentState is CharacterState.Defeated or CharacterState.Corpse)
                return;
            
            if (_owner.Display.TrySome(out CharacterDisplay display) == false)
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

        public void ForceUpdateDisplay(in CharacterDisplay display)
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

        private void DoCuesAndBarks(in CharacterDisplay display)
        {
            if (_orgasmLimitAnimationQueued)
                return;

            CheckAccumulatedLust(display);
            CheckAccumulatedTemptation();
        }

        private void CheckAccumulatedLust(CharacterDisplay display)
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
                if (ally != _owner)
                    ally.PlayBark(_lust < 100 ? BarkType.AlyReceivedLust0To100 : BarkType.AlyReceivedLust100To200, _owner, calculateProbability: true);

            _accumulatedLustDeltaForVisualCue = 0;
        }

        private void CheckAccumulatedTemptation()
        {
            if (_accumulatedTemptationDeltaForVisualCue <= 0f)
                return;
            
            if (StatusVFXManager.AssertInstance(out StatusVFXManager statusVFXManager))
                statusVFXManager.Enqueue(StatusCueHandler.FromTemptationTick(_owner, StatusCueHandler.StandardValidator, _accumulatedTemptationDeltaForVisualCue));
            
            if (_accumulatedTemptationDeltaForVisualCue < 0.04f)
            {
                _accumulatedTemptationDeltaForVisualCue = 0f;
                return;
            }

            BarkType barkType = _accumulatedTemptationDeltaForVisualCue switch
            {
                < 0.25f => BarkType.Temptation0To25,
                < 0.5f  => BarkType.Temptation25To50,
                < 0.75f => BarkType.Temptation50To75,
                _       => BarkType.Temptation75To100
            };
            
            _owner.PlayBark(barkType);
            _accumulatedTemptationDeltaForVisualCue = 0f;
        }

        private bool _orgasmLimitAnimationQueued;

        void ILustModule.OrgasmInternal()
        {
            Debug.Assert(_orgasmLimitAnimationQueued == false, message: "Orgasm limit animation already queued.");

            float temptation = _interface.GetTemptation() + TemptationDeltaOnOrgasm;
            _interface.SetTemptation(temptation);
            _interface.SetLust(0);

            if (_owner.Display.AssertSome(out CharacterDisplay display) == false)
                return;

            _owner.PlayBark(OrgasmLimit - _orgasmCount > 1 ? BarkType.OrgasmWithMoreThanOneRemaining : BarkType.OrgasmWithOnlyOneRemaining);

            display.CombatManager.ActionAnimator.AnimateOrgasm(display);
            if (_orgasmCount == OrgasmLimit)
            {
                _orgasmLimitAnimationQueued = true;
                _owner.StateEvaluator.OutOfForces();
            }
        }
    }
}