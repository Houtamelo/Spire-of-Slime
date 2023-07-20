using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Behaviour.Modules;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Timeline;
using Core.Utils.Collections;
using Core.Utils.Math;
using Core.Utils.Patterns;
using JetBrains.Annotations;
using UnityEngine;

namespace Core.Combat.Scripts.Effects.Types.OvertimeHeal
{
    public class OvertimeHeal : StatusInstance
    {
        public int HealPerSecond { get; }

        private TSpan _accumulatedTime;

        private OvertimeHeal(TSpan duration, bool isPermanent, CharacterStateMachine owner, int healPerTime) : base(duration, isPermanent, owner) =>
            HealPerSecond = healPerTime;

        public static Option<StatusInstance> CreateInstance(TSpan duration, bool isPermanent, CharacterStateMachine owner, CharacterStateMachine caster, int healPerTime)
        {
            if ((duration.Ticks <= 0 && isPermanent == false) || healPerTime == 0)
            {
                Debug.LogWarning($"Invalid parameters for {nameof(OvertimeHeal)}. Duration: {duration}, Permanent: {isPermanent}, HealPerTime: {healPerTime}");
                return Option.None;
            }
            
            OvertimeHeal instance = new(duration, isPermanent, owner, healPerTime);
            owner.StatusReceiverModule.AddStatus(instance, caster);
            return Option<StatusInstance>.Some(instance);
        }

        public OvertimeHeal([NotNull] OvertimeHealRecord record, CharacterStateMachine owner) : base(record, owner)
        {
            HealPerSecond = record.HealPerTime;
            _accumulatedTime = record.AccumulatedTime;
        }

        public override void Tick(TSpan timeStep)
        {
            _accumulatedTime += TSpan.ChoseMin(timeStep, Duration);
            if (Duration <= timeStep)
            {
                int heal = (int)(_accumulatedTime.Seconds * HealPerSecond);
                _accumulatedTime.Ticks = 0;

                if (heal != 0 && Owner.StaminaModule.TrySome(out IStaminaModule staminaModule))
                    staminaModule.DoHeal(heal, isOvertime: true);
            }
            else if (_accumulatedTime.Seconds >= 1)
            {
                int roundSeconds = (int)_accumulatedTime.Seconds;
                _accumulatedTime.Ticks -= roundSeconds * TSpan.TicksPerSecond;

                if (Owner.StaminaModule.TrySome(out IStaminaModule staminaModule))
                    staminaModule.DoHeal(roundSeconds * HealPerSecond, isOvertime: true);
            }

            base.Tick(timeStep);
        }
        
        public override void FillTimelineEvents([NotNull] SelfSortingList<CombatEvent> events)
        {
            Debug.Assert(IsActive);

            switch (Permanent)
            {
                case true when HealPerSecond > 0:
                {
                    AddEventsAsPermanent();

                    break;
                }
                case false:
                {
                    events.Add(CombatEvent.FromStatusEnd(Owner, Duration, status: this));

                    if (HealPerSecond > 0)
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
                    int heal = roundSeconds * HealPerSecond;

                    if (heal > 0)
                        events.Add(CombatEvent.FromHealTick(Owner, TSpan.Zero, source: this, heal));

                    current = -(_accumulatedTime - TSpan.FromSeconds(roundSeconds));
                }
                else
                {
                    current = -_accumulatedTime;
                }

                for (int i = 0; i < 10; i++)
                {
                    current += TSpan.OneSecond;
                    events.Add(CombatEvent.FromHealTick(Owner, current, source: this, HealPerSecond));
                }
            }

            void AddEventsUntilEnd()
            {
                TSpan totalTime = Duration + _accumulatedTime;
                int roundTotalSecondsLeft = (int)totalTime.Seconds;

                if (roundTotalSecondsLeft < 1)
                {
                    int heal = (int)(totalTime.Seconds * HealPerSecond);

                    if (heal > 0)
                        events.Add(CombatEvent.FromHealTick(Owner, Duration, source: this, heal));

                    return;
                }

                TSpan current;

                if (_accumulatedTime >= TSpan.OneSecond)
                {
                    int roundSeconds = (int)_accumulatedTime.Seconds;
                    int heal = roundSeconds * HealPerSecond;

                    if (heal > 0)
                        events.Add(CombatEvent.FromHealTick(Owner, TSpan.Zero, source: this, heal));

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
                    events.Add(CombatEvent.FromHealTick(Owner, current, source: this, HealPerSecond));
                }

                TSpan remainingTime = totalTime - TSpan.FromSeconds(roundTotalSecondsLeft);

                if (remainingTime.Ticks > 0)
                {
                    current += remainingTime;
                    int heal = (int)(remainingTime.Seconds * HealPerSecond);

                    if (heal > 0)
                        events.Add(CombatEvent.FromHealTick(Owner, current, source: this, heal));
                }
            }
        }

        [NotNull]
        public override StatusRecord GetRecord() => new OvertimeHealRecord(Duration, Permanent, HealPerSecond, _accumulatedTime);

        public override Option<string> GetDescription() => StatusInstanceDescriptions.Get(this);
        public override EffectType EffectType => EffectType.OvertimeHeal;
        public override bool IsPositive => true;
        public const int GlobalId = Marked.Marked.GlobalId + 1;
    }
}