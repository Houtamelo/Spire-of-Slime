using System;
using Core.Combat.Scripts.Effects;
using Core.Combat.Scripts.Enums;
using Color = UnityEngine.Color;

namespace Core.Combat.Scripts
{
    public static class ColorReferences
    {
        public static (string start, string end) BuffedRichText => BuffRichText;
        public static (string start, string end) DebuffedRichText => DebuffRichText;

        public static readonly Color Lust = FromHex("#FE1BB3");
        public static readonly (string start, string end) LustRichText = ("<color=#FE1BB3>", "</color>");

        public static Color Composure => Lust;
        public static (string start, string end) ComposureRichText => LustRichText;

        public static readonly Color Temptation = FromHex("#9d5cc9");
        public static readonly (string start, string end) TemptationRichText = ("<color=#9d5cc9>", "</color>");

        public static Color Arousal => Lust;
        public static (string start, string end) ArousalRichText => LustRichText;
        public static Color ArousalApplyChance => Arousal;
        public static (string start, string end) ArousalApplyChanceRichText => ArousalRichText;

        public static readonly Color Buff = FromHex("#4c9cff");
        public static readonly (string start, string end) BuffRichText = ("<color=#4c9cff>", "</color>");

        public static readonly Color Debuff = FromHex("#e29b43");
        public static readonly (string start, string end) DebuffRichText = ("<color=#e29b43>", "</color>");
        public static Color DebuffResistance => Debuff;
        public static (string start, string end) DebuffResistanceRichText => DebuffRichText;
        public static Color DebuffApplyChance => Debuff;
        public static (string start, string end) DebuffApplyChanceRichText => DebuffRichText;

        public static readonly Color Stun = FromHex("#fcd74a");
        public static readonly (string start, string end) StunRichText = ("<color=#fcd74a>", "</color>");
        
        public static Color StunMitigation => Stun;
        public static (string start, string end) StunMitigationRichText => StunRichText;
        
        public static readonly Color Guarded = FromHex("#5ca1eb");
        public static readonly (string start, string end) GuardedRichText = ("<color=#5ca1eb>", "</color>");
        
        public static readonly Color Marked = FromHex("#8a4343");
        public static readonly (string start, string end) MarkedRichText = ("<color=#8a4343>", "</color>");
        
        public static readonly Color Stamina = FromHex("#50db5e");
        public static readonly (string start, string end) StaminaRichText = ("<color=#50db5e>", "</color>");
        
        public static Color Heal => Stamina;
        public static (string start, string end) HealRichText => StaminaRichText;
        
        public static Color OvertimeHeal => Stamina;
        public static (string start, string end) OvertimeHealRichText => StaminaRichText;
        
        public static Color Resilience => Stamina;
        public static (string start, string end) ResilienceRichText => StaminaRichText;
        
        public static readonly Color Poison = FromHex("#b8f149");
        public static readonly (string start, string end) PoisonRichText = ("<color=#b8f149>", "</color>");
        public static Color PoisonResistance => Poison;
        public static (string start, string end) PoisonResistanceRichText => PoisonRichText;
        public static Color PoisonApplyChance => Poison;
        public static (string start, string end) PoisonApplyChanceRichText => PoisonRichText;

        public static readonly Color Riposte = FromHex("#e42626");
        public static readonly (string start, string end) RiposteRichText = ("<color=#e42626>", "</color>");

        public static readonly Color Move = FromHex("#6fdbdb");
        public static readonly (string start, string end) MoveRichText = ("<color=#6fdbdb>", "</color>");
        public static Color MoveResistance => Move;
        public static (string start, string end) MoveResistanceRichText => MoveRichText;
        public static Color MoveApplyChance => Move;
        public static (string start, string end) MoveApplyChanceRichText => MoveRichText;

        public static readonly Color Speed = FromHex("#64e7e2");
        public static readonly (string start, string end) SpeedRichText = ("<color=#64e7e2>", "</color>");
        
        public static readonly Color CriticalChance = FromHex("#ff6f3f");
        public static readonly (string start, string end) CriticalChanceRichText = ("<color=#ff6f3f>", "</color>");
        
        public static readonly Color Accuracy = FromHex("#fb7932");
        public static readonly (string start, string end) AccuracyRichText = ("<color=#fb7932>", "</color>");
        
        public static readonly Color Damage = FromHex("#f03d3d");
        public static readonly (string start, string end) DamageRichText = ("<color=#f03d3d>", "</color>");
        
        public static readonly Color Dodge = FromHex("#64dfe7");
        public static readonly (string start, string end) DodgeRichText = ("<color=#64dfe7>", "</color>");
        
        public static readonly Color Recovery = FromHex("#8bdae1");
        public static readonly (string start, string end) RecoveryRichText = ("<color=#8bdae1>", "</color>");
        
        public static readonly Color Charge = FromHex("#88deb1");
        public static readonly (string start, string end) ChargeRichText = ("<color=#88deb1>", "</color>");

        public static readonly Color Name = new(1f, 0.2216981f, 1f, 1f);

        public static Color HighLust => Lust;
        public static readonly Color LowLust = FromHex("#59093F");

        public static Color KnockedDown = FromHex("#FF003A");
        public static readonly (string start, string end) KnockedDownRichText = ("<color=#FF003A>", "</color>");
        
        public static Color GetColor(this EffectType effectType)
        {
            return effectType switch
            {
                EffectType.Lust           => Lust,
                EffectType.Buff           => Buff,
                EffectType.Debuff         => Debuff,
                EffectType.Stun           => Stun,
                EffectType.Guarded        => Guarded,
                EffectType.Marked         => Marked,
                EffectType.Heal           => Heal,
                EffectType.OvertimeHeal   => OvertimeHeal,
                EffectType.Poison         => Poison,
                EffectType.Riposte        => Riposte,
                EffectType.Move           => Move,
                EffectType.Arousal        => Arousal,
                EffectType.LustGrappled   => HighLust,
                EffectType.Perk           => Buff,
                EffectType.HiddenPerk     => Buff,
                EffectType.NemaExhaustion => Lust,
                EffectType.Mist           => Lust,
                EffectType.Summon         => Buff,
                EffectType.Temptation     => Temptation,
                _                         => throw new ArgumentOutOfRangeException(nameof(effectType), effectType, null)
            };
        }

        public static Color GetColor(this CombatStat stat)
        {
            return stat switch
            {
                CombatStat.DebuffResistance   => DebuffResistance,
                CombatStat.PoisonResistance   => PoisonResistance,
                CombatStat.MoveResistance     => MoveResistance,
                CombatStat.Accuracy           => Accuracy,
                CombatStat.CriticalChance     => CriticalChance,
                CombatStat.Dodge              => Dodge,
                CombatStat.Resilience         => Resilience,
                CombatStat.Composure          => Composure,
                CombatStat.StunMitigation     => StunMitigation,
                CombatStat.DamageMultiplier   => Damage,
                CombatStat.Speed              => Speed,
                CombatStat.DebuffApplyChance  => DebuffApplyChance,
                CombatStat.PoisonApplyChance  => PoisonApplyChance,
                CombatStat.MoveApplyChance    => MoveApplyChance,
                CombatStat.ArousalApplyChance => ArousalApplyChance,
                _                             => throw new ArgumentOutOfRangeException(nameof(stat), stat, message: null)
            };
        }

        public static Color GetColor(this OtherStats stat)
        {
            return stat switch
            {
                OtherStats.Stamina        => Stamina,
                OtherStats.Accuracy       => Accuracy,
                OtherStats.CriticalChance => CriticalChance,
                OtherStats.Dodge          => Dodge,
                OtherStats.Charge         => Charge,
                OtherStats.Recovery       => Recovery,
                OtherStats.Damage         => Damage,
                OtherStats.Name           => Name,
                OtherStats.LowLust        => LowLust,
                OtherStats.HighLust       => HighLust,
                _                         => throw new ArgumentOutOfRangeException(nameof(stat), stat, null)
            };
        }
        
        private static Color FromHex(string hex)
        {
            System.Drawing.Color color = CustomColorTranslator.FromHtml(hex);
            int r = Convert.ToInt16(color.R);
            int g = Convert.ToInt16(color.G);
            int b = Convert.ToInt16(color.B);
            return new Color(r / 255f, g / 255f, b / 255f);
        }
    }
}