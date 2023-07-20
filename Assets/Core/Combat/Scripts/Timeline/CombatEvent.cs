using System;
using System.Collections.Generic;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Skills.Action;
using Core.Localization.Scripts;
using Core.Utils.Extensions;
using Core.Utils.Math;
using JetBrains.Annotations;

namespace Core.Combat.Scripts.Timeline
{
    public readonly struct CombatEvent
    {
        private static LocalizedText DescriptionTranslation(Type type) => type switch
        {
            Type.Turn       => new LocalizedText("combat_event_turn"),
            Type.PoisonTick => new LocalizedText("combat_event_poisontick"),
            Type.LustTick   => new LocalizedText("combat_event_lusttick"),
            Type.HealTick   => new LocalizedText("combat_event_healtick"),
            Type.StunEnd    => new LocalizedText("combat_event_stunend"),
            Type.DownedEnd  => new LocalizedText("combat_event_downedend"),
            Type.StatusEnd  => new LocalizedText("combat_event_statusend"),
            Type.Action     => new LocalizedText("combat_event_action"),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        public CharacterStateMachine Owner { get; private init; }
        public Type EventType { get; private init; }
        public TSpan Time { get; private init; }
        public object Source { get; private init; }

        public int PoisonAmount { get; private init; }
        public int LustDelta { get; private init; }
        public int HealAmount { get; private init; }
        public StatusInstance Status { get; private init; }
        public PlannedSkill Action { get; private init; }

        public string GetDescription()
        {
            LocalizedText label = DescriptionTranslation(EventType);
            
            return EventType switch
            {
                Type.Turn       => label.Translate().GetText(),
                Type.PoisonTick => GetPoisonTickDescription(PoisonAmount),
                Type.LustTick   => GetLustTickDescription(LustDelta),
                Type.HealTick   => GetHealTickDescription(HealAmount),
                Type.StunEnd    => label.Translate().GetText(),
                Type.DownedEnd  => label.Translate().GetText(),
                Type.StatusEnd  => label.Translate().GetText(Status.EffectType.UpperCaseName().Translate().GetText()),
                Type.Action     => label.Translate().GetText(),
                _ => throw new ArgumentOutOfRangeException(nameof(EventType), EventType, null)
            };
        }
        
        public static CombatEvent FromTurn(CharacterStateMachine owner, TSpan time) 
            => new() { Owner = owner, EventType = Type.Turn, Time = time, Source = owner };
        public static CombatEvent FromPoisonTick(CharacterStateMachine owner, TSpan time, object source, int poisonAmount) 
            => new() { Owner = owner, EventType = Type.PoisonTick, Time = time, Source = source, PoisonAmount = poisonAmount };
        public static CombatEvent FromLustTick(CharacterStateMachine owner, TSpan time, object source, int lustDelta) 
            => new() { Owner = owner, EventType = Type.LustTick, Time = time, Source = source, LustDelta = lustDelta };
        public static CombatEvent FromHealTick(CharacterStateMachine owner, TSpan time, object source, int healAmount) 
            => new() { Owner = owner, EventType = Type.HealTick, Time = time, Source = source, HealAmount = healAmount };
        public static CombatEvent FromStunEnd(CharacterStateMachine owner, TSpan time) 
            => new() { Owner = owner, EventType = Type.StunEnd, Time = time, Source = owner };
        public static CombatEvent FromDownedEnd(CharacterStateMachine owner, TSpan time) 
            => new() { Owner = owner, EventType = Type.DownedEnd, Time = time, Source = owner };
        public static CombatEvent FromStatusEnd(CharacterStateMachine owner, TSpan time, StatusInstance status)
            => new() { Owner = owner, EventType = Type.StatusEnd, Time = time, Source = status, Status = status };
        public static CombatEvent FromAction(CharacterStateMachine owner, TSpan time, PlannedSkill action)
            => new() { Owner = owner, EventType = Type.Action, Time = time, Source = owner, Action = action };

        public static string GetPoisonTickDescription(int poisonAmount) => DescriptionTranslation(Type.PoisonTick).Translate().GetText(poisonAmount.ToString("0"));

        public static string GetLustTickDescription(int lustDelta) => DescriptionTranslation(Type.LustTick).Translate().GetText(lustDelta.WithSymbol());
        
        public static string GetHealTickDescription(int healAmount) => DescriptionTranslation(Type.HealTick).Translate().GetText(healAmount.ToString("0"));

        /// Take this, memory allocation, also take this, time spent. ** Still allocates string **
        [NotNull]
        public static string GetMultipleStatusEndDescription([NotNull] in HashSet<StatusInstance> statuses)
        {
            LocalizedText label = DescriptionTranslation(Type.StatusEnd);
            string rawTrans = label.Translate().RawText;
            Span<char> span = stackalloc char[512];
            int index = 0;

            foreach (StatusInstance statusInstance in statuses)
            {
                if (index != 0)
                {
                    span[index] = '\n';
                    index++;
                }

                for (int transIndex = 0; transIndex < rawTrans.Length && index < span.Length; transIndex++, index++)
                {
                    char currentChar = rawTrans[transIndex];
                    if (currentChar != '{')
                    {
                        span[index] = currentChar;
                        continue;
                    }

                    transIndex += 2; // skipping "0}"
                    string statusName = statusInstance.EffectType.ToStringNonAlloc();
                    for (int statusNameIndex = 0; statusNameIndex < statusName.Length && index < span.Length; statusNameIndex++, index++)
                        span[index] = statusName[statusNameIndex];
                    
                    span[index] = ' ';
                }
            }
            
            return new string(span[..index]);
        }

        public enum Type
        {
            Turn = 0,
            PoisonTick = 1,
            LustTick = 2,
            HealTick = 3,
            StunEnd = 4,
            DownedEnd = 5,
            StatusEnd = 6,
            Action = 7
        }
    }
}