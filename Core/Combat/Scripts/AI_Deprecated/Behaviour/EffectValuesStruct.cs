/*using System;
using System.Buffers;
using System.Collections.Generic;
using Combat.Behaviour;
using NetFabric.Hyperlinq;
using UnityEngine;
using Utility;

namespace Combat.AI.Behaviour
{
    public ref struct EffectValuesStruct
    {
        public float ArousalApplyChance { get; set; }
        public float EstimatedArousal { get; set; }
        public float GuardedDuration { get; set; }
        public float LustAmount { get; set; }
        public float MarkDuration { get; set; }
        public float MoveApplyChance { get; set; }
        public float MoveDelta { get; set; }
        public float EstimatedOvertimeHeal { get; set; }
        public float PoisonApplyChance { get; set; }
        public float EstimatedPoisonDamage { get; set; }
        public float StealthDuration { get; set; }
        public float StunDuration { get; set; }
        public float RiposteDuration { get; set; }
        public float RiposteDamageMultiplier { get; set; }
        public float AverageDamage { get; set; }
        public Dictionary<CombatStat, (float, float)> BuffsAndDebuffs { get; }
        public Character Self { get; }
        public Character Target { get; }
        
        public EffectValuesStruct(Dictionary<CombatStat, (float, float)> reusableBuffsAndDebuffs, Character self, Character target)
        {
            Self = self;
            Target = target;
            ArousalApplyChance = 0;
            EstimatedArousal = 0;
            GuardedDuration = 0;
            LustAmount = 0;
            MarkDuration = 0;
            MoveApplyChance = 0;
            MoveDelta = 0;
            EstimatedOvertimeHeal = 0;
            PoisonApplyChance = 0;
            EstimatedPoisonDamage = 0;
            StealthDuration = 0;
            StunDuration = 0;
            AverageDamage = 0;
            RiposteDuration = 0;
            RiposteDamageMultiplier = 0;
            BuffsAndDebuffs = reusableBuffsAndDebuffs;
        }

        public void NormalizeValues()
        {
            ArousalApplyChance = ArousalApplyChance.NormalizeClamped(0f, 2f);
            EstimatedArousal = EstimatedArousal.NormalizeClamped(0f, Character.MaxLust);
            GuardedDuration = GuardedDuration.NormalizeClamped(0f, Character.TimeUpperBound);
            LustAmount = LustAmount.NormalizeClamped(0f, Character.MaxLust);
            MarkDuration = MarkDuration.NormalizeClamped(0f, Character.TimeUpperBound);
            MoveApplyChance = MoveApplyChance.NormalizeClamped(0f, 2f);
            MoveDelta = MoveDelta.NormalizeClamped(-3, 3);
            EstimatedOvertimeHeal = EstimatedOvertimeHeal.NormalizeClamped(0f, Character.StaminaBaseConstant);
            PoisonApplyChance = PoisonApplyChance.NormalizeClamped(0f, 2f);
            EstimatedPoisonDamage = EstimatedPoisonDamage.NormalizeClamped(0f, Character.StaminaBaseConstant);
            StealthDuration = StealthDuration.NormalizeClamped(0f, Character.TimeUpperBound);
            StunDuration = StunDuration.NormalizeClamped(0f, Character.TimeUpperBound);
            RiposteDuration = RiposteDuration.NormalizeClamped(0f, Character.TimeUpperBound);
            RiposteDamageMultiplier = RiposteDamageMultiplier.NormalizeClamped(0f, 2f);
            AverageDamage = AverageDamage.NormalizeClamped(-Character.StaminaBaseConstant, Character.StaminaBaseConstant);

            using (var lease = BuffsAndDebuffs.AsValueEnumerable().ToArray(ArrayPool<KeyValuePair<CombatStat, (float, float)>>.Shared))
            {
                foreach ((CombatStat stat, (float applyChance, float delta)) in lease)
                    BuffsAndDebuffs[stat] = (applyChance.NormalizeClamped(0f, 2f), delta.NormalizeClamped(-2f, 2f));
            }
        }
        
        public EffectValuesStructEnumerator GetEnumerator() => new(this);
        
        public ref struct EffectValuesStructEnumerator
        {
            private EffectValuesStruct _effectValuesStruct;
            private int _index;

            public EffectValuesStructEnumerator(EffectValuesStruct effectValuesStruct)
            {
                _effectValuesStruct = effectValuesStruct;
                _index = -1;
            }

            public float Current
            {
                get
                {
                    float value = _index switch
                    {
                        0 => _effectValuesStruct.ArousalApplyChance, // 5
                        1 => _effectValuesStruct.EstimatedArousal, // 6
                        2 => _effectValuesStruct.GuardedDuration,
                        3 => _effectValuesStruct.LustAmount,
                        4 => _effectValuesStruct.MarkDuration,
                        5 => _effectValuesStruct.MoveApplyChance,
                        6 => _effectValuesStruct.MoveDelta,
                        7 => _effectValuesStruct.EstimatedOvertimeHeal,
                        8 => _effectValuesStruct.PoisonApplyChance,
                        9 => _effectValuesStruct.EstimatedPoisonDamage,
                        10 => _effectValuesStruct.StealthDuration,
                        11 => _effectValuesStruct.StunDuration,
                        12 => _effectValuesStruct.RiposteDuration,
                        13 => _effectValuesStruct.RiposteDamageMultiplier,
                        14 => _effectValuesStruct.AverageDamage,
                        15 => _effectValuesStruct.BuffsAndDebuffs[CombatStat.Accuracy].Item1,
                        16 => _effectValuesStruct.BuffsAndDebuffs[CombatStat.Accuracy].Item2,
                        17 => _effectValuesStruct.BuffsAndDebuffs[CombatStat.Composure].Item1,
                        18 => _effectValuesStruct.BuffsAndDebuffs[CombatStat.Composure].Item2,
                        19 => _effectValuesStruct.BuffsAndDebuffs[CombatStat.CriticalChance].Item1,
                        20 => _effectValuesStruct.BuffsAndDebuffs[CombatStat.CriticalChance].Item2,
                        21 => _effectValuesStruct.BuffsAndDebuffs[CombatStat.Dodge].Item1,
                        22 => _effectValuesStruct.BuffsAndDebuffs[CombatStat.Dodge].Item2,
                        23 => _effectValuesStruct.BuffsAndDebuffs[CombatStat.Resilience].Item1,
                        24 => _effectValuesStruct.BuffsAndDebuffs[CombatStat.Resilience].Item2,
                        25 => _effectValuesStruct.BuffsAndDebuffs[CombatStat.Speed].Item1,
                        26 => _effectValuesStruct.BuffsAndDebuffs[CombatStat.Speed].Item2,
                        27 => _effectValuesStruct.BuffsAndDebuffs[CombatStat.DamageMultiplier].Item1,
                        28 => _effectValuesStruct.BuffsAndDebuffs[CombatStat.DamageMultiplier].Item2,
                        29 => _effectValuesStruct.BuffsAndDebuffs[CombatStat.DebuffResistance].Item1,
                        30 => _effectValuesStruct.BuffsAndDebuffs[CombatStat.DebuffResistance].Item2,
                        31 => _effectValuesStruct.BuffsAndDebuffs[CombatStat.MoveResistance].Item1,
                        32 => _effectValuesStruct.BuffsAndDebuffs[CombatStat.MoveResistance].Item2,
                        33 => _effectValuesStruct.BuffsAndDebuffs[CombatStat.PoisonResistance].Item1,
                        34 => _effectValuesStruct.BuffsAndDebuffs[CombatStat.PoisonResistance].Item2,
                        35 => _effectValuesStruct.BuffsAndDebuffs[CombatStat.StunSpeed].Item1,
                        36 => _effectValuesStruct.BuffsAndDebuffs[CombatStat.StunSpeed].Item2,
                        37 => _effectValuesStruct.BuffsAndDebuffs[CombatStat.ArousalResistance].Item1,
                        38 => _effectValuesStruct.BuffsAndDebuffs[CombatStat.ArousalResistance].Item2, // 44
                        _ => throw new IndexOutOfRangeException()
                    };
                    
                    Debug.Assert(float.IsFinite(value), $"index: {_index}");
                    return value;
                }
            }

            public bool MoveNext()
            {
                _index++;
                return _index < 39;
            }

            public void Reset() => _index = -1;
        }
    }
}*/