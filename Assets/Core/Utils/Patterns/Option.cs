using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using UnityEngine;

namespace Utils.Patterns
{
    public readonly struct Option<T>
    {
        private readonly T _value;
        public readonly bool IsSome;
        [Pure] public bool TrySome(out T value)
        {
            value = _value;
            return IsSome;
        }

        /// <summary> Logs Warning if none </summary>
        [Pure]
        public bool AssertSome(out T value)
        {
            if (IsNone)
                Debug.LogWarning($"Option of {typeof(T).Name} is none");
            
            value = _value;
            return IsSome;
        }
        
        public bool IsNone => !IsSome;
        public T Value
        {
            get
            {
                #if UNITY_EDITOR
                if (IsSome)
                    return _value;
                else
                    throw new InvalidOperationException("Check if option is some before accessing value.");
                #else
                return _value;
                #endif
            }
        }

        /// <summary>
        /// Returns default value if option is none.
        /// </summary>
        public T SomeOrDefault() => _value;

        private Option(bool isSome, T value)
        {
            IsSome = isSome;
            _value = value;
        }

        public static Option<T> None => new(false, default);
        public static Option<T> Some(T value) => new(true, value);

        public static implicit operator Option<T>(T value) => Some(value);
        public static implicit operator Option<T>(NoneOption _) => default;
        public static implicit operator Option<T>(Option _) => default;

        public bool Equals(Option<T> other)
        {
            return IsSome == other.IsSome && EqualityComparer<T>.Default.Equals(_value, other._value);
        }

        public override bool Equals(object obj)
        {
            return obj is Option<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(IsSome, IsSome ? _value : default);
        }
        
        public static bool operator ==(Option<T> left, Option<T> right) => left.Equals(right);
        public static bool operator !=(Option<T> left, Option<T> right) => !left.Equals(right);
    }

    public readonly ref struct NoneOption
    {
    }

    public enum Option
    {
        None
    }
}