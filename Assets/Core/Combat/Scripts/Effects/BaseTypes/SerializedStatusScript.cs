using System;
using System.Collections.Generic;
using Core.Combat.Scripts.Behaviour.UI;
using Core.Combat.Scripts.Effects.Types.Arousal;
using Core.Combat.Scripts.Effects.Types.BuffOrDebuff;
using Core.Combat.Scripts.Effects.Types.Grappled;
using Core.Combat.Scripts.Effects.Types.Guarded;
using Core.Combat.Scripts.Effects.Types.Heal;
using Core.Combat.Scripts.Effects.Types.Lust;
using Core.Combat.Scripts.Effects.Types.Marked;
using Core.Combat.Scripts.Effects.Types.Move;
using Core.Combat.Scripts.Effects.Types.OvertimeHeal;
using Core.Combat.Scripts.Effects.Types.Perk;
using Core.Combat.Scripts.Effects.Types.Poison;
using Core.Combat.Scripts.Effects.Types.Riposte;
using Core.Combat.Scripts.Effects.Types.Stun;
using Core.Combat.Scripts.Effects.Types.Summon;
using Core.Combat.Scripts.Effects.Types.Tempt;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Interfaces;
using Core.Combat.Scripts.Perks;
using Core.Utils.Patterns;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils.Patterns;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace Core.Combat.Scripts.Effects.BaseTypes
{
    [Serializable]
    public class SerializedStatusScript : IEquatable<SerializedStatusScript>, IEquatable<IBaseStatusScript>, IBaseStatusScript
    {
        [SerializeField, ValidateInput(nameof(IsValidEffectType))]
        private EffectType effectType;

        [SerializeField, ShowIf(nameof(ShowPermanent))]
        private bool permanent;

        [SerializeField, ShowIf(nameof(ShowDuration)), LabelText("Duration"), PropertyRange(0f, 10f)]
        private float baseDuration;

        [SerializeField, ShowIf(nameof(ShowApplyChance)), LabelText("Apply Chance"), PropertyRange(0f, 2f), SuffixLabel("@($value * 100f) + \"%\"")]
        private float baseApplyChance;

        [SerializeField, ShowIf(nameof(ShowBaseValuePerSecond)), LabelText(@"$BaseValuePerSecondLabel"), PropertyRange(1, 40)]
        private uint baseValuePerSecond;
        
        [SerializeField, ShowIf(nameof(ShowFloatValuePerSecond)), LabelText(@"$FloatValuePerSecondLabel"), PropertyRange(-2f, 2f)]
        private float baseFloatValuePerSecond;

        [SerializeField, ShowIf(nameof(ShowStat)), LabelText("Affected Stat")]
        private CombatStat buffStat;

        [SerializeField, ShowIf(nameof(ShowStat)), LabelText("Delta"), PropertyRange(-2f, 2f)]
        private float buffBaseDelta;

        [SerializeField, ShowIf(nameof(ShowTriggerName))]
        private string triggerName;

        [SerializeField, ShowIf(nameof(ShowTriggerName))]
        private float graphicalX;

        [SerializeField, ShowIf(nameof(ShowHealPower)), PropertyRange(0, 3f), SuffixLabel("@($value * 100f) + \"%\"")]
        private float healPower;

        [SerializeField, ShowIf(nameof(ShowLust)), LabelText("Lust Lower"), PropertyRange(-50, 50), ValidateInput(nameof(IsLustUpperHigherThanLower))]
        private int lustLower;

        [SerializeField, ShowIf(nameof(ShowLust)), LabelText("Lust Upper"), PropertyRange(-50, 50)]
        private int lustUpper;

        [SerializeField, ShowIf(nameof(ShowMoveDelta)), LabelText("Delta"), PropertyRange(-3, 3), ValidateInput(nameof(IsEffectCorrectlyBuffOrDebuff)), InfoBox("Positive means retreating, negative means advancing")]
        private int moveDelta;

        [SerializeField, ShowIf(nameof(ShowPerkToApply)), LabelText("Perk")]
        private PerkScriptable perkToApply;

        [SerializeField, ShowIf(nameof(ShowRiposteMultiplier)), LabelText("Multiplier"), PropertyRange(0f, 2f), SuffixLabel("@($value * 100f) + \"%\"")]
        private float baseRiposteMultiplier;

        [SerializeField, ShowIf(nameof(ShowSummon)), Required]
        private CharacterScriptable characterToSummon;

        [SerializeField, ShowIf(nameof(ShowTemptation))]
        private float temptationPower;

        [SerializeField, ShowIf(nameof(ShowSummon)), InfoBox("The AI uses this to calculate how good the summoned character's skill are. Default value should be 1.")]
        private float pointsMultiplier;

    #region PropertyStates
        private bool IsValidEffectType()
        {
           return effectType switch
            {
                EffectType.Arousal        => true,
                EffectType.Buff           => true,
                EffectType.Debuff         => true,
                EffectType.Guarded        => true,
                EffectType.Heal           => true,
                EffectType.Lust           => true,
                EffectType.Marked         => true,
                EffectType.Move           => true,
                EffectType.Perk           => true,
                EffectType.HiddenPerk     => true,
                EffectType.Poison         => true,
                EffectType.Riposte        => true,
                EffectType.Stun           => true,
                EffectType.Summon         => true,
                EffectType.OvertimeHeal   => true,
                EffectType.Temptation     => true,
                EffectType.LustGrappled   => false,
                EffectType.NemaExhaustion => false,
                EffectType.Mist           => false,
                _                         => throw new ArgumentException(message: $"Unhandled EffectType: {effectType}")
            };
        }
        
        private bool IsEffectCorrectlyBuffOrDebuff()
        {
            if (effectType is not EffectType.Buff and not EffectType.Debuff)
                return true;
            
            return buffBaseDelta switch
            {
                > 0 => effectType == EffectType.Buff,
                < 0 => effectType == EffectType.Debuff,
                _   => true
            };
        }
        
        private bool IsLustUpperHigherThanLower() => lustUpper > lustLower;
        
        private bool ShowDuration =>
            permanent == false
            && effectType switch
            {
                EffectType.Buff           => true,
                EffectType.Debuff         => true,
                EffectType.Poison         => true,
                EffectType.Arousal        => true,
                EffectType.Riposte        => true,
                EffectType.OvertimeHeal   => true,
                EffectType.Marked         => true,
                EffectType.Stun           => true,
                EffectType.Guarded        => true,
                EffectType.LustGrappled   => true,
                EffectType.Perk           => true,
                EffectType.HiddenPerk     => true,
                EffectType.NemaExhaustion => true,
                EffectType.Mist           => true,
                EffectType.Move           => false,
                EffectType.Heal           => false,
                EffectType.Lust           => false,
                EffectType.Summon         => false,
                EffectType.Temptation     => false,
                _                         => throw new ArgumentException(message: $"Unhandled EffectType: {effectType}")
            };
        private bool ShowPermanent => effectType switch
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
            EffectType.Perk           => true,
            EffectType.HiddenPerk     => true,
            EffectType.NemaExhaustion => true,
            EffectType.Mist           => true,
            EffectType.Stun           => false,
            EffectType.Move           => false,
            EffectType.Heal           => false,
            EffectType.Lust           => false,
            EffectType.Summon         => false,
            EffectType.Temptation     => false,
            _                         => throw new ArgumentException(message: $"Unhandled EffectType: {effectType}")
        };
        private bool ShowApplyChance =>
            effectType switch
            {
                EffectType.Buff    => true,
                EffectType.Debuff  => true,
                EffectType.Poison  => true,
                EffectType.Arousal => true,
                EffectType.Move    => true,
                _                  => false
            };
        private bool ShowBaseValuePerSecond =>
            effectType switch
            {
                EffectType.Poison       => true,
                EffectType.Arousal      => true,
                EffectType.OvertimeHeal => true,
                EffectType.LustGrappled => true,
                _                       => false
            };

        private bool ShowFloatValuePerSecond =>
            effectType switch
            {
                EffectType.LustGrappled => true,
                _                       => false
            };
        
        [UsedImplicitly] private string BaseValuePerSecondLabel => effectType switch
        {
            EffectType.Poison       => "Damage per second",
            EffectType.Arousal      => "Lust per second",
            EffectType.OvertimeHeal => "Heal per second",
            EffectType.LustGrappled => "Lust per second",
            _                       => "Base Value Per Second"
        };
        
        [UsedImplicitly] private string FloatValuePerSecondLabel => effectType switch
        {
            EffectType.LustGrappled => "Temptation per second",
            _                       => "Float Value Per Second"
        };

        private bool ShowStat => effectType is EffectType.Buff or EffectType.Debuff;
        private bool ShowTriggerName => effectType is EffectType.LustGrappled;
        private bool ShowHealPower => effectType is EffectType.Heal;
        private bool ShowLust => effectType is EffectType.Lust;
        private bool ShowMoveDelta => effectType is EffectType.Move;
        private bool ShowPerkToApply => effectType is EffectType.Perk or EffectType.HiddenPerk;
        private bool ShowRiposteMultiplier => effectType is EffectType.Riposte;
        private bool ShowSummon => effectType is EffectType.Summon;
        private bool ShowTemptation => effectType is EffectType.Temptation;
        #endregion
        
        public IActualStatusScript Deserialize()
        {
            return effectType switch
            {
                EffectType.Buff         => new BuffOrDebuffScript(permanent, baseDuration, baseApplyChance, buffStat, buffBaseDelta),
                EffectType.Debuff       => new BuffOrDebuffScript(permanent, baseDuration, baseApplyChance, buffStat, buffBaseDelta),
                EffectType.Poison       => new PoisonScript(permanent, baseDuration, baseApplyChance, baseValuePerSecond),
                EffectType.Arousal      => new ArousalScript(permanent, baseDuration, baseApplyChance, baseValuePerSecond),
                EffectType.Riposte      => new RiposteScript(permanent, baseDuration, baseRiposteMultiplier),
                EffectType.OvertimeHeal => new OvertimeHealScript(permanent, baseDuration, baseValuePerSecond),
                EffectType.Marked       => new MarkedScript(permanent, baseDuration),
                EffectType.Stun         => new StunScript(baseDuration),
                EffectType.Guarded      => new GuardedScript(permanent, baseDuration),
                EffectType.Move         => new MoveScript(baseApplyChance, moveDelta),
                EffectType.LustGrappled => new LustGrappledScript(permanent, baseDuration, triggerName, graphicalX, baseValuePerSecond, baseFloatValuePerSecond),
                EffectType.Perk         => new PerkStatusScript(permanent, baseDuration, perkToApply, IsHidden: false),
                EffectType.HiddenPerk   => new PerkStatusScript(permanent, baseDuration, perkToApply, IsHidden: true),
                EffectType.Heal         => new HealScript(healPower),
                EffectType.Lust         => new LustScript(lustLower, lustUpper),
                EffectType.Summon       => new SummonScript(characterToSummon, pointsMultiplier),
                EffectType.Temptation   => new TemptScript(temptationPower),
                _                       => throw new ArgumentException(message: $"effectType {effectType} is not supported")
            };
        }

        public bool Equals(IBaseStatusScript other)
        {
            if (ReferenceEquals(null, other))
                return false;

            if (ReferenceEquals(null, this))
                return false;
            
            if (other.EffectType != effectType)
                return false;

            switch (other)
            {
                case ArousalScript arousalScript when effectType is EffectType.Arousal:
                    return arousalScript.BaseLustPerTime == baseValuePerSecond
                           && Math.Abs(arousalScript.BaseApplyChance - baseApplyChance) < 0.0001f
                           && (arousalScript.Permanent == permanent || Mathf.Abs(arousalScript.BaseDuration - baseDuration) < 0.0001f);
                case BuffOrDebuffScript buffOrDebuffScript when effectType is EffectType.Buff or EffectType.Debuff:
                    return Math.Abs(buffOrDebuffScript.BaseDelta - buffBaseDelta) < 0.0001f
                           && buffOrDebuffScript.Stat == buffStat
                           && Math.Abs(buffOrDebuffScript.BaseApplyChance - baseApplyChance) < 0.0001f
                           && (buffOrDebuffScript.Permanent == permanent || Mathf.Abs(buffOrDebuffScript.BaseDuration - baseDuration) < 0.0001f);
                case HealScript healScript when effectType is EffectType.Heal:
                    return Math.Abs(healScript.Power - healPower) < 0.0001f;
                case LustScript lustScript when effectType is EffectType.Lust:
                    return lustScript.LustLower == lustLower && lustScript.LustUpper == lustUpper;
                case MarkedScript markedScript when effectType is EffectType.Marked:
                    return markedScript.Permanent == permanent || Mathf.Abs(markedScript.BaseDuration - baseDuration) < 0.0001f;
                case MoveScript moveScript when effectType is EffectType.Move:
                    return Math.Abs(moveScript.BaseApplyChance - baseApplyChance) < 0.0001f
                           && moveScript.MoveDelta == moveDelta;
                case OvertimeHealScript overtimeHealScript when effectType is EffectType.OvertimeHeal:
                    return overtimeHealScript.BaseHealPerTime == baseValuePerSecond
                           && (overtimeHealScript.Permanent == permanent || Mathf.Abs(overtimeHealScript.BaseDuration - baseDuration) < 0.0001f);
                case PerkStatusScript perkStatusScript when effectType is EffectType.Perk or EffectType.HiddenPerk:
                    return (perkStatusScript.Permanent == permanent || Mathf.Abs(perkStatusScript.BaseDuration - baseDuration) < 0.0001f)
                           && perkStatusScript.PerkToApply == perkToApply;
                case PoisonScript poisonScript when effectType is EffectType.Poison:
                    return poisonScript.BasePoisonPerTime == baseValuePerSecond
                           && Math.Abs(poisonScript.BaseApplyChance - baseApplyChance) < 0.0001f
                           && (poisonScript.Permanent == permanent || Mathf.Abs(poisonScript.BaseDuration - baseDuration) < 0.0001f);
                case RiposteScript riposteScript when effectType is EffectType.Riposte:
                    return Math.Abs(riposteScript.BasePower - baseRiposteMultiplier) < 0.0001f
                           && (riposteScript.Permanent == permanent || Mathf.Abs(riposteScript.BaseDuration - baseDuration) < 0.0001f);
                case StunScript stunScript when effectType is EffectType.Stun:
                    return Math.Abs(stunScript.BaseDuration - baseDuration) < 0.0001f;
                case GuardedScript guardedScript when effectType is EffectType.Guarded:
                    return guardedScript.Permanent == permanent || Mathf.Abs(guardedScript.BaseDuration - baseDuration) < 0.0001f;
                case LustGrappledScript lustGrappledScript when effectType is EffectType.LustGrappled:
                    return lustGrappledScript.BaseLustPerTime == baseValuePerSecond
                           && (lustGrappledScript.Permanent == permanent || Mathf.Abs(lustGrappledScript.BaseDuration - baseDuration) < 0.0001f)
                           && lustGrappledScript.TriggerName == triggerName;
                case SummonScript summonScript when effectType is EffectType.Summon:
                    return EqualityComparer<ICharacterScript>.Default.Equals(summonScript.CharacterToSummon, characterToSummon) && Mathf.Abs(summonScript.PointsMultiplier - pointsMultiplier) < 0.0001f;
                case TemptScript temptationScript when effectType is EffectType.Temptation:
                    return Mathf.Abs(temptationScript.Power - temptationPower) < 0.0001f;
            }

            return false;
        }
        
        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is StatusScript other && Equals(other);
        }

        public bool Equals(SerializedStatusScript other)
        {
            bool isOtherNull = ReferenceEquals(null, other);
            bool isSelfNull = ReferenceEquals(null, this);
            if (isSelfNull && isOtherNull)
                return true;
            
            if (isOtherNull || isSelfNull || other.effectType != effectType)
                return false;

            return effectType switch
            {
                EffectType.Arousal => other.baseValuePerSecond == baseValuePerSecond 
                                   && Mathf.Abs(other.baseApplyChance - baseApplyChance) < 0.0001f 
                                   && (other.permanent == permanent || Mathf.Abs(other.baseDuration - baseDuration) < 0.0001f),
                EffectType.Buff => Mathf.Abs(other.buffBaseDelta - buffBaseDelta) < 0.0001f
                                && other.buffStat == buffStat
                                && Mathf.Abs(other.baseApplyChance - baseApplyChance) < 0.0001f
                                && (other.permanent == permanent || Mathf.Abs(other.baseDuration - baseDuration) < 0.0001f),
                EffectType.Debuff => Mathf.Abs(other.buffBaseDelta - buffBaseDelta) < 0.0001f
                                  && other.buffStat == buffStat
                                  && Mathf.Abs(other.baseApplyChance - baseApplyChance) < 0.0001f
                                  && (other.permanent == permanent || Mathf.Abs(other.baseDuration - baseDuration) < 0.0001f),
                EffectType.Heal           => Mathf.Abs(other.healPower - healPower) < 0.0001f,
                EffectType.Lust           => other.lustLower == lustLower && other.lustUpper == lustUpper,
                EffectType.Marked         => other.permanent == permanent || Mathf.Abs(other.baseDuration - baseDuration) < 0.0001f,
                EffectType.Move           => Mathf.Abs(other.baseApplyChance - baseApplyChance) < 0.0001f && other.moveDelta == moveDelta,
                EffectType.OvertimeHeal   => other.baseValuePerSecond == baseValuePerSecond && (other.permanent == permanent || Mathf.Abs(other.baseDuration - baseDuration) < 0.0001f),
                EffectType.Perk           => (other.permanent == permanent || Mathf.Abs(other.baseDuration - baseDuration) < 0.0001f) && other.perkToApply == perkToApply,
                EffectType.HiddenPerk     => (other.permanent == permanent || Mathf.Abs(other.baseDuration - baseDuration) < 0.0001f) && other.perkToApply == perkToApply,
                EffectType.Poison         => other.baseValuePerSecond == baseValuePerSecond 
                                          && Mathf.Abs(other.baseApplyChance - baseApplyChance) < 0.0001f 
                                          && (other.permanent == permanent || Math.Abs(other.baseDuration - baseDuration) < 0.0001f),
                EffectType.Riposte        => Mathf.Abs(other.baseRiposteMultiplier - baseRiposteMultiplier) < 0.0001f 
                                          && (other.permanent == permanent || Mathf.Abs(other.baseDuration - baseDuration) < 0.0001f),
                EffectType.Stun           => Mathf.Abs(other.baseDuration - baseDuration) < 0.0001f,
                EffectType.Guarded        => other.permanent == permanent || Mathf.Abs(other.baseDuration - baseDuration) < 0.0001f,
                EffectType.LustGrappled   => other.baseValuePerSecond == baseValuePerSecond 
                                          && (other.permanent == permanent || Mathf.Abs(other.baseDuration - baseDuration) < 0.0001f) && other.triggerName == triggerName,
                EffectType.Summon         => other.characterToSummon == characterToSummon && Mathf.Abs(other.pointsMultiplier - pointsMultiplier) < 0.0001f,
                EffectType.Temptation     => Mathf.Abs(other.temptationPower - temptationPower) < 0.0001f,
                EffectType.NemaExhaustion => false,
                EffectType.Mist           => false,
                _                         => throw new ArgumentException($"Unhandled effect type {effectType}")
            };
        }

        public override int GetHashCode()
        {
            return effectType switch
            {
                EffectType.Buff           => HashCode.Combine(effectType, buffStat,              buffBaseDelta,   baseApplyChance, permanent, baseDuration),
                EffectType.Debuff         => HashCode.Combine(effectType, buffStat,              buffBaseDelta,   baseApplyChance, permanent, baseDuration),
                EffectType.Poison         => HashCode.Combine(effectType, baseValuePerSecond,    baseApplyChance, permanent,       baseDuration),
                EffectType.Arousal        => HashCode.Combine(effectType, baseValuePerSecond,    baseApplyChance, permanent,       baseDuration),
                EffectType.Riposte        => HashCode.Combine(effectType, baseRiposteMultiplier, permanent,       baseDuration),
                EffectType.OvertimeHeal   => HashCode.Combine(effectType, baseValuePerSecond,    permanent,       baseDuration),
                EffectType.Marked         => HashCode.Combine(effectType, permanent,             baseDuration),
                EffectType.Stun           => HashCode.Combine(effectType, baseDuration),
                EffectType.Guarded        => HashCode.Combine(effectType, permanent,          baseDuration),
                EffectType.Move           => HashCode.Combine(effectType, baseApplyChance,    moveDelta),
                EffectType.LustGrappled   => HashCode.Combine(effectType, baseValuePerSecond, permanent,    baseDuration, triggerName),
                EffectType.Perk           => HashCode.Combine(effectType, permanent,          baseDuration, perkToApply),
                EffectType.HiddenPerk     => HashCode.Combine(effectType, permanent,          baseDuration, perkToApply),
                EffectType.Heal           => HashCode.Combine(effectType, healPower),
                EffectType.Lust           => HashCode.Combine(effectType, lustLower,         lustUpper),
                EffectType.Summon         => HashCode.Combine(effectType, characterToSummon, pointsMultiplier),
                EffectType.Temptation     => HashCode.Combine(effectType, temptationPower),
                EffectType.NemaExhaustion => effectType.GetHashCode(),
                EffectType.Mist           => effectType.GetHashCode(),
                _                         => throw new ArgumentException($"Unhandled effect type {effectType}")
            };
        }

        public EffectType EffectType => effectType;

        public bool IsPositive =>
            effectType switch
            {
                EffectType.Buff           => true,
                EffectType.Debuff         => false,
                EffectType.Poison         => false,
                EffectType.Arousal        => false,
                EffectType.Riposte        => true,
                EffectType.OvertimeHeal   => true,
                EffectType.Marked         => false,
                EffectType.Stun           => false,
                EffectType.Guarded        => true,
                EffectType.Move           => true,
                EffectType.LustGrappled   => false,
                EffectType.Perk           => perkToApply == null || perkToApply.IsPositive,
                EffectType.HiddenPerk     => perkToApply == null || perkToApply.IsPositive,
                EffectType.Heal           => true,
                EffectType.Lust           => false,
                EffectType.NemaExhaustion => false,
                EffectType.Mist           => false,
                EffectType.Summon         => true,
                EffectType.Temptation     => false,
                _                         => throw new ArgumentException($"Unhandled effect type {effectType}")
            };

        public Option<PredictionIconsDisplay.IconType> GetPredictionIconType() => effectType.GetPredictionIcon();

        public IActualStatusScript GetActual => Deserialize();
    }
}