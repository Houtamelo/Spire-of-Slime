using JetBrains.Annotations;
using UnityEngine;

namespace Core.Utils.Math
{
    using System;
    using System.Globalization;
    
    [System.Runtime.InteropServices.ComVisible(true), Serializable]
    // ReSharper disable once InconsistentNaming
    public struct TSpan : IComparable, IComparable<TSpan>, IEquatable<TSpan>, IFormattable
    {
        private const long TicksPerMillisecond = 10000;
        private const double MillisecondsPerTick = 1.0 / TicksPerMillisecond;

        public static long TicksToMilliseconds(long ticks) => (long)(ticks * MillisecondsPerTick);

        public static long MillisecondsToTicks(long milliseconds)
        {
            double temp = milliseconds * TicksPerMillisecond;
            if (temp is > long.MaxValue or < long.MinValue)
                throw new OverflowException("Overflow_TimeSpanTooLong");
            
            return (long)temp;
        }

        public const long TicksPerSecond = TicksPerMillisecond * 1000; // 10,000,000
        private const double SecondsPerTick = 1.0 / TicksPerSecond;     // 0.0001
        private const float FloatSecondsPerTick = (float)SecondsPerTick; // 0.0001f

        public static double TicksToSeconds(long ticks) => ticks * SecondsPerTick;

        public static long SecondsToTicks(double seconds)
        {
            double temp = seconds * TicksPerSecond;
            if (temp is > long.MaxValue or < long.MinValue)
                throw new OverflowException("Overflow_TimeSpanTooLong");
            
            return (long)temp;
        }

        private const double SecondsPerMillisecond = 1.0 / MillisPerSecond;
        private const int MillisPerSecond = 1000;
        
        public static double MillisecondsToSeconds(long milliseconds) => milliseconds * SecondsPerMillisecond;
        
        public static long SecondsToMilliseconds(double seconds)
        {
            double temp = seconds * MillisPerSecond;
            if (temp is > MaxMilliSeconds or < MinMilliSeconds)
                throw new OverflowException("Overflow_TimeSpanTooLong");
            
            return (long)temp;
        }

        public const long MaxSeconds = long.MaxValue / TicksPerSecond;
        public const long MinSeconds = long.MinValue / TicksPerSecond;

        public const long MaxMilliSeconds = long.MaxValue / TicksPerMillisecond;
        public const long MinMilliSeconds = long.MinValue / TicksPerMillisecond;

        public const long TicksPerTenthSecond = TicksPerMillisecond * 100;

        public static readonly TSpan Zero = new(ticks: 0);
        public static readonly TSpan OneSecond = new(ticks: TicksPerSecond);

        public static readonly TSpan MaxValue = new(long.MaxValue);
        public static readonly TSpan MinValue = new(long.MinValue);

        [SerializeField]
        private long ticks;
        
        [SerializeField]
        private TimeMode timeMode; // used by property drawer

        public TSpan(long ticks)
        {
            this.ticks = ticks;
            timeMode = TimeMode.Seconds;
        }
        
        public TSpan(long milliSeconds, bool _)
        {
            ticks = milliSeconds * TicksPerMillisecond;
            timeMode = TimeMode.Seconds;
        }

        public enum TimeMode
        {
            Seconds = 0, // leaving the default value as seconds
            Milliseconds = 1,
            Ticks = 2,
        }

        public long Ticks
        {
            get => ticks;
            set => ticks = value;
        }

        [System.Diagnostics.Contracts.Pure]
        public readonly long Milliseconds // Through this I hereby decide that the smallest unit of time is the millisecond
        {
            get
            {
                double temp = ticks * MillisecondsPerTick;
                if (temp > MaxMilliSeconds)
                    return MaxMilliSeconds;

                if (temp < MinMilliSeconds)
                    return MinMilliSeconds;

                return (long)temp;
            }
        }

        [System.Diagnostics.Contracts.Pure]
        public readonly double Seconds => ticks * SecondsPerTick;
        
        [System.Diagnostics.Contracts.Pure]
        public readonly float FloatSeconds => ticks * FloatSecondsPerTick;

        [System.Diagnostics.Contracts.Pure]
        public readonly TSpan GetAddition(TSpan ts)
        {
            long result = ticks + ts.ticks;
            // Overflow if signs of operands was identical and result's
            // sign was opposite.
            // >> 63 gives the sign bit (either 64 1's or 64 0's).
            if ((ticks >> 63 == ts.ticks >> 63) && (ticks >> 63 != result >> 63))
                throw new OverflowException("Overflow_TimeSpanTooLong");
            
            return new TSpan(result);
        }

        [System.Diagnostics.Contracts.Pure]
        public readonly TSpan GetMultiplication(double factor)
        {
            double result = ticks * factor;
            if (result is > long.MaxValue or < long.MinValue)
                throw new OverflowException("Overflow_TimeSpanTooLong");
            
            return new TSpan(ticks: (long)result);
        }

        [System.Diagnostics.Contracts.Pure]
        public static int Compare(TSpan t1, TSpan t2)
        {
            if (t1.ticks > t2.ticks)
                return 1;
            
            if (t1.ticks < t2.ticks)
                return -1;
            
            return 0;
        }

        [System.Diagnostics.Contracts.Pure]
        // Returns a value less than zero if this  object
        public readonly int CompareTo([CanBeNull] object value)
        {
            if (value == null)
                return 1;
            
            if (value is not TSpan serialSpan)
                throw new ArgumentException("Arg_MustBeTimeSpan");
            
            long otherTicks = serialSpan.ticks;
            if (ticks > otherTicks)
                return 1;
            
            if (ticks < otherTicks)
                return -1;
            
            return 0;
        }
        
        [System.Diagnostics.Contracts.Pure]
        public readonly int CompareTo(TSpan value)
        {
            long t = value.ticks;
            if (ticks > t)
                return 1;
            if (ticks < t)
                return -1;
            return 0;
        }

        [System.Diagnostics.Contracts.Pure]
        public readonly TSpan Duration()
        {
            if (ticks == MinValue.Ticks)
                throw new OverflowException("Overflow_Duration");
            
            return new TSpan(ticks >= 0 ? ticks : -ticks);
        }

        public readonly override bool Equals(object value)
        {
            if (value is TSpan span)
                return ticks == span.ticks;

            return false;
        }

        public readonly bool Equals(TSpan obj) => ticks == obj.ticks;

        public static bool Equals(TSpan t1, TSpan t2) => t1.ticks == t2.ticks;

        public readonly override int GetHashCode() => (int)ticks ^ (int)(ticks >> 32);

        [System.Diagnostics.Contracts.Pure]
        public static TSpan Interval(double value, int scale)
        {
            if (double.IsNaN(value))
                throw new ArgumentException("Arg_CannotBeNaN");
            
            double tmp = value * scale;
            double millis = tmp + (value >= 0 ? 0.5 : -0.5);
            if (millis is > long.MaxValue / TicksPerMillisecond or < long.MinValue / TicksPerMillisecond)
                throw new OverflowException("Overflow_TimeSpanTooLong");
            
            return new TSpan((long)millis * TicksPerMillisecond);
        }

        [System.Diagnostics.Contracts.Pure]
        public readonly TSpan Negate()
        {
            if (ticks == MinValue.Ticks)
                throw new OverflowException("Overflow_NegateTwosCompNum");
            
            return new TSpan(-ticks);
        }

        [System.Diagnostics.Contracts.Pure]
        public readonly TSpan GetSubtraction(TSpan ts)
        {
            long result = ticks - ts.ticks;
            // Overflow if signs of operands was different and result's sign was opposite from the first argument's sign. >> 63 gives the sign bit (either 64 1's or 64 0's).
            if ((ticks >> 63 != ts.ticks >> 63) && (ticks >> 63 != result >> 63))
                throw new OverflowException("Overflow_TimeSpanTooLong");
            
            return new TSpan(result);
        }

        [System.Diagnostics.Contracts.Pure]
        public static TSpan FromMilliseconds(double value) => Interval(value, 1);

        [System.Diagnostics.Contracts.Pure]
        public static TSpan FromSeconds(double value) => Interval(value, MillisPerSecond);

        public void AddSeconds(int seconds)
        {
            if ((seconds + Seconds) is > MaxSeconds or < MinSeconds)
                throw new ArgumentOutOfRangeException(nameof(seconds), "Overflow_TimeSpanTooLong");
            
            ticks += seconds * TicksPerSecond;
        }

        public void SubtractTicks(long ticksToSubtract) => ticks -= ticksToSubtract;

        public void SubtractMilliseconds(long milliseconds) => ticks -= MillisecondsToTicks(milliseconds);

        public void SubtractSeconds(double seconds) => ticks -= SecondsToTicks(seconds);

        public void Multiply(double factor)
        {
            double result = ticks * factor;
            if (result > long.MaxValue || result < long.MinValue)
                throw new OverflowException("Overflow_TimeSpanTooLong");
            
            ticks = (long)result;
        }

        public void Divide(double factor)
        {
            if (factor == 0.0)
                throw new DivideByZeroException("DivideByZero_TimeSpan");
            
            double result = ticks / factor;
            if (result is > long.MaxValue or < long.MinValue)
                throw new OverflowException("Overflow_TimeSpanTooLong");
            
            ticks = (long)result;
        }

        [System.Diagnostics.Contracts.Pure]
        public static TSpan FromTicks(long value) => new(ticks: value);

        [System.Diagnostics.Contracts.Pure]
        public static long TimeToTicks(int hour, int minute, int second)
        {
            // totalSeconds is bounded by 2^31 * 2^12 + 2^31 * 2^8 + 2^31,
            // which is less than 2^44, meaning we won't overflow totalSeconds.
            long totalSeconds = ((long)hour * 3600) + ((long)minute * 60) + second;
            if (totalSeconds is > MaxSeconds or < MinSeconds)
                throw new ArgumentOutOfRangeException(null, "Overflow_TimeSpanTooLong");
            
            return totalSeconds * TicksPerSecond;
        }

        public static explicit operator TimeSpan(TSpan value) => new(value.ticks);
        public static explicit operator TSpan(TimeSpan value) => new(value.Ticks);

#region ParseAndFormat

        public static TSpan Parse([NotNull] string s) => (TSpan)TimeSpan.Parse(s);
        public static TSpan Parse(string input, IFormatProvider formatProvider) => (TSpan)TimeSpan.Parse(input, formatProvider);
        public static TSpan ParseExact(string input, string format, IFormatProvider formatProvider) => (TSpan)TimeSpan.ParseExact(input,                           format,  formatProvider, TimeSpanStyles.None);
        public static TSpan ParseExact(string input, string[] formats, IFormatProvider formatProvider) => (TSpan)TimeSpan.ParseExact(input,                        formats, formatProvider, TimeSpanStyles.None);
        public static TSpan ParseExact(string input, string format, IFormatProvider formatProvider, TimeSpanStyles styles) => (TSpan)TimeSpan.ParseExact(input,    format,  formatProvider, styles);
        public static TSpan ParseExact(string input, string[] formats, IFormatProvider formatProvider, TimeSpanStyles styles) => (TSpan)TimeSpan.ParseExact(input, formats, formatProvider, styles);

        public static bool TryParse(string s, out TSpan result)
        {
            if (TimeSpan.TryParse(s, out TimeSpan ts))
            {
                result = (TSpan)ts;
                return true;
            }
            
            result = default;
            return false;
        }
        
        public static bool TryParse(string input, IFormatProvider formatProvider, out TSpan result)
        {
            if (TimeSpan.TryParse(input, formatProvider, out TimeSpan ts))
            {
                result = (TSpan)ts;
                return true;
            }
            
            result = default;
            return false;
        }

        public static bool TryParseExact(string input, string format, IFormatProvider formatProvider, out TSpan result)
        {
            if (TimeSpan.TryParseExact(input, format, formatProvider, TimeSpanStyles.None, out TimeSpan ts))
            {
                result = (TSpan)ts;
                return true;
            }
            
            result = default;
            return false;
        }

        public static bool TryParseExact(string input, string[] formats, IFormatProvider formatProvider, out TSpan result)
        {
            if (TimeSpan.TryParseExact(input, formats, formatProvider, TimeSpanStyles.None, out TimeSpan ts))
            {
                result = (TSpan)ts;
                return true;
            }
            
            result = default;
            return false;
        }

        public static bool TryParseExact(string input, string format, IFormatProvider formatProvider, TimeSpanStyles styles, out TSpan result)
        {
            if (TimeSpan.TryParseExact(input, format, formatProvider, styles, out TimeSpan ts))
            {
                result = (TSpan)ts;
                return true;
            }
            
            result = default;
            return false;
        }

        public static bool TryParseExact(string input, string[] formats, IFormatProvider formatProvider, TimeSpanStyles styles, out TSpan result)
        {
            if (TimeSpan.TryParseExact(input, formats, formatProvider, styles, out TimeSpan ts))
            {
                result = (TSpan)ts;
                return true;
            }
            
            result = default;
            return false;
        }

        public override string ToString() => ((TimeSpan)this).ToString();

        [NotNull]
        public string ToString(string format) => ((TimeSpan)this).ToString(format);

        public string ToString(string format, IFormatProvider formatProvider) => ((TimeSpan)this).ToString(format, formatProvider);

#endregion

        [System.Diagnostics.Contracts.Pure]
        public static TSpan ChoseMax(TSpan a, TSpan b) => a > b ? a : b;

        [System.Diagnostics.Contracts.Pure]
        public static TSpan ChoseMin(TSpan a, TSpan b) => a < b ? a : b;

        [System.Diagnostics.Contracts.Pure]
        public readonly TSpan Clamp(TSpan min, TSpan max)
        {
            long desiredTicks = ticks.Clamp(min.ticks, max.ticks);
            return new TSpan(ticks: desiredTicks);
        }

        public static TSpan operator -(TSpan t)
        {
            if (t.ticks == MinValue.ticks)
                throw new OverflowException("Overflow_NegateTwosCompNum");
            
            return new TSpan(-t.ticks);
        }

        public static TSpan operator -(TSpan t1, TSpan t2) => t1.GetSubtraction(t2);

        public static TSpan operator +(TSpan t) => t;

        public static TSpan operator +(TSpan t1, TSpan t2) => t1.GetAddition(t2);

        public static bool operator ==(TSpan t1, TSpan t2) => t1.ticks == t2.ticks;

        public static bool operator !=(TSpan t1, TSpan t2) => t1.ticks != t2.ticks;

        public static bool operator <(TSpan t1, TSpan t2) => t1.ticks < t2.ticks;

        public static bool operator <=(TSpan t1, TSpan t2) => t1.ticks <= t2.ticks;

        public static bool operator >(TSpan t1, TSpan t2) => t1.ticks > t2.ticks;

        public static bool operator >=(TSpan t1, TSpan t2) => t1.ticks >= t2.ticks;
    }
}