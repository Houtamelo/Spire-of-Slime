using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Behaviour.Modules;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Timeline;
using Core.Utils.Collections;
using Core.Utils.Math;
using Core.Utils.Patterns;
using JetBrains.Annotations;
using UnityEngine;

namespace Core.Combat.Scripts.Effects.Types.Poison
{
    public class Poison : StatusInstance
    {
        public override bool IsPositive => false;

        public readonly CharacterStateMachine Caster;
        public readonly int DamagePerSecond;
        private TSpan _accumulatedTime;

        private Poison(TSpan duration, bool isPermanent, CharacterStateMachine owner, CharacterStateMachine caster, int damagePerSecond)
            : base(duration, isPermanent, owner)
        {
            DamagePerSecond = damagePerSecond;
            Caster = caster;
        }

        public static Option<StatusInstance> CreateInstance(TSpan duration, bool isPermanent, CharacterStateMachine owner, CharacterStateMachine caster, int damagePerSecond)
        {
            if ((duration.Ticks <= 0 && isPermanent == false) || damagePerSecond <= 0)
            {
                Debug.LogWarning($"Invalid poison parameters: duration: {duration}, isPermanent: {isPermanent}, damagePerSecond: {damagePerSecond}");
                return Option.None;
            }
            
            Poison instance = new(duration, isPermanent, owner, caster, damagePerSecond);
            owner.StatusReceiverModule.AddStatus(instance, caster);
            return Option<StatusInstance>.Some(instance);
        }

        public Poison([NotNull] PoisonRecord record, CharacterStateMachine owner, CharacterStateMachine caster) : base(record, owner)
        {
            DamagePerSecond = record.DamagePerTime;
            _accumulatedTime = record.AccumulatedTime;
            Caster = caster;
        }

        public override void Tick(TSpan timeStep)
        {
            _accumulatedTime += TSpan.ChoseMin(timeStep, Duration);
            if (Duration <= timeStep)
            {
                int damage = (int)(_accumulatedTime.Seconds * DamagePerSecond);
                _accumulatedTime.Ticks = 0;

                if (damage != 0 && Owner.StaminaModule.TrySome(out IStaminaModule staminaModule))
                    staminaModule.ReceiveDamage(damage, DamageType.Poison, Caster);
            }
            else if (_accumulatedTime.Seconds >= 1)
            {
                int roundSeconds = (int)_accumulatedTime.Seconds;
                _accumulatedTime.Ticks -= roundSeconds * TSpan.TicksPerSecond;
                
                if (Owner.StaminaModule.TrySome(out IStaminaModule staminaModule))
                    staminaModule.ReceiveDamage(roundSeconds * DamagePerSecond, DamageType.Poison, Caster);
            }

            base.Tick(timeStep);
        }

        public override void FillTimelineEvents([NotNull] SelfSortingList<CombatEvent> events)
        {
            Debug.Assert(IsActive);

            switch (Permanent)
            {
                case true when DamagePerSecond > 0:
                {
                    AddEventsAsPermanent();
                    break;
                }
                case false:
                {
                    events.Add(CombatEvent.FromStatusEnd(Owner, Duration, status: this));

                    if (DamagePerSecond > 0)
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
                    int damage = roundSeconds * DamagePerSecond;

                    if (damage > 0)
                        events.Add(CombatEvent.FromPoisonTick(Owner, TSpan.Zero, source: this, damage));

                    current = -(_accumulatedTime - TSpan.FromSeconds(roundSeconds));
                }
                else
                {
                    current = -_accumulatedTime;
                }

                for (int i = 0; i < 10; i++)
                {
                    current += TSpan.OneSecond;
                    events.Add(CombatEvent.FromPoisonTick(Owner, current, source: this, DamagePerSecond));
                }
            }

            void AddEventsUntilEnd()
            {
                TSpan totalTime = Duration + _accumulatedTime;
                int roundTotalSecondsLeft = (int)(totalTime.Seconds);
                if (roundTotalSecondsLeft < 1)
                {
                    int poisonDamage = (int)(totalTime.Seconds * DamagePerSecond);
                    if (poisonDamage > 0)
                        events.Add(CombatEvent.FromPoisonTick(Owner, Duration, source: this, poisonDamage));
                
                    return;
                }
                
                TSpan current;

                if (_accumulatedTime >= TSpan.OneSecond)
                {
                    int roundSeconds = (int)_accumulatedTime.Seconds;
                    int damage = roundSeconds * DamagePerSecond;

                    if (damage > 0)
                        events.Add(CombatEvent.FromPoisonTick(Owner, TSpan.Zero, source: this, damage));

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
                    events.Add(CombatEvent.FromPoisonTick(Owner, current, source: this, DamagePerSecond));
                }

                TSpan remainingTime = totalTime - TSpan.FromSeconds(roundTotalSecondsLeft);

                if (remainingTime.Ticks > 0)
                {
                    current += remainingTime;
                    int damage = (int)(remainingTime.Seconds * DamagePerSecond);

                    if (damage > 0)
                        events.Add(CombatEvent.FromPoisonTick(Owner, current, source: this, damage));
                }
            }
        }

        [NotNull]
        public override StatusRecord GetRecord() => new PoisonRecord(Duration, Permanent, DamagePerSecond, _accumulatedTime, Caster.Guid);

        public override Option<string> GetDescription() => StatusInstanceDescriptions.Get(this);
        public override EffectType EffectType => EffectType.Poison;
        public const int GlobalId = OvertimeHeal.OvertimeHeal.GlobalId + 1;
    }
}