using System;
using System.Runtime.Serialization;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Save_Management.SaveObjects
{
    /// <summary>  Only letters, digits and underscore '_' are allowed. </summary>
    [Serializable, DataContract, InlineProperty]
    public struct CleanString : IEquatable<string>, IComparable, IComparable<string>, IComparable<CleanString>, IEquatable<CleanString>
    {
        [DataMember, SerializeField, HideLabel] private string value;
        
        [NonSerialized] private bool _sanitized;

        public CleanString(string value)
        {
            this.value = Sanitize(value);
            _sanitized = true;
        }

        private static string Sanitize(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;
            
            int j = 0;
            Span<char> output = stackalloc char[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (char.IsLetterOrDigit(c))
                {
                    output[j] = char.ToLowerInvariant(c);
                    j++;
                }
                else if (c == '_')
                {
                    output[j] = '_';
                    j++;
                }
            }
            
            return output[..j].ToString();
        }

        public override string ToString()
        {
            CheckSanitization(ref this);
            return value;
        }

        public bool Equals(CleanString other)
        {
            CheckSanitization(ref other);
            CheckSanitization(ref this);
            return value == other.value;
        }

        private static void CheckSanitization(ref CleanString input)
        {
            if (input._sanitized == false)
            {
                input.value = Sanitize(input.value);
                input._sanitized = true;
            }
        }

        public bool Equals(string other)
        {
            bool isOtherNull = other == null;
            bool isSelfNull = value == null;
            if (isOtherNull && isSelfNull)
                return true;

            if (isOtherNull || isSelfNull)
                return false;
            
            CheckSanitization(ref this);

            ReadOnlySpan<char> selfSpan = value.AsSpan();
            Span<char> otherPivot = stackalloc char[other.Length];
            int j = 0;
            for (int i = 0; i < other.Length; i++)
            {
                char c = other[i];
                if (char.IsLetterOrDigit(c))
                {
                    otherPivot[j] = char.ToLowerInvariant(c);
                    j++;
                }
                else if (c == '_')
                {
                    otherPivot[j] = '_';
                    j++;
                }
            }
            
            if (j != selfSpan.Length)
                return false;
            
            ReadOnlySpan<char> otherSpan = otherPivot[..j];
            for (int i = 0; i < otherSpan.Length; i++)
                if (selfSpan[i] != otherSpan[i])
                    return false;

            return true;
        }

        public int CompareTo(string other)
        {
            CleanString otherCleanString = new(other);
            return CompareTo(otherCleanString);
        }

        public int CompareTo(CleanString other)
        {
            CheckSanitization(ref other);
            CheckSanitization(ref this);
            return string.Compare(value, other.value, StringComparison.Ordinal);
        }

        public int CompareTo(object obj)
        {
            if (obj is CleanString other)
                return CompareTo(other);
            
            if (obj is string otherString)
                return CompareTo(otherString);
            
            return 1;
        }

        public override bool Equals(object obj)
        {
            if (obj == null && value == null)
                return true;
            
            if (obj is CleanString other && Equals(other))
                return true;

            if (obj is not string otherString)
                return false;
            
            CheckSanitization(ref this);

            ReadOnlySpan<char> selfSpan = value.AsSpan();
            Span<char> otherPivot = stackalloc char[otherString.Length];
            int j = 0;
            for (int i = 0; i < otherString.Length; i++)
            {
                char c = otherString[i];
                if (char.IsLetterOrDigit(c))
                {
                    otherPivot[j] = char.ToLowerInvariant(c);
                    j++;
                }
                else if (c == '_')
                {
                    otherPivot[j] = '_';
                    j++;
                }
            }
            
            ReadOnlySpan<char> otherSpan = otherPivot[..j];

            if (selfSpan.Length != otherSpan.Length)
                return false;

            for (int i = 0; i < otherSpan.Length; i++)
                if (selfSpan[i] != otherSpan[i])
                    return false;

            return true;
        }

        public override int GetHashCode()
        {
            CheckSanitization(ref this);
            return value != null ? value.GetHashCode() : 0;
        }

        public static bool operator ==(CleanString left, CleanString right) => left.Equals(right);
        public static bool operator !=(CleanString left, CleanString right) => !left.Equals(right);
        
        public static bool operator ==(CleanString left, string right) => left.Equals(right);
        public static bool operator !=(CleanString left, string right) => !left.Equals(right);
        
        public static bool operator ==(string left, CleanString right) => right.Equals(left);
        public static bool operator !=(string left, CleanString right) => !right.Equals(left);
        
        public static implicit operator CleanString(string value) => new(value);

        public bool StartsWith(string prefix)
        {
            CleanString cleanString = new(prefix);
            return StartsWith(ref cleanString);
        }

        public bool StartsWith(ref CleanString prefix)
        {
            CheckSanitization(ref prefix);
            CheckSanitization(ref this);
            
            ReadOnlySpan<char> selfSpan = value.AsSpan();
            ReadOnlySpan<char> otherSpan = prefix.value.AsSpan();
            if (otherSpan.Length > selfSpan.Length)
                return false;

            for (int i = 0; i < otherSpan.Length; i++)
                if (selfSpan[i] != otherSpan[i])
                    return false;

            return true;
        }
        
        public bool IsNullOrEmpty() => string.IsNullOrEmpty(value);
        public bool IsNone() => string.IsNullOrEmpty(value);
        public bool IsSome() => !string.IsNullOrEmpty(value);

        public bool Contains(string input)
        {
            if (string.IsNullOrEmpty(value))
                return false;
            
            CheckSanitization(ref this);
            CleanString cleanInput = new(input);
            return value.Contains(cleanInput.ToString());
        }

        public bool Contains(CleanString input)
        {
            if (string.IsNullOrEmpty(value))
                return false;
            
            CheckSanitization(ref this);
            return Contains(input.value);
        }

        public CleanString Remove(CleanString input)
        {
            string pivot = value.Replace(input.value, "");
            return new CleanString(pivot);
        }
    }
}