using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Effects.Interfaces;
using Core.Combat.Scripts.Effects.Types.Grappled;
using Core.Save_Management;
using Core.Utils.Math;
using Core.Utils.Patterns;
using JetBrains.Annotations;
using UnityEngine;
using Save = Core.Save_Management.SaveObjects.Save;

namespace Core.Combat.Scripts.Effects.Types.Mist
{
    public class MistStatus : StatusInstance
    {
        private const int LustPerSecond = 2;
        public readonly ComposureModifier ComposureDebuff = new();

        private TSpan _accumulatedRawTime;
        private TSpan _accumulatedProcessedTime;

        private MistStatus(TSpan duration, bool isPermanent, CharacterStateMachine owner) : base(duration, isPermanent, owner)
        {
        }

        public static Option<StatusInstance> CreateInstance(TSpan duration, bool isPermanent, CharacterStateMachine owner)
        {
            if (duration.Ticks <= 0 && !isPermanent)
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                Debug.LogWarning($"Invalid parameters for {nameof(MistStatus)}. Duration: {duration.Seconds.ToString()}, isPermanent: {isPermanent}");
                return Option.None;
            }
            
            MistStatus instance = new(duration, isPermanent, owner);
            owner.StatusReceiverModule.AddStatus(instance, owner);
            owner.LustModule.Value.SubscribeComposure(instance.ComposureDebuff, allowDuplicates: false);
            return Option<StatusInstance>.Some(instance);
        }

        public MistStatus([NotNull] MistStatusRecord record, CharacterStateMachine owner) : base(record, owner)
        {
            _accumulatedRawTime = record.AccumulatedRawTime;
            _accumulatedProcessedTime = record.AccumulatedProcessedTime;
        }

        public override void Tick(TSpan timeStep)
        {
            base.Tick(timeStep);
            if (Save.AssertInstance(out Save save) == false || Owner.LustModule.IsNone)
                return;
            
            if (Owner.StatusReceiverModule.GetAll.FindType<LustGrappled>().TrySome(out LustGrappled lustGrappled) && lustGrappled.IsActive)
                return;

            (bool isInCombat, bool isStanding) = save.GetNemaCombatStatus();
            int exhaustion = save.NemaExhaustion;
            switch (Save.Current.IsNemaClearingMist && isInCombat && isStanding)
            {
                case false:                                               _accumulatedProcessedTime += timeStep.GetMultiplication(2);   break;
                case true when exhaustion >= NemaStatus.HighExhaustion:   _accumulatedProcessedTime += timeStep.GetMultiplication(2);   break;
                case true when exhaustion >= NemaStatus.MediumExhaustion: _accumulatedProcessedTime += timeStep;                        break;
                case true when exhaustion >= NemaStatus.LowExhaustion:    _accumulatedProcessedTime += timeStep.GetMultiplication(0.5); break;
                case true when exhaustion < NemaStatus.LowExhaustion:     return;
            }
            
            _accumulatedRawTime += timeStep;

            if (_accumulatedRawTime.Seconds < 2)
                return;

            int halfSecondCount = (int)(_accumulatedProcessedTime.Seconds * 2);
            double processedPercentage = (halfSecondCount / _accumulatedProcessedTime.Seconds).Clamp01();
            
            _accumulatedRawTime.SubtractSeconds(_accumulatedRawTime.Seconds * processedPercentage);
            _accumulatedProcessedTime.SubtractSeconds(_accumulatedProcessedTime.Seconds * processedPercentage);

            int generatedLust = halfSecondCount * LustPerSecond / 2;
            generatedLust += save.GeneralRandomizer.Next(3) - 1;
            Owner.LustModule.Value.ChangeLust(generatedLust);
        }

        public override void RequestDeactivation()
        {
            base.RequestDeactivation();
            if (Owner.LustModule.IsNone)
                return;
            
            Owner.LustModule.Value.UnsubscribeComposure(ComposureDebuff);
        }

        [NotNull]
        public override StatusRecord GetRecord() => new MistStatusRecord(_accumulatedRawTime, _accumulatedProcessedTime, Duration, Permanent);

        public override Option<string> GetDescription() => StatusInstanceDescriptions.Get(this);
        public override EffectType EffectType => EffectType.Mist;
        public const int GlobalId = NemaExhaustion.NemaExhaustion.GlobalId + 1;
        public override bool IsPositive => false;

        public class ComposureModifier : IBaseAttributeModifier
        {
            public int Priority => -1;
            [NotNull]
            public string SharedId => nameof(MistStatus);

            public void Modify(ref int value, CharacterStateMachine self)
            {
                if (Save.AssertInstance(out Save save) == false)
                    return;
                
                (bool isInCombat, bool isStanding) = save.GetNemaCombatStatus();
                value = (Save.Current.NemaExhaustion, Save.Current.IsNemaClearingMist && isInCombat && isStanding) switch
                {
                    (NemaExhaustion: _, false)                             => value -= 20,
                    (NemaExhaustion: >= NemaStatus.HighExhaustion, true)   => value -= 20,
                    (NemaExhaustion: >= NemaStatus.MediumExhaustion, true) => value -= 10,
                    (NemaExhaustion: >= NemaStatus.LowExhaustion, true)    => value -= 5,
                    _                                                      => value
                };
            }
        }
    }
}