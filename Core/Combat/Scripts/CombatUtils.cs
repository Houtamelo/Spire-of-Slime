using System;
using System.Collections.Generic;
using System.Linq;
using Core.Combat.Scripts.Behaviour;
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
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using KGySoft.CoreLibraries;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Core.Combat.Scripts
{
    public static class CombatUtils
    {
        private static readonly CombatStat[] CombatStats = Enum<CombatStat>.GetValues();
        private static readonly HashSet<CombatStat> CombatStatsSet = new(CombatStats);
        public static string LowerCaseName(this CombatStat stat)
        {
            return stat switch
            {
                CombatStat.DebuffResistance   => "debuff resistance",
                CombatStat.PoisonResistance   => "poison resistance",
                CombatStat.MoveResistance     => "move resistance",
                CombatStat.Accuracy           => "accuracy",
                CombatStat.CriticalChance     => "critical chance",
                CombatStat.Dodge              => "dodge",
                CombatStat.Resilience         => "resilience",
                CombatStat.Composure          => "composure",
                CombatStat.StunSpeed          => "stun recovery speed",
                CombatStat.DamageMultiplier   => "damage multiplier",
                CombatStat.Speed              => "speed",
                CombatStat.DebuffApplyChance  => "debuff apply chance",
                CombatStat.PoisonApplyChance  => "poison apply chance",
                CombatStat.MoveApplyChance    => "move apply chance",
                CombatStat.ArousalApplyChance => "arousal apply chance",
                _                             => throw new ArgumentOutOfRangeException(nameof(stat), stat, null)
            };
        }
        
        public static string CompactLowerCaseName(this CombatStat stat)
        {
            return stat switch
            {
                CombatStat.DebuffResistance   => "debufR",
                CombatStat.PoisonResistance   => "poisR",
                CombatStat.MoveResistance     => "moveR",
                CombatStat.Accuracy           => "acc",
                CombatStat.CriticalChance     => "crit",
                CombatStat.Dodge              => "dodge",
                CombatStat.Resilience         => "resil",
                CombatStat.Composure          => "compo",
                CombatStat.StunSpeed          => "stunR",
                CombatStat.DamageMultiplier   => "dmg",
                CombatStat.Speed              => "speed",
                CombatStat.DebuffApplyChance  => "debufAp",
                CombatStat.PoisonApplyChance  => "poisAp",
                CombatStat.MoveApplyChance    => "moveAp",
                CombatStat.ArousalApplyChance => "arousAp",
                _                             => throw new ArgumentOutOfRangeException(nameof(stat), stat, null)
            };
        }
        
        public static string UpperCaseName(this CombatStat stat)
        {
            return stat switch
            {
                CombatStat.DebuffResistance   => "Debuff Resistance",
                CombatStat.PoisonResistance   => "Poison Resistance",
                CombatStat.MoveResistance     => "Move Resistance",
                CombatStat.Accuracy           => "Accuracy",
                CombatStat.CriticalChance     => "Critical Chance",
                CombatStat.Dodge              => "Dodge",
                CombatStat.Resilience         => "Resilience",
                CombatStat.Composure          => "Composure",
                CombatStat.StunSpeed          => "Stun Recovery Speed",
                CombatStat.DamageMultiplier   => "Damage Multiplier",
                CombatStat.Speed              => "Speed",
                CombatStat.DebuffApplyChance  => "Debuff Apply Chance",
                CombatStat.PoisonApplyChance  => "Poison Apply Chance",
                CombatStat.MoveApplyChance    => "Move Apply Chance",
                CombatStat.ArousalApplyChance => "Arousal Apply Chance",
                _                             => throw new ArgumentOutOfRangeException(nameof(stat), stat, null)
            };
        }

        public static string UpperCaseName(this Race race)
        {
            return race switch
            {
                Race.Beast    => "Beast",
                Race.Human    => "Humanoid",
                Race.Mutation => "Mutation",
                Race.Plant    => "Plant",
                _             => "Unknown"
            };
        }

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

        public static CombatStat GetRandomCombatStat()
        {
            return CombatStats[Random.Range(0, CombatStats.Length)];
        }
        
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

        public static int GetEffectId(StatusInstance effect)
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
		
		public static void CreateStatusInstanceFromRecord(StatusRecord record, CharacterStateMachine owner, CharacterEnumerator allCharacters)
        {
            switch (record)
            {
                case ArousalRecord arousalRecord:           Arousal.CreateInstance(arousalRecord, owner); break;
                case BuffOrDebuffRecord buffOrDebuffRecord: BuffOrDebuff.CreateInstance(buffOrDebuffRecord, owner); break;
                case LustGrappledRecord lustGrappledRecord: LustGrappled.CreateInstance(lustGrappledRecord, owner, ref allCharacters); break;
                case GuardedRecord guardedRecord:           Guarded.CreateInstance(guardedRecord, owner, ref allCharacters); break;
                case MarkedRecord markedRecord:             Marked.CreateInstance(markedRecord, owner); break;
                case MistStatusRecord mistStatusRecord:     MistStatus.CreateInstance(mistStatusRecord, owner); break;
                case NemaExhaustionStatusRecord exhaustion: NemaExhaustion.CreateInstance(exhaustion, owner); break;
                case OvertimeHealRecord overtimeHealRecord: OvertimeHeal.CreateInstance(overtimeHealRecord, owner); break;
                case PerkStatusRecord perkStatusRecord:
                {
                    foreach (PerkInstance perkInstance in owner.PerksModule.GetAll)
                        if (perkInstance.Key == perkStatusRecord.PerkKey)
                        {
                            PerkStatus.CreateInstance(perkStatusRecord, owner, perkInstance);
                            break;
                        }

                    break;
                }
                case PoisonRecord poisonRecord:   Poison.CreateInstance(poisonRecord, owner, ref allCharacters); break;
                case RiposteRecord riposteRecord: Riposte.CreateInstance(riposteRecord, owner); break;
                default:                          throw new ArgumentOutOfRangeException(nameof(record));
            }
        }

        public static float Percentage(this IStaminaModule staminaModule)
        {
            return (float) staminaModule.GetCurrent() / staminaModule.ActualMax;
        }
    }
}