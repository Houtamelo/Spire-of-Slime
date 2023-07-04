using System;
using Utils.Patterns;

namespace Utils.Handlers
{
    public class ValueHandler<T>
    {
        public event Action<T> Changed;
        public T Value { get; private set; }

        public void SetValue(T value)
        {
            Value = value;
            Changed?.Invoke(value);
        }
    }

    public class ValueHandler<T1, T2>
    {
        public event Action<T1, T2> Changed;
        public T1 ValueOne { get; private set; }
        public T2 ValueTwo { get; private set; }

        public void SetValue(T1 valueOne, T2 valueTwo)
        {
            ValueOne = valueOne;
            ValueTwo = valueTwo;
            Changed?.Invoke(valueOne, valueTwo);
        }
    }

    public class NullableHandler<T> where T : class
    {
        public event Action<Option<T>> Changed;
        private T _value;
        public bool IsSome { get; private set; }
        public bool IsNone => !IsSome;
        public T Value
        {
            get
            {
                if (IsSome == false)
                    throw new InvalidOperationException("Check if NullableHandler has value before accessing it.");
                
                return _value;
            }
        }

        public void ClearValue()
        {
            _value = default;
            IsSome = false;
            Changed?.Invoke(Option<T>.None);
        }

        public void AddValue(T value, bool checkIfDefault)
        {
            if (checkIfDefault && value.Equals(default))
            {
                ClearValue();
                return;
            }
            
            _value = value;
            IsSome = true;
            Changed?.Invoke(Option<T>.Some(value));
        }
        
        public Option<T> AsOption() => IsSome ? Option<T>.Some(_value) : Option<T>.None;
    }
}