/*#nullable enable
using System;

namespace Core.Utils.Patterns
{
    public readonly struct OneOf<T1, T2>
    {
        private readonly T1? _value1;
        private readonly T2? _value2;
        private readonly Type _type;

        private OneOf(T1 value)
        {
            _value1 = value;
            _value2 = default;
            _type = Type.T1;
        }
        
        private OneOf(T2 value)
        {
            _value1 = default;
            _value2 = value;
            _type = Type.T2;
        }
        
        public void Assign(out T1? t1, out T2? t2)
        {
            t1 = default;
            t2 = default;
            switch (_type)
            {
                case Type.None: break;
                case Type.T1:   t1 = _value1; break;
                case Type.T2:   t2 = _value2; break;
                default:        throw new ArgumentOutOfRangeException(nameof(_type), _type, null);
            }
        }
        
        public void Match(Action<T1> t1, Action<T2> t2)
        {
            switch (_type)
            {
                case Type.None: break;
                case Type.T1:   t1(_value1); break;
                case Type.T2:   t2(_value2); break;
                default:        throw new ArgumentOutOfRangeException(nameof(_type), _type, null);
            }
        }
        
        public static implicit operator OneOf<T1, T2>(T1 value) => new(value);
        public static implicit operator OneOf<T1, T2>(T2 value) => new(value);

        private enum Type
        {
            None = 0,
            T1 = 1,
            T2 = 2,
        }
    }
}*/