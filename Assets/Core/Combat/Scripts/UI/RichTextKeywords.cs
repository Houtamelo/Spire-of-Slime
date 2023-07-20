using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
// ReSharper disable ConditionalTernaryEqualBranch

// ReSharper disable StringLiteralTypo

namespace Core.Combat.Scripts.UI
{
    public static class RichTextKeywords
    {
        public static readonly IReadOnlyDictionary<string, (Color, string)> Keywords;

        static RichTextKeywords() =>
            Keywords = new Dictionary<string, (Color, string)>
            {
                [" damage "] = (ColorReferences.Damage, ColorUtility.ToHtmlStringRGBA(ColorReferences.Damage)),
                ["warning"] = (ColorReferences.Damage, ColorUtility.ToHtmlStringRGBA(ColorReferences.Damage)),
                [" stamina "] = (ColorReferences.Stamina, ColorUtility.ToHtmlStringRGBA(ColorReferences.Stamina)),
                [" heal "] = (ColorReferences.Heal, ColorUtility.ToHtmlStringRGBA(ColorReferences.Heal)),
                [" heals "] = (ColorReferences.Heal, ColorUtility.ToHtmlStringRGBA(ColorReferences.Heal)),
                [" speed "] = (ColorReferences.Speed, ColorUtility.ToHtmlStringRGBA(ColorReferences.Speed)),
                [" critical "] = (ColorReferences.CriticalChance, ColorUtility.ToHtmlStringRGBA(ColorReferences.CriticalChance)),
                [" crit "] = (ColorReferences.CriticalChance, ColorUtility.ToHtmlStringRGBA(ColorReferences.CriticalChance)),
                [" crits "] = (ColorReferences.CriticalChance, ColorUtility.ToHtmlStringRGBA(ColorReferences.CriticalChance)),
                [" stack "] = (ColorUtility.TryParseHtmlString("CD8DFF", out Color color) ? color : color, "CD8DFF"),
                [" stacks "] = (color, "CD8DFF"),
                [" accuracy "] = (ColorReferences.Accuracy, ColorUtility.ToHtmlStringRGBA(ColorReferences.Accuracy)),
                [" buff "] = (ColorReferences.Buff, ColorUtility.ToHtmlStringRGBA(ColorReferences.Buff)),
                [" buffs "] = (ColorReferences.Buff, ColorUtility.ToHtmlStringRGBA(ColorReferences.Buff)),
                [" buffed "] = (ColorReferences.Buff, ColorUtility.ToHtmlStringRGBA(ColorReferences.Buff)),
                [" charge "] = (ColorReferences.Charge, ColorUtility.ToHtmlStringRGBA(ColorReferences.Charge)),
                [" debuff resistance "] = (ColorReferences.DebuffResistance, ColorUtility.ToHtmlStringRGBA(ColorReferences.DebuffResistance)),
                [" debuff "] = (ColorReferences.Debuff, ColorUtility.ToHtmlStringRGBA(ColorReferences.Debuff)),
                [" debuffs "] = (ColorReferences.Debuff, ColorUtility.ToHtmlStringRGBA(ColorReferences.Debuff)),
                [" debuffed "] = (ColorReferences.Debuff, ColorUtility.ToHtmlStringRGBA(ColorReferences.Debuff)),
                [" dodge "] = (ColorReferences.Dodge, ColorUtility.ToHtmlStringRGBA(ColorReferences.Dodge)),
                [" dodged "] = (ColorReferences.Dodge, ColorUtility.ToHtmlStringRGBA(ColorReferences.Dodge)),
                [" guard "] = (ColorReferences.Guarded, ColorUtility.ToHtmlStringRGBA(ColorReferences.Guarded)),
                [" guarded "] = (ColorReferences.Guarded, ColorUtility.ToHtmlStringRGBA(ColorReferences.Guarded)),
                [" lust resistance "] = (ColorReferences.Composure, ColorUtility.ToHtmlStringRGBA(ColorReferences.Composure)),
                [" lust "] = (ColorReferences.Lust, ColorUtility.ToHtmlStringRGBA(ColorReferences.Lust)),
                [" mark "] = (ColorReferences.Marked, ColorUtility.ToHtmlStringRGBA(ColorReferences.Marked)),
                [" marked "] = (ColorReferences.Marked, ColorUtility.ToHtmlStringRGBA(ColorReferences.Marked)),
                [" move resistance "] = (ColorReferences.MoveResistance, ColorUtility.ToHtmlStringRGBA(ColorReferences.MoveResistance)),
                [" move "] = (ColorReferences.Move, ColorUtility.ToHtmlStringRGBA(ColorReferences.Move)),
                [" poison resistance "] = (ColorReferences.PoisonResistance, ColorUtility.ToHtmlStringRGBA(ColorReferences.PoisonResistance)),
                [" poison "] = (ColorReferences.Poison, ColorUtility.ToHtmlStringRGBA(ColorReferences.Poison)),
                [" poisoned "] = (ColorReferences.Poison, ColorUtility.ToHtmlStringRGBA(ColorReferences.Poison)),
                [" recovery "] = (ColorReferences.Recovery, ColorUtility.ToHtmlStringRGBA(ColorReferences.Recovery)),
                [" recovery speed "] = (ColorReferences.Recovery, ColorUtility.ToHtmlStringRGBA(ColorReferences.Recovery)),
                [" resilience "] = (ColorReferences.Resilience, ColorUtility.ToHtmlStringRGBA(ColorReferences.Resilience)),
                [" riposte "] = (ColorReferences.Riposte, ColorUtility.ToHtmlStringRGBA(ColorReferences.Riposte)),
                [" stun recovery speed "] = (ColorReferences.StunMitigation, ColorUtility.ToHtmlStringRGBA(ColorReferences.StunMitigation)),
                [" stun recovery "] = (ColorReferences.StunMitigation, ColorUtility.ToHtmlStringRGBA(ColorReferences.StunMitigation)),
                [" stun speed "] = (ColorReferences.StunMitigation, ColorUtility.ToHtmlStringRGBA(ColorReferences.StunMitigation)),
                [" stun "] = (ColorReferences.Stun, ColorUtility.ToHtmlStringRGBA(ColorReferences.Stun)),
                [" stunned "] = (ColorReferences.Stun, ColorUtility.ToHtmlStringRGBA(ColorReferences.Stun)),
                [" stuns "] = (ColorReferences.Stun, ColorUtility.ToHtmlStringRGBA(ColorReferences.Stun)),
                //[" temptation resistance "] = (ColorReferences.TemptationResistance, ColorUtility.ToHtmlStringRGBA(ColorReferences.TemptationResistance)),
                //[" temptation "] = (ColorReferences.Temptation, ColorUtility.ToHtmlStringRGBA(ColorReferences.Temptation)),
            };

#if UNITY_EDITOR
        /// <summary> Forced to use only in editor because it's slow, O(n^2) complexity </summary>
        public static string InsertRichText([NotNull] string input)
        {
            string[] words = input.Split(' ');
            foreach ((string keyword, (_, string hex)) in Keywords)
            {
                for (int i = 0; i < words.Length; i++)
                {
                    string word = words[i];
                    if (word.Equals(keyword, StringComparison.InvariantCultureIgnoreCase)) 
                        words[i] = $"<color=#{hex}>{word}</color>";
                }
            }

            return string.Join(' ', words);
        }
#endif
    }
}