using System;
using JetBrains.Annotations;

namespace Core.Utils.Patterns
{
    public readonly struct Result<T>
    {
        private readonly T _value;
        public readonly bool IsOk;
        public bool IsErr => !IsOk;
        public T Value
        {
            get
            {
#if UNITY_EDITOR
                if (IsOk)
                    return _value;
                else
                    throw new InvalidOperationException("Check if result is ok before accessing value.");
#else
                return _value;
#endif
            }
        }

        private readonly string _reason;
        public string Reason
        {
            get
            {
#if UNITY_EDITOR
                if (IsOk)
                    throw new InvalidOperationException("Check if result is err before accessing reason.");
                else
                    return _reason;
#else
                return _reason;
#endif
            }
        }

        /// <summary>
        /// Returns default value if result is error.
        /// </summary>
        public T GetIfOk() => _value;

        private Result(bool isOk, T value)
        {
            IsOk = isOk;
            _value = value;
            _reason = null;
        }
        
        private Result(string reason)
        {
            IsOk = false;
            _value = default;
            _reason = reason;
        }

        public static Result<T> Ok(T value) => new(true, value);
        public static Result<T> Error([NotNull] Exception error) => new(error.Message);
        public static Result<T> Error(string reason) => new(reason);
        public static implicit operator Result<T>(T value) => Ok(value);
        public static implicit operator Result<T>([NotNull] Exception error) => Error(error);
    }
}