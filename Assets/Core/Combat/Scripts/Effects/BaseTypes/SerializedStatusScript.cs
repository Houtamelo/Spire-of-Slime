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
using Core.Utils.Math;
using Core.Utils.Patterns;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace Core.Combat.Scripts.Effects.BaseTypes
{
    [Serializable]
    public class SerializedStatusScript : IEquatable<SerializedStatusScript>, IEquatable<IBaseStatusScript>, IBaseStatusScript
    {
        [SerializeField, ValidateInput(nameof(IsValidEffectType))]
        private EffectType effectType;
        public EffectType EffectType => effectType;

        [SerializeField, ShowIf(nameof(ShowPermanent))]
        private bool permanent;

        [SerializeField, ShowIf(nameof(ShowDuration))]
        private TSpan duration;

        [SerializeField, ShowIf(nameof(ShowApplyChance)), Range(-100, 300)]
        private int applyChance;

        [SerializeField, ShowIf(nameof(IsArousal)), Range(0, 20)]
        private int lustPerSecond;
        
        [SerializeField, ShowIf(nameof(IsBuffOrDebuff)), LabelText("Affected Stat")]
        private CombatStat buffStat;

        [SerializeField, ShowIf(nameof(IsBuffOrDebuff)), Range(-200, 200), ValidateInput(nameof(IsEffectCorrectlyBuffOrDebuff))]
        private int buffDelta;
        
        [SerializeField, ShowIf(nameof(IsHeal)), Range(0, 300)]
        private int healPower;

        [SerializeField, ShowIf(nameof(IsLust)), Range(-50, 50), ValidateInput(nameof(IsLustUpperHigherThanLower))]
        private int lustLower;

        [SerializeField, ShowIf(nameof(IsLust)), Range(-50, 50)]
        private int lustUpper;
        
        [SerializeField, ShowIf(nameof(IsLust)), Range(0, 500)]
        private int lustPower;
        
        [SerializeField, ShowIf(nameof(IsMove)), Range(-3, 3), InfoBox("Positive means retreating, negative means advancing")]
        private int moveDelta;
        
        [SerializeField, ShowIf(nameof(IsPerk)), Required]
        private PerkScriptable perkToApply;
        
        [SerializeField, ShowIf(nameof(IsPoison)), Range(0, 40)]
        private int poisonPerSecond;
        
        [SerializeField, ShowIf(nameof(IsRiposte)), Range(0, 300)]
        private int ripostePower;
        
        [SerializeField, ShowIf(nameof(IsStun)), Range(0, 400)]
        private int stunPower;
        
        [SerializeField, ShowIf(nameof(IsOvertimeHeal)), Range(0, 40)]
        private int healPerSecond;
        
        [SerializeField, ShowIf(nameof(IsTemptation)), Range(0, 300)]
        private int temptationPower;
        
        [SerializeField, ShowIf(nameof(IsSummon)), Required]
        private CharacterScriptable characterToSummon;

        [SerializeField, ShowIf(nameof(IsSummon)), InfoBox("The AI uses this to calculate how good the summoned character's skill are. Default value should be 1.")]
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
            
            return buffDelta switch
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
                EffectType.Perk           => true,
                EffectType.HiddenPerk     => true,
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
            EffectType.Perk           => true,
            EffectType.HiddenPerk     => true,
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

        private bool IsArousal => effectType is EffectType.Arousal;
        private bool IsPoison => effectType is EffectType.Poison;
        private bool IsStun => effectType is EffectType.Stun;
        private bool IsBuffOrDebuff => effectType is EffectType.Buff or EffectType.Debuff;
        private bool IsHeal => effectType is EffectType.Heal;
        private bool IsOvertimeHeal => effectType is EffectType.OvertimeHeal;
        private bool IsLust => effectType is EffectType.Lust;
        private bool IsMove => effectType is EffectType.Move;
        private bool IsPerk => effectType is EffectType.Perk or EffectType.HiddenPerk;
        private bool IsRiposte => effectType is EffectType.Riposte;
        private bool IsSummon => effectType is EffectType.Summon;
        private bool IsTemptation => effectType is EffectType.Temptation;
    #endregion
        
        [NotNull]
        public IActualStatusScript Deserialize()
        {
            return effectType switch
            {
                EffectType.Buff         => new BuffOrDebuffScript(permanent, duration, applyChance, buffStat, buffDelta),
                EffectType.Debuff       => new BuffOrDebuffScript(permanent, duration, applyChance, buffStat, buffDelta),
                EffectType.Poison       => new PoisonScript(permanent, duration, applyChance, poisonPerSecond),
                EffectType.Arousal      => new ArousalScript(permanent, duration, applyChance, lustPerSecond),
                EffectType.Riposte      => new RiposteScript(permanent, duration, ripostePower),
                EffectType.OvertimeHeal => new OvertimeHealScript(permanent, duration, healPerSecond),
                EffectType.Marked       => new MarkedScript(permanent, duration),
                EffectType.Stun         => new StunScript(stunPower),
                EffectType.Guarded      => new GuardedScript(permanent, duration),
                EffectType.Move         => new MoveScript(applyChance, moveDelta),
                EffectType.Perk         => new PerkStatusScript(permanent, duration, perkToApply, IsHidden: false),
                EffectType.HiddenPerk   => new PerkStatusScript(permanent, duration, perkToApply, IsHidden: true),
                EffectType.Heal         => new HealScript(healPower),
                EffectType.Lust         => new LustScript(lustLower, lustUpper, lustPower),
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

            return other switch
            {
                ArousalScript arousalScript when effectType is EffectType.Arousal
                    => arousalScript.BaseLustPerSecond == lustPerSecond
                    && arousalScript.BaseApplyChance == applyChance
                    && arousalScript.Permanent == permanent
                    && (permanent == true || arousalScript.BaseDuration == duration),
                BuffOrDebuffScript buffOrDebuffScript when effectType is EffectType.Buff or EffectType.Debuff
                    => buffOrDebuffScript.BaseDelta == buffDelta
                    && buffOrDebuffScript.Stat == buffStat
                    && buffOrDebuffScript.BaseApplyChance == applyChance
                    && buffOrDebuffScript.Permanent == permanent
                    && (permanent == true || buffOrDebuffScript.BaseDuration == duration),
                HealScript healScript when effectType is EffectType.Heal       
                    => healScript.Power == healPower,
                LustScript lustScript when effectType is EffectType.Lust       
                    => lustScript.LustLower == lustLower 
                    && lustScript.LustUpper == lustUpper,
                MarkedScript markedScript when effectType is EffectType.Marked 
                    => markedScript.Permanent == permanent 
                    && (permanent == true || markedScript.BaseDuration == duration),
                MoveScript moveScript when effectType is EffectType.Move       
                    => moveScript.BaseApplyChance == applyChance 
                    && moveScript.MoveDelta == moveDelta,
                OvertimeHealScript overtimeHealScript when effectType is EffectType.OvertimeHeal 
                    => overtimeHealScript.BaseHealPerTime == healPerSecond
                    && overtimeHealScript.Permanent == permanent
                    && (permanent == true || overtimeHealScript.BaseDuration == duration),
                PerkStatusScript perkStatusScript when effectType is EffectType.Perk or EffectType.HiddenPerk 
                    => perkStatusScript.PerkToApply == perkToApply
                    && perkStatusScript.Permanent == permanent
                    && (permanent == true || perkStatusScript.BaseDuration == duration),
                PoisonScript poisonScript when effectType is EffectType.Poison
                    => poisonScript.BasePoisonPerTime == poisonPerSecond
                    && poisonScript.BaseApplyChance == applyChance
                    && poisonScript.Permanent == permanent
                    && (permanent == true || poisonScript.BaseDuration == duration),
                RiposteScript riposteScript when effectType is EffectType.Riposte 
                    => riposteScript.BasePower == ripostePower 
                    && riposteScript.Permanent == permanent 
                    && (permanent == true || riposteScript.BaseDuration == duration),
                StunScript stunScript when effectType is EffectType.Stun         
                    => stunScript.StunPower == stunPower,
                GuardedScript guardedScript when effectType is EffectType.Guarded 
                    => guardedScript.Permanent == permanent 
                    && (permanent == true || guardedScript.BaseDuration == duration),
                SummonScript summonScript when effectType is EffectType.Summon
                    => EqualityComparer<ICharacterScript>.Default.Equals(summonScript.CharacterToSummon, characterToSummon)
                    && Mathf.Abs(summonScript.PointsMultiplier - pointsMultiplier) < 0.0001f,
                TemptScript temptationScript when effectType is EffectType.Temptation 
                    => temptationScript.Power == temptationPower,
                _ => false
            };
        }

        public override bool Equals(object obj) => ReferenceEquals(this, obj) || (obj is StatusScript other && Equals(other));

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
                EffectType.Arousal
                    => other.lustPerSecond == lustPerSecond
                    && other.applyChance == applyChance
                    && other.permanent == permanent
                    && (permanent == true || other.duration == duration),
                EffectType.Buff or EffectType.Debuff
                    => other.buffDelta == buffDelta
                    && other.buffStat == buffStat
                    && other.applyChance == applyChance
                    && other.permanent == permanent
                    && (permanent == true || other.duration == duration),
                EffectType.Heal       
                    => other.healPower == healPower,
                EffectType.Lust       
                    => other.lustLower == lustLower 
                    && other.lustUpper == lustUpper,
                EffectType.Marked 
                    => other.permanent == permanent 
                    && (permanent == true || other.duration == duration),
                EffectType.Move       
                    => other.applyChance == applyChance 
                    && other.moveDelta == moveDelta,
                EffectType.OvertimeHeal 
                    => other.healPerSecond == healPerSecond
                    && other.permanent == permanent
                    && (permanent == true || other.duration == duration),
                EffectType.Perk or EffectType.HiddenPerk 
                    => other.perkToApply == perkToApply
                    && other.permanent == permanent
                    && (permanent == true || other.duration == duration),
                EffectType.Poison
                    => other.poisonPerSecond == poisonPerSecond
                    && other.applyChance == applyChance
                    && other.permanent == permanent
                    && (permanent == true || other.duration == duration),
                EffectType.Riposte 
                    => other.ripostePower == ripostePower 
                    && other.permanent == permanent 
                    && (permanent == true || other.duration == duration),
                EffectType.Stun         
                    => other.stunPower == stunPower,
                EffectType.Guarded 
                    => other.permanent == permanent 
                    && (permanent == true || other.duration == duration),
                EffectType.Summon 
                    => EqualityComparer<ICharacterScript>.Default.Equals(other.characterToSummon, characterToSummon)
                    && Mathf.Abs(other.pointsMultiplier - pointsMultiplier) < 0.0001f,
                EffectType.Temptation 
                    => other.temptationPower == temptationPower,
                _ => false
            };
        }

        public override int GetHashCode()
        {
            return effectType switch
            {
                EffectType.Buff         => HashCode.Combine(effectType, buffStat, buffDelta, applyChance, permanent, duration),
                EffectType.Debuff       => HashCode.Combine(effectType, buffStat, buffDelta, applyChance, permanent, duration),
                EffectType.Poison       => HashCode.Combine(effectType, poisonPerSecond, applyChance, permanent, duration),
                EffectType.Arousal      => HashCode.Combine(effectType, lustPerSecond, applyChance, permanent, duration),
                EffectType.Riposte      => HashCode.Combine(effectType, ripostePower, permanent, duration),
                EffectType.OvertimeHeal => HashCode.Combine(effectType, healPerSecond, permanent, duration),
                EffectType.Marked       => HashCode.Combine(effectType, permanent, duration),
                EffectType.Stun         => HashCode.Combine(effectType, stunPower),
                EffectType.Guarded      => HashCode.Combine(effectType, permanent, duration),
                EffectType.Move         => HashCode.Combine(effectType, applyChance, moveDelta),
                EffectType.Perk         => HashCode.Combine(effectType, permanent, duration, perkToApply),
                EffectType.HiddenPerk   => HashCode.Combine(effectType, permanent, duration, perkToApply),
                EffectType.Heal         => HashCode.Combine(effectType, healPower),
                EffectType.Lust         => HashCode.Combine(effectType, lustLower, lustUpper),
                EffectType.Summon       => HashCode.Combine(effectType, characterToSummon, pointsMultiplier),
                EffectType.Temptation   => HashCode.Combine(effectType, temptationPower),
                _                       => throw new ArgumentException($"Unhandled effect type {effectType}")
            };
        }

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
                EffectType.Perk           => perkToApply == null || perkToApply.IsPositive,
                EffectType.HiddenPerk     => perkToApply == null || perkToApply.IsPositive,
                EffectType.Heal           => true,
                EffectType.Lust           => false,
                EffectType.Summon         => true,
                EffectType.Temptation     => false,
                _                         => throw new ArgumentException($"Unhandled effect type {effectType}")
            };

        public Option<PredictionIconsDisplay.IconType> GetPredictionIconType() => effectType.GetPredictionIcon();

        [NotNull]
        public IActualStatusScript GetActual => Deserialize();
    }
}