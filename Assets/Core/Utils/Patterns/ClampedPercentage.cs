using System.Runtime.Serialization;
using Core.Utils.Math;
using UnityEngine;

namespace Core.Utils.Patterns
{
    [DataContract, System.Serializable]
    public struct ClampedPercentage
    {
        [DataMember, SerializeField]
        public float value;

        public ClampedPercentage(float value)
        {
            this.value = Mathf.Clamp(value, 0f, 1f);
        }
        
        public static implicit operator float(ClampedPercentage percentage) => percentage.value;
        public static implicit operator ClampedPercentage(float value) => new(value);
        
        public static ClampedPercentage operator +(ClampedPercentage a, ClampedPercentage b) => new(a.value + b.value);
        public static ClampedPercentage operator -(ClampedPercentage a, ClampedPercentage b) => new(a.value - b.value);
        public static ClampedPercentage operator *(ClampedPercentage a, ClampedPercentage b) => new(a.value * b.value);
        public static ClampedPercentage operator /(ClampedPercentage a, ClampedPercentage b) => new(a.value / b.value);
        
        public static ClampedPercentage operator +(ClampedPercentage a, float b) => new(a.value + b);
        public static ClampedPercentage operator -(ClampedPercentage a, float b) => new(a.value - b);
        public static ClampedPercentage operator *(ClampedPercentage a, float b) => new(a.value * b);
        public static ClampedPercentage operator /(ClampedPercentage a, float b) => new(a.value / b);
        
        public static ClampedPercentage operator +(float a, ClampedPercentage b) => new(a + b.value);
        public static ClampedPercentage operator -(float a, ClampedPercentage b) => new(a - b.value);
        public static ClampedPercentage operator *(float a, ClampedPercentage b) => new(a * b.value);
        public static ClampedPercentage operator /(float a, ClampedPercentage b) => new(a / b.value);
        
        public static bool operator ==(ClampedPercentage a, ClampedPercentage b) => a.value == b.value;
        public static bool operator !=(ClampedPercentage a, ClampedPercentage b) => a.value != b.value;
        public static bool operator >(ClampedPercentage a, ClampedPercentage b) => a.value > b.value;
        public static bool operator <(ClampedPercentage a, ClampedPercentage b) => a.value < b.value;
        public static bool operator >=(ClampedPercentage a, ClampedPercentage b) => a.value >= b.value;
        public static bool operator <=(ClampedPercentage a, ClampedPercentage b) => a.value <= b.value;
        
        public static bool operator ==(ClampedPercentage a, float b) => a.value == b;
        public static bool operator !=(ClampedPercentage a, float b) => a.value != b;
        public static bool operator >(ClampedPercentage a, float b) => a.value > b;
        public static bool operator <(ClampedPercentage a, float b) => a.value < b;
        public static bool operator >=(ClampedPercentage a, float b) => a.value >= b;
        public static bool operator <=(ClampedPercentage a, float b) => a.value <= b;
        
        public static bool operator ==(float a, ClampedPercentage b) => a == b.value;
        public static bool operator !=(float a, ClampedPercentage b) => a != b.value;
        public static bool operator >(float a, ClampedPercentage b) => a > b.value;
        public static bool operator <(float a, ClampedPercentage b) => a < b.value;
        public static bool operator >=(float a, ClampedPercentage b) => a >= b.value;
        public static bool operator <=(float a, ClampedPercentage b) => a <= b.value;
        
        public override bool Equals(object obj) => obj is ClampedPercentage percentage && value == percentage.value;
        public override int GetHashCode() => value.GetHashCode();
        public override string ToString() => value.ToPercentageString();
    }
}