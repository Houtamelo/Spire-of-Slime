using System;
using System.Text;
using Core.Utils.Extensions;
using JetBrains.Annotations;
using UnityEngine;

namespace Core.Utils.Math
{
    public static class MathExtensions
    {
        private static readonly StringBuilder Builder = new();
        
        public static int Next([NotNull] this System.Random random, int max) => random.Next(max);
        
        [NotNull]
        public static string ToPercentageString(this float value)
        {
            Builder.Clear();
            Builder.Append((value * 100f).ToString("0.0"), '%');
            return Builder.ToString();
        }

        [NotNull]
        public static string ToPercentageStringBase100(this int value) => Builder.Override(value.ToString(), '%').ToString();
        
        //[NotNull]
        //public static string ToPercentageStringBase100(this int value) => Builder.Override(value.ToString(), '%').ToString();

        [NotNull]
        public static string ToPercentageWithSymbol(this float value)
        {
            Builder.Clear();
            if (value > 0)
                Builder.Append('+');
            
            Builder.Append((value * 100f).ToString("0.0"), '%');
            return Builder.ToString();
        }

        [NotNull]
        public static string ToPercentlessString(this float value, int digits, int decimalDigits)
        {
            Span<char> span = stackalloc char[digits + decimalDigits + 1];
            for (int i = 0; i < digits; i++)
                span[i] = '0';
            
            if (decimalDigits > 0)
            {
                span[digits] = '.';

                for (int i = 0; i < decimalDigits; i++)
                    span[digits + 1 + i] = '0';
            }

            return Builder.Override((value * 100f).ToString(format: span.ToString())).ToString();
        }
        
        [NotNull]
        public static string ToPercentlessStringWithSymbol(this float value, int digits, int decimalDigits)
        {
            Span<char> span = stackalloc char[digits + decimalDigits + 1];
            for (int i = 0; i < digits; i++)
                span[i] = '0';
            
            if (decimalDigits > 0)
            {
                span[digits] = '.';

                for (int i = 0; i < decimalDigits; i++)
                    span[digits + 1 + i] = '0';
            }

            Builder.Clear();
            if (value > 0)
                Builder.Append('+');
            
            return Builder.Append((value * 100f).ToString(format: span.ToString())).ToString();
        }

        public static float ConvertToDefaultInterval(this float angle)
        {
            float divisor = Mathf.Abs(angle / 360f);
            if (divisor > 1) 
                angle /= Mathf.FloorToInt(divisor) * 360;

            if (angle < 0)
                angle +=  360;
            
            return angle;
        }

        public static float ToSignedAngle(this float polarAngle) => polarAngle.ConvertToDefaultInterval() <= 180 ? polarAngle : polarAngle - 360;

        public static float GetSmallestClockwiseAngle(float start, float end) // polar
        {
            start = start.ConvertToDefaultInterval();
            end = end.ConvertToDefaultInterval();
            float distance = end - start;
            distance = distance switch
            {
                > 180 => end - 360 - start,
                < -180 => 360 - start + end,
                _ => distance
            };

            return distance;
        }

        [NotNull]
        public static string ToDamageRangeFormat(this (int lower, int upper) damage) => Builder.Override(damage.lower.ToString("0"), " - ", damage.upper.ToString("0")).ToString();
        
        [NotNull]
        public static string ToLustRangeFormat(this (int lower, int upper) lust) => Builder.Override(lust.lower.ToString("0"), " - ", lust.upper.ToString("0")).ToString();
        
        [NotNull]
        public static string ToHealRangeFormat(this (int lower, int upper) heal) => Builder.Override(heal.lower.ToString("0"), " - ", heal.upper.ToString("0")).ToString();

        [NotNull]
        public static string WithSymbolSingleCase(this float number)
        {
            return number switch
            {
                0f   => "0.0",
                > 0f => Builder.Override('+', number.ToString("0.0")).ToString(),
                _    => number.ToString("0.0")
            };
        }

        [NotNull]
        public static string WithSymbol(this int integer)
        {
            return integer switch
            {
                0    => "0",
                > 0  => Builder.Override('+', integer.ToString("0")).ToString(),
                _    => integer.ToString("0")
            };
        }

        public static float Clamp(this float input, float min, float max) => Mathf.Clamp(input, min, max);
        public static int Clamp(this int input, int min, int max) => Mathf.Clamp(input, min, max);
        public static uint Clamp(this uint input, uint min, uint max) => input < min         ? min : input > max ? max : input;
        public static double Clamp(this double input, double min, double max) => input < min ? min : input > max ? max : input;
        public static long Clamp(this long input, long min, long max) => input < min         ? min : input > max ? max : input;
        
        public static float Clamp01(this float input) => Mathf.Clamp01(input);
        public static double Clamp01(this double input) => input < 0 ? 0 : input > 1 ? 1 : input;
        
        public static int CeilToInt(this float input) => Mathf.CeilToInt(input);
        //public static int CeilToUInt(this float input) => Mathf.CeilToInt(input);
        public static int CeilToInt(this double input) => Mathf.CeilToInt((float) input);
        //public static int CeilToUInt(this double input) => Mathf.CeilToInt((float) input);
        
        public static int FloorToInt(this float input) => Mathf.FloorToInt(input);
        //public static int FloorToUInt(this float input) => Mathf.FloorToInt(input);
        public static int FloorToInt(this double input) => Mathf.FloorToInt((float) input);
        //public static int FloorToUInt(this double input) => Mathf.FloorToInt((float) input);
        
        public static double Lerp(this double a, double b, double t) => a + ((b - a) * t);
    }
}