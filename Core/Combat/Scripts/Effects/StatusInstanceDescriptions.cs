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
using Save_Management;
using UnityEngine;
using Utils.Extensions;
using Utils.Math;
using Utils.Patterns;
using static Core.Combat.Scripts.ColorReferences;
using Save = Save_Management.Save;

namespace Core.Combat.Scripts.Effects
{
    public static class StatusInstanceDescriptions
    {
        private static readonly StringBuilder Builder = new();
        
        public static Option<string> Get(StatusInstance instance)
        {
            StringBuilder builder = instance switch
            {
                Arousal a      => Builder.Override("Lust ", a.LustPerTime.ToString("0"), "pt, ", a.GetCompactDurationString()).Surround(ArousalRichText),
                BuffOrDebuff b => Builder.Override(b.Delta.ToPercentageWithSymbol(), "% ",b.Attribute.LowerCaseName(), ", ", b.GetCompactDurationString()).Surround(b.IsPositive ? BuffRichText : DebuffRichText),
                LustGrappled   => Builder.Override("<i>Having a good <b>time</b></i>").Surround(LustRichText),
                Guarded g      => Builder.Override("Guarded by ", g.Caster.Script.CharacterName, ", ", g.GetCompactDurationString()).Surround(GuardedRichText),
                Marked m       => Builder.Override("Marked, ", m.GetCompactDurationString()).Surround(MarkedRichText),
                MistStatus     => Builder.Clear(),
                NemaExhaustion => NemaExhaustionDescription(),
                OvertimeHeal o => Builder.Override("Heal ", o.HealPerTime.ToString("0"), "pt, ", o.GetCompactDurationString()).Surround(OvertimeHealRichText),
                PerkStatus per => per.IsHidden ? Builder.Override("Hidden from curious eyes") : Builder.Clear(),
                Poison poi     => Builder.Override("Poison ", poi.DamagePerTime.ToString("0"), "pt, ", poi.GetCompactDurationString()).Surround(PoisonRichText),
                Riposte r      => RiposteDescription(r),
                _              => LogType(instance)
            };
            
            return builder.Length > 0 ? Option<string>.Some(builder.ToString()) : Option.None;
        }
        
        private static StringBuilder RiposteDescription(Riposte r)
        {
            (float lower, float upper) damage = r.Owner.StatsModule.GetDamageWithMultiplier();
            damage.lower *= r.Power;
            damage.upper *= r.Power;
            uint actualLower = damage.lower.CeilToUInt();
            uint actualUpper = damage.upper.CeilToUInt();
            return Builder.Override("Riposte ", (actualLower, actualUpper).ToDamageFormat(), ", ", r.GetCompactDurationString()).Surround(RiposteRichText);
        }
        
        private static StringBuilder LogType(this StatusInstance instance)
        {
            Debug.LogWarning($"Missing description for {instance.GetType()}");
            return Builder.Clear();
        }
        
        private static StringBuilder NemaExhaustionDescription()
        {
            Builder.Clear();
            Save save = Save.Current;
            if (save == null)
                return Builder;

            ExhaustionEnum exhaustion = save.NemaExhaustionAsEnum;
            string title = exhaustion switch
            {
                ExhaustionEnum.None   => "Nema is energetic, no modifiers",
                ExhaustionEnum.Low    => "Nema is a little tired, penalties: \n",
                ExhaustionEnum.Medium => "Nema is exhausted, penalties: \n",
                ExhaustionEnum.High   => "Nema is extremely exhausted, penalties: \n",
                _                     => throw new System.ArgumentOutOfRangeException($"Unknown ExhaustionEnum: {exhaustion}")
            };
            
            Builder.Append(title);

            Option<float> speedModifier = NemaExhaustion.GetSpeedModifier(exhaustion);
            if (speedModifier.IsSome)
                Builder.AppendLine("Speed ", speedModifier.Value.ToPercentageString());
            
            Option<float> dodgeModifier = NemaExhaustion.GetDodgeModifier(exhaustion);
            if (dodgeModifier.IsSome)
                Builder.AppendLine("Dodge ", dodgeModifier.Value.ToPercentageString());
            
            Option<float> resistancesModifier = NemaExhaustion.GetResistancesModifier(exhaustion);
            if (resistancesModifier.IsSome)
            {
                Builder.AppendLine("Debuff Res ", resistancesModifier.Value.ToPercentageString());
                Builder.AppendLine("Move Res ", resistancesModifier.Value.ToPercentageString());
                Builder.AppendLine("Poison Res ", resistancesModifier.Value.ToPercentageString());
            }
            
            Option<float> stunRecoverySpeedModifier = NemaExhaustion.GetStunRecoverySpeedModifier(exhaustion);
            if (stunRecoverySpeedModifier.IsSome)
                Builder.AppendLine("Stun Recovery ", stunRecoverySpeedModifier.Value.ToPercentageString());
            
            return Builder.Surround(LustRichText);
        }
    }
}