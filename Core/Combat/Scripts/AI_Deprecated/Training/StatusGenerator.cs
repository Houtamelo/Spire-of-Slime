/*using System;
using System.Collections.Generic;
using System.Linq;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Effects.Types.Arousal;
using Core.Combat.Scripts.Effects.Types.BuffOrDebuff;
using Core.Combat.Scripts.Effects.Types.Guarded;
using Core.Combat.Scripts.Effects.Types.Heal;
using Core.Combat.Scripts.Effects.Types.Lust;
using Core.Combat.Scripts.Effects.Types.Marked;
using Core.Combat.Scripts.Effects.Types.Move;
using Core.Combat.Scripts.Effects.Types.OvertimeHeal;
using Core.Combat.Scripts.Effects.Types.Poison;
using Core.Combat.Scripts.Effects.Types.Riposte;
using Core.Combat.Scripts.Effects.Types.Stun;

namespace Core.Combat.Scripts.AI.Training
{
    public static class StatusGenerator
    {
        private static readonly IReadOnlyList<PositiveStatus> PositiveStatusList;
        private static readonly IReadOnlyList<NegativeStatus> NegativeStatusList;
        private static readonly IReadOnlyList<CombatStat> PossibleBuffOrDebuffs;

        static StatusGenerator()
        {
            PositiveStatusList = Enum.GetValues(typeof(PositiveStatus)).Cast<PositiveStatus>().ToArray();
            NegativeStatusList = Enum.GetValues(typeof(NegativeStatus)).Cast<NegativeStatus>().ToArray();
            List<CombatStat> list =  Enum.GetValues(typeof(CombatStat)).Cast<CombatStat>().ToList();
            PossibleBuffOrDebuffs = list;
        }

        public static IBaseStatusScript GenerateRandom(Random random, bool isPositive) => isPositive ? GenerateRandomPositive(random) : GenerateRandomNegative(random);

        private static IBaseStatusScript GenerateRandomNegative(Random random)
        {
            NegativeStatus status = NegativeStatusList[random.Next(0, NegativeStatusList.Count)];

            switch (status)
            {
                case NegativeStatus.Arousal:
                {
                    float duration = (float)random.NextDouble() * 3f + 1f;
                    float applyChance = (float)random.NextDouble() + 0.4f;
                    int lustPerTick = random.Next(2, 7);
                    return new ArousalScript(false, duration, applyChance, lustPerTick);
                }
                case NegativeStatus.Debuff:
                {
                    float duration = (float)random.NextDouble() * 3f + 1f;
                    float applyChance = (float)random.NextDouble() + 0.4f;
                    CombatStat attribute = PossibleBuffOrDebuffs[random.Next(0, PossibleBuffOrDebuffs.Count)];
                    float delta = (float)random.NextDouble() * 0.3f + 0.1f;
                    return new BuffOrDebuffScript(false, duration, applyChance, attribute, delta);
                }
                case NegativeStatus.Lust:
                {
                    int lustLower = random.Next(8, 17);
                    int lustUpper = random.Next(lustLower, lustLower + 17);
                    return new LustScript(lustLower, lustUpper);
                }
                case NegativeStatus.Mark:
                {
                    float duration = (float)random.NextDouble() * 3f + 1f;
                    return new MarkedScript(false, duration);
                }
                case NegativeStatus.Move:
                {
                    float applyChance = (float)random.NextDouble() + 0.4f;
                    int moveDelta = random.Next(-3, 4);
                    return new MoveScript(applyChance, moveDelta);
                }
                case NegativeStatus.Poison:
                {
                    float duration = (float)random.NextDouble() * 3f + 1f;
                    float applyChance = (float)random.NextDouble() + 0.4f;
                    int damagePerTick = random.Next(2, 7);
                    return new PoisonScript(false, duration, applyChance, damagePerTick);
                }
                case NegativeStatus.Stun:
                {
                    float duration = (float)random.NextDouble() * 1.5f + 0.5f;
                    return new StunScript(duration);
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static IBaseStatusScript GenerateRandomPositive(Random random)
        {
            PositiveStatus status = PositiveStatusList[random.Next(0, PositiveStatusList.Count)];

            switch (status)
            {
                case PositiveStatus.Buff:
                {
                    float duration = (float)random.NextDouble() * 3f + 1f;
                    CombatStat attribute = PossibleBuffOrDebuffs[random.Next(0, PossibleBuffOrDebuffs.Count)];
                    float delta = (float)random.NextDouble() * 0.3f + 0.1f;
                    return new BuffOrDebuffScript(Permanent: false, duration, BaseApplyChance: 1, attribute, delta);
                }
                case PositiveStatus.Guarded:
                {
                    float duration = (float)random.NextDouble() * 3f + 1f;
                    return new GuardedScript(false, duration);
                }
                case PositiveStatus.Heal:
                {
                    float power = (float)random.NextDouble() * 0.8f + 0.5f;
                    return new HealScript(power);
                }
                case PositiveStatus.Mark:
                {
                    float duration = (float)random.NextDouble() * 3f + 1f;
                    return new MarkedScript(false, duration);
                }
                case PositiveStatus.Move:
                {
                    int moveDelta = random.Next(-3, 4);
                    return new MoveScript(1, moveDelta);
                }
                case PositiveStatus.OvertimeHeal:
                {
                    float duration = (float)random.NextDouble() * 3f + 1f;
                    int healPerTick = random.Next(2, 7);
                    return new OvertimeHealScript(false, duration, healPerTick);
                }
                case PositiveStatus.Riposte:
                {
                    float duration = (float)random.NextDouble() * 3f + 1f;
                    return new RiposteScript(false, duration, 1f);
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        private enum PositiveStatus
        {
            Buff,
            Guarded,
            Heal,
            Mark,
            Move,
            OvertimeHeal,
            Riposte,
        }
        
        private enum NegativeStatus
        {
            Arousal,
            Debuff,
            Lust,
            Mark,
            Move,
            Poison,
            Stun
        }
    }
}*/