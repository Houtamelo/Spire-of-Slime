using System;
using System.Collections.Generic;
using System.Linq;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Behaviour.Modules;
using Core.Combat.Scripts.Effects;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Effects.Types.Arousal;
using Core.Combat.Scripts.Effects.Types.BuffOrDebuff;
using Core.Combat.Scripts.Effects.Types.Grappled;
using Core.Combat.Scripts.Effects.Types.Guarded;
using Core.Combat.Scripts.Effects.Types.Marked;
using Core.Combat.Scripts.Effects.Types.Mist;
using Core.Combat.Scripts.Effects.Types.NemaExhaustion;
using Core.Combat.Scripts.Effects.Types.OvertimeHeal;
using Core.Combat.Scripts.Effects.Types.Perk;
using Core.Combat.Scripts.Effects.Types.Poison;
using Core.Combat.Scripts.Effects.Types.Riposte;
using Core.Combat.Scripts.Enums;
using Core.Localization.Scripts;
using Core.Utils.Extensions;
using JetBrains.Annotations;
using KGySoft.CoreLibraries;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Core.Combat.Scripts
{
    public static class CombatUtils
    {
        private static readonly CombatStat[] CombatStats = Enum<CombatStat>.GetValues();
        private static readonly HashSet<CombatStat> CombatStatsSet = new(CombatStats);

        private static readonly Dictionary<CombatStat, LocalizedText> CombatStat_LowerCaseNames =
            Enum<CombatStat>.GetValues().ToDictionary(keySelector: stat => stat, elementSelector: stat => new LocalizedText("combatstat_lowercase_" + stat.ToStringNonAlloc().ToLowerInvariant().Trim()));

        public static LocalizedText LowerCaseName(this CombatStat stat) => CombatStat_LowerCaseNames[stat];

        private static readonly Dictionary<CombatStat, LocalizedText> CombatStat_CompactLowerCaseNames =
            Enum<CombatStat>.GetValues().ToDictionary(keySelector: stat => stat, elementSelector: stat => new LocalizedText("combatstat_lowercase_compact_" + stat.ToStringNonAlloc().ToLowerInvariant().Trim()));

        public static LocalizedText CompactLowerCaseName(this CombatStat stat) => CombatStat_CompactLowerCaseNames[stat];
        
        private static readonly Dictionary<CombatStat, LocalizedText> CombatStat_UpperCaseNames =
            Enum<CombatStat>.GetValues().ToDictionary(keySelector: stat => stat, elementSelector: stat => new LocalizedText("combatstat_uppercase_" + stat.ToStringNonAlloc().ToLowerInvariant().Trim()));
        
        public static LocalizedText UpperCaseName(this CombatStat stat) => CombatStat_UpperCaseNames[stat];
        
        private static readonly Dictionary<Race, LocalizedText> Race_UpperCaseNames =
            Enum<Race>.GetValues().ToDictionary(keySelector: stat => stat, elementSelector: stat => new LocalizedText("race_uppercase_" + stat.ToStringNonAlloc().ToLowerInvariant().Trim()));

        public static LocalizedText UpperCaseName(this Race race) => Race_UpperCaseNames[race];

        public static bool HasIcon(this EffectType effectType)
        {
            return effectType switch
            {
                EffectType.Buff           => true,
                EffectType.Debuff         => true,
                EffectType.Poison         => true,
                EffectType.Arousal        => true,
                EffectType.Riposte        => true,
                EffectType.OvertimeHeal   => true,
                EffectType.Marked         => true,
                EffectType.Guarded        => true,
                EffectType.LustGrappled   => true,
                EffectType.Move           => false,
                EffectType.Stun           => false,
                EffectType.Perk           => false,
                EffectType.HiddenPerk     => true,
                EffectType.Heal           => false,
                EffectType.Lust           => false,
                EffectType.NemaExhaustion => true,
                EffectType.Mist           => false,
                EffectType.Summon         => false,
                EffectType.Temptation     => false,
                _                         => throw new ArgumentOutOfRangeException(nameof(effectType), effectType, null)
            };
        }

        public static CombatStat GetRandomCombatStat() => CombatStats[Random.Range(0, CombatStats.Length)];

        public static CombatStat GetRandomCombatStatExcept(CombatStat combatStat)
        {
            CombatStatsSet.Remove(combatStat);
            CombatStat result = CombatStatsSet.ElementAt(Random.Range(0, CombatStatsSet.Count));
            CombatStatsSet.Add(combatStat);
            return result;
        }
        
        public static CombatStat GetRandomCombatStatExcept(CombatStat one, CombatStat two)
        {
            CombatStatsSet.Remove(one);
            CombatStatsSet.Remove(two);
            CombatStat result = CombatStatsSet.ElementAt(Random.Range(0, CombatStatsSet.Count));
            CombatStatsSet.Add(one);
            CombatStatsSet.Add(two);
            return result;
        }

        public static int GetEffectId([NotNull] StatusInstance effect)
        {
            switch (effect)
            {
                case Arousal:        return Arousal.GlobalId;
                case BuffOrDebuff:   return BuffOrDebuff.GlobalId;
                case Guarded:        return Guarded.GlobalId;
                case Marked:         return Marked.GlobalId;
                case OvertimeHeal:   return OvertimeHeal.GlobalId;
                case Poison:         return Poison.GlobalId;
                case Riposte:        return Riposte.GlobalId;
                case LustGrappled:   return LustGrappled.GlobalId;
                case PerkStatus:     return PerkStatus.GlobalId;
                case NemaExhaustion: return NemaExhaustion.GlobalId;
                case MistStatus:     return MistStatus.GlobalId;
                default:   
                    Debug.LogWarning($"Unknown effect type: {effect.GetType()}");
                    return -1;
            }
        }

        public static bool OnlyOneAllowed(this EffectType effectType)
        {
            return effectType switch
            {
                EffectType.Riposte => true,
                EffectType.Marked => true,
                EffectType.LustGrappled => true,
                EffectType.NemaExhaustion => true,
                EffectType.Mist => true,
                EffectType.Guarded => true,
                _ => false
            };
        }
        
        public static float Percentage([NotNull] this IStaminaModule staminaModule) => (float) staminaModule.GetCurrent() / staminaModule.ActualMax;
    }
}