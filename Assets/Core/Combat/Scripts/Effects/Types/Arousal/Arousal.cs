using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Behaviour.Modules;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Timeline;
using Core.Utils.Collections;
using Core.Utils.Math;
using Core.Utils.Patterns;
using JetBrains.Annotations;
using UnityEngine;

namespace Core.Combat.Scripts.Effects.Types.Arousal
{
    public class Arousal : StatusInstance
    {
        public override bool IsPositive => false;

        public int LustPerSecond { get; }
        private TSpan _accumulatedTime;

        private Arousal([NotNull] ArousalToApply s) : base(s.Duration, s.Permanent, s.Target) => LustPerSecond = s.LustPerSecond;

        public static Option<StatusInstance> CreateInstance([NotNull] ref ArousalToApply effectStruct)
        {
            if ((effectStruct.Duration.Ticks <= 0 && effectStruct.Permanent == false) || effectStruct.LustPerSecond <= 0)
            {
                Debug.LogWarning($"Invalid parameters for {nameof(Arousal)} effect. Duration: {effectStruct.Duration.Seconds.ToString()}, Permanent: {effectStruct.Permanent.ToString()}, LustPerTime: {effectStruct.LustPerSecond.ToString()}");
                return Option.None;
            }
            
            Arousal instance = new(effectStruct);
            effectStruct.Target.StatusReceiverModule.AddStatus(instance, effectStruct.Caster);
            return instance;
        }

        public Arousal([NotNull] ArousalRecord record, CharacterStateMachine owner) : base(record, owner)
        {
            LustPerSecond = record.LustPerTime;
            _accumulatedTime = record.AccumulatedTime;
        }

        public override void Tick(TSpan timeStep)
        {
            _accumulatedTime += TSpan.ChoseMin(timeStep, Duration);
            if (Duration <= timeStep)
            {
                int lust = (int)(_accumulatedTime.Seconds * LustPerSecond);
                _accumulatedTime.Ticks = 0;

                if (lust != 0 && Owner.LustModule.TrySome(out ILustModule lustModule))
                    lustModule.ChangeLust(lust);
            }
            else  if (_accumulatedTime.Seconds >= 1)
            {
                int roundSeconds = (int)_accumulatedTime.Seconds;
                _accumulatedTime.Ticks -= roundSeconds * TSpan.TicksPerSecond;

                if (Owner.LustModule.TrySome(out ILustModule lustModule))
                    lustModule.ChangeLust(roundSeconds * LustPerSecond);
            }

            base.Tick(timeStep);
        }

        public override void FillTimelineEvents([NotNull] SelfSortingList<CombatEvent> events)
        {
            Debug.Assert(IsActive);

            switch (Permanent)
            {
                case true when LustPerSecond > 0:
                {
                    AddEventsAsPermanent();

                    break;
                }
                case false:
                {
                    events.Add(CombatEvent.FromStatusEnd(Owner, Duration, status: this));

                    if (LustPerSecond > 0)
                        AddEventsUntilEnd();

                    break;
                }
            }

            return;

            void AddEventsAsPermanent()
            {
                TSpan current;

                if (_accumulatedTime >= TSpan.OneSecond)
                {
                    int roundSeconds = (int)_accumulatedTime.Seconds;
                    int lust = roundSeconds * LustPerSecond;

                    if (lust > 0)
                        events.Add(CombatEvent.FromLustTick(Owner, TSpan.Zero, source: this, lust));

                    current = -(_accumulatedTime - TSpan.FromSeconds(roundSeconds));
                }
                else
                {
                    current = -_accumulatedTime;
                }

                for (int i = 0; i < 10; i++)
                {
                    current += TSpan.OneSecond;
                    events.Add(CombatEvent.FromLustTick(Owner, current, source: this, LustPerSecond));
                }
            }

            void AddEventsUntilEnd()
            {
                TSpan totalTime = Duration + _accumulatedTime;
                int roundTotalSecondsLeft = (int)(totalTime.Seconds);

                if (roundTotalSecondsLeft < 1)
                {
                    int lust = (int)(totalTime.Seconds * LustPerSecond);

                    if (lust > 0)
                        events.Add(CombatEvent.FromLustTick(Owner, Duration, source: this, lust));

                    return;
                }

                TSpan current;

                if (_accumulatedTime >= TSpan.OneSecond)
                {
                    int roundSeconds = (int)_accumulatedTime.Seconds;
                    int lust = roundSeconds * LustPerSecond;

                    if (lust > 0)
                        events.Add(CombatEvent.FromLustTick(Owner, TSpan.Zero, source: this, lust));

                    current = -(_accumulatedTime - TSpan.FromSeconds(roundSeconds));
                    roundTotalSecondsLeft -= roundSeconds;
                }
                else
                {
                    current = -_accumulatedTime;
                }

                for (int i = 0; i < roundTotalSecondsLeft; i++)
                {
                    current += TSpan.OneSecond;
                    events.Add(CombatEvent.FromLustTick(Owner, current, source: this, LustPerSecond));
                }

                TSpan remainingTime = totalTime - TSpan.FromSeconds(roundTotalSecondsLeft);

                if (remainingTime.Ticks > 0)
                {
                    current += remainingTime;
                    int lust = (int)(remainingTime.Seconds * LustPerSecond);

                    if (lust > 0)
                        events.Add(CombatEvent.FromLustTick(Owner, current, source: this, lust));
                }
            }
        }

        [NotNull]
        public override StatusRecord GetRecord() => new ArousalRecord(Duration, Permanent, LustPerSecond, _accumulatedTime);

        public override Option<string> GetDescription() => StatusInstanceDescriptions.Get(instance: this);
        public override EffectType EffectType => EffectType.Arousal;
        public const int GlobalId = 1;
    }
}