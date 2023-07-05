using System;
using System.Text;
using Core.Utils.Extensions;
using UnityEngine;

namespace Core.Utils.Math
{
    public static class MathExtensions
    {
        private static readonly StringBuilder Builder = new();
        
        public static string ToPercentageString(this float value)
        {
            Builder.Clear();
            Builder.Append((value * 100f).ToString("0.0"), '%');
            return Builder.ToString();
        }

        public static string ToPercentageWithSymbol(this float value)
        {
            Builder.Clear();
            if (value > 0)
                Builder.Append('+');
            
            Builder.Append((value * 100f).ToString("0.0"), '%');
            return Builder.ToString();
        }

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

        public static string ToDamageFormat(this (uint lower, uint upper) damage) => Builder.Override(damage.lower.ToString("0"), " - ", damage.upper.ToString("0")).ToString();

        public static string WithSymbolSingleCase(this float number)
        {
            return number switch
            {
                0f   => "0.0",
                > 0f => Builder.Override('+', number.ToString("0.0")).ToString(),
                _    => number.ToString("0.0")
            };
        }

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
        public static uint Clamp(this uint input, uint min, uint max) => input < min ? min : input > max ? max : input;
        
        public static int CeilToInt(this float input) => Mathf.CeilToInt(input);
        public static uint CeilToUInt(this float input) => (uint) Mathf.CeilToInt(input);
        
        public static int FloorToInt(this float input) => Mathf.FloorToInt(input);
        public static uint FloorToUInt(this float input) => (uint) Mathf.FloorToInt(input);
    }
}