using System.Text;
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
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using Core.Utils.Math;
using Core.Utils.Patterns;
using JetBrains.Annotations;
using UnityEngine;
using static Core.Combat.Scripts.ColorReferences;
using Save = Core.Save_Management.SaveObjects.Save;

namespace Core.Combat.Scripts.Effects
{
    public static class StatusInstanceDescriptions
    {
        private static readonly StringBuilder Builder = new();

        private static readonly LocalizedText
            ArousalTrans = new("status_instance_description_arousal"),            // "Lust {0}pt/s, {1}"
            BuffOrDebuffTrans = new("status_instance_description_buff_debuff"),   // "{0} {1}, {2}"
            LustGrappledTrans = new("status_instance_description_lust_grappled"), // "<i>Having a good time<b>time</b></i>"
            GuardedTrans = new("status_instance_description_guarded"),            // "Guarded by {0}, {1}"
            MarkedTrans = new("status_instance_description_marked"),              // "Marked, {0}"
            OvertimeHealTrans = new("status_instance_description_overtime_heal"), // "Heal {0}pt/s, {1}"
            HiddenPerkTrans = new("status_instance_description_hidden_perk"),     // "Hidden from curious eyes"
            PoisonTrans = new("status_instance_description_poison"),              // "Poison {0}pt/s, {1}"
            RiposteTrans = new("status_instance_description_riposte");            // "Riposte {0}, {1}"
        
        public static Option<string> Get([NotNull] StatusInstance instance)
        {
            StringBuilder builder = instance switch
            {
                Arousal a      
                    => Builder.Override(ArousalTrans.Translate().GetText(a.LustPerSecond.ToString("0"), a.GetCompactDurationString()))
                              .Surround(ArousalRichText),
                BuffOrDebuff b 
                    => Builder.Override(BuffOrDebuffTrans.Translate().GetText(b.GetDelta.WithSymbol(), b.Attribute.LowerCaseName().Translate().GetText(), b.GetCompactDurationString()))
                              .Surround(b.IsPositive ? BuffRichText : DebuffRichText),
                LustGrappled   
                    => Builder.Override(LustGrappledTrans.Translate().GetText())
                              .Surround(LustRichText),
                Guarded g      
                    => Builder.Override(GuardedTrans.Translate().GetText(g.Caster.Script.CharacterName.Translate().GetText(), g.GetCompactDurationString()))
                              .Surround(GuardedRichText),
                Marked m       
                    => Builder.Override(MarkedTrans.Translate().GetText(m.GetCompactDurationString()))
                              .Surround(MarkedRichText),
                MistStatus     
                    => Builder.Clear(),
                NemaExhaustion 
                    => NemaExhaustionDescription(),
                OvertimeHeal o 
                    => Builder.Override(OvertimeHealTrans.Translate().GetText(o.HealPerSecond.ToString("0"), o.GetCompactDurationString()))
                              .Surround(OvertimeHealRichText),
                PerkStatus per 
                    => per.IsHidden ? Builder.Override(HiddenPerkTrans.Translate().GetText()) : Builder.Clear(),
                Poison poi     
                    => Builder.Override(PoisonTrans.Translate().GetText(poi.DamagePerSecond.ToString("0"), poi.GetCompactDurationString()))
                              .Surround(PoisonRichText),
                Riposte r      
                    => RiposteDescription(r),
                _ => LogType(instance)
            };
            
            return builder.Length > 0 ? builder.ToString() : Option.None;
        }
        
        [NotNull]
        private static StringBuilder RiposteDescription([NotNull] Riposte r)
        {
            int characterPower = r.Owner.StatsModule.GetPower();
            (int lower, int upper) damage = r.Owner.StatsModule.GetBaseDamageRaw();
            damage.lower = (damage.lower * characterPower * r.Power) / 10000;
            damage.upper = (damage.upper * characterPower * r.Power) / 10000;
            return Builder.Override(RiposteTrans.Translate().GetText(damage.ToDamageRangeFormat(), r.GetCompactDurationString()))
                          .Surround(RiposteRichText);
        }
        
        [NotNull]
        private static StringBuilder LogType([NotNull] this StatusInstance instance)
        {
            Debug.LogWarning($"Missing description for {instance.GetType()}");
            return Builder.Clear();
        }

        private static readonly LocalizedText
            NemaExhaustionNone = new("status_instance_description_nema_exhaustion_none"),     // "Nema is energetic, no modifiers."
            NemaExhaustionLow = new("status_instance_description_nema_exhaustion_low"),       // "Nema is a little tired, penalties: \n"
            NemaExhaustionMedium = new("status_instance_description_nema_exhaustion_medium"), // "Nema is exhausted, penalties: \n"
            NemaExhaustionHigh = new("status_instance_description_nema_exhaustion_high");     // "Nema is extremely exhausted, penalties: \n"
        
        [NotNull]
        private static StringBuilder NemaExhaustionDescription()
        {
            Builder.Clear();
            Save save = Save.Current;
            if (save == null)
                return Builder;

            ExhaustionEnum exhaustion = save.NemaExhaustionAsEnum;
            LocalizedText title = exhaustion switch
            {
                ExhaustionEnum.None   => NemaExhaustionNone,
                ExhaustionEnum.Low    => NemaExhaustionLow,
                ExhaustionEnum.Medium => NemaExhaustionMedium,
                ExhaustionEnum.High   => NemaExhaustionHigh,
                _                     => throw new System.ArgumentOutOfRangeException(nameof(exhaustion), exhaustion, message: null)
            };
            
            Builder.Append(title.Translate().GetText());

            Option<int> speedModifier = NemaExhaustion.GetSpeedModifier(exhaustion);
            if (speedModifier.IsSome)
                Builder.AppendLine(CombatStat.Speed.UpperCaseName().Translate().GetText(), ' ', speedModifier.Value.WithSymbol());
            
            Option<int> accuracyModifier = NemaExhaustion.GetAccuracyModifier(exhaustion);
            if (accuracyModifier.IsSome)
                Builder.AppendLine(CombatStat.Accuracy.UpperCaseName().Translate().GetText(), ' ', accuracyModifier.Value.WithSymbol());

            Option<int> dodgeModifier = NemaExhaustion.GetDodgeModifier(exhaustion);
            if (dodgeModifier.IsSome)
                Builder.AppendLine(CombatStat.Dodge.UpperCaseName().Translate().GetText(), ' ', dodgeModifier.Value.WithSymbol());
            
            Option<int> resistancesModifier = NemaExhaustion.GetResistancesModifier(exhaustion);
            if (resistancesModifier.IsSome)
            {
                Builder.AppendLine(CombatStat.DebuffResistance.UpperCaseName().Translate().GetText(), ' ', resistancesModifier.Value.WithSymbol());
                Builder.AppendLine(CombatStat.MoveResistance.UpperCaseName().Translate().GetText(),   ' ', resistancesModifier.Value.WithSymbol());
                Builder.AppendLine(CombatStat.PoisonResistance.UpperCaseName().Translate().GetText(), ' ', resistancesModifier.Value.WithSymbol());
            }
            
            Option<int> stunRecoverySpeedModifier = NemaExhaustion.GetStunMitigationModifier(exhaustion);
            if (stunRecoverySpeedModifier.IsSome)
                Builder.AppendLine(CombatStat.StunMitigation.UpperCaseName().Translate().GetText(), ' ', stunRecoverySpeedModifier.Value.WithSymbol());

            Option<int> composureModifier = NemaExhaustion.GetComposureModifier(exhaustion);
            if (composureModifier.IsSome)
                Builder.AppendLine(CombatStat.Composure.UpperCaseName().Translate().GetText(), ' ', composureModifier.Value.WithSymbol());
            
            return Builder.Surround(LustRichText);
        }
    }
}