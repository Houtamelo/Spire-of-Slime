using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace Core.Misc
{
    [Serializable]
    public class SerializableTuple<T1, T2> : IEquatable<SerializableTuple<T1, T2>>
    {
        [SerializeField] private T1 item1;
        public T1 Item1 => item1;

        [SerializeField] private T2 item2;
        public T2 Item2 => item2;

        public SerializableTuple()
        {

        }

        public SerializableTuple(T1 item1, T2 item2)
        {
            this.item1 = item1;
            this.item2 = item2;
        }

        public bool Equals(SerializableTuple<T1, T2> other)
        {
            bool isOtherNull = ReferenceEquals(other, null);
            bool isThisNull = ReferenceEquals(this, null);
            if (isOtherNull && isThisNull)
                return true;

            if (isOtherNull || isThisNull)
                return false;
            
            EqualityComparer<T1> comparer1 = EqualityComparer<T1>.Default;
            EqualityComparer<T2> comparer2 = EqualityComparer<T2>.Default;

            return comparer1.Equals(item1, other.item1) &&
                comparer2.Equals(item2, other.item2);
        }

        public override int GetHashCode()
        {
            EqualityComparer<T1> comparer1 = EqualityComparer<T1>.Default;
            EqualityComparer<T2> comparer2 = EqualityComparer<T2>.Default;
            int h0 = comparer1.GetHashCode(item1);
            h0 = ((h0 << 5) + h0) ^ comparer2.GetHashCode(item2);
            return h0;
        }

        public override string ToString() => $"({item1}, {item2})";

        public void Deconstruct(out T1 ctxitem1, out T2 ctxItem2)
        {
            ctxitem1 = item1;
            ctxItem2 = item2;
        }

        public static implicit operator (T1,T2) ([NotNull] SerializableTuple<T1,T2> tuple) => (tuple.item1, tuple.item2);
        [NotNull]
        public static implicit operator SerializableTuple<T1,T2> ((T1,T2) tuple) => new(tuple.Item1, tuple.Item2);
        
        public (T1, T2) ToValue() => (item1, item2);
    }

    [Serializable]
    public class SerializableTuple<T1, T2, T3> : IEquatable<SerializableTuple<T1, T2, T3>>
    {
        [SerializeField] private T1 item1;
        public T1 Item1 => item1;

        [SerializeField] private T2 item2;
        public T2 Item2 => item2;

        [SerializeField] private T3 item3;
        public T3 Item3 => item3;

        public SerializableTuple()
        {

        }

        public SerializableTuple(T1 item1, T2 item2, T3 item3)
        {
            this.item1 = item1;
            this.item2 = item2;
            this.item3 = item3;
        }

        public bool Equals(SerializableTuple<T1, T2, T3> other)
        {
            bool isOtherNull = ReferenceEquals(other, null);
            bool isThisNull = ReferenceEquals(this, null);
            if (isOtherNull && isThisNull)
                return true;

            if (isOtherNull || isThisNull)
                return false;
            
            EqualityComparer<T1> comparer1 = EqualityComparer<T1>.Default;
            EqualityComparer<T2> comparer2 = EqualityComparer<T2>.Default;
            EqualityComparer<T3> comparer3 = EqualityComparer<T3>.Default;

            return comparer1.Equals(item1, other.item1) &&
                comparer2.Equals(item2, other.item2) &&
                comparer3.Equals(item3, other.item3);
        }

        public override int GetHashCode()
        {
            var comparer1 = EqualityComparer<T1>.Default;
            var comparer2 = EqualityComparer<T2>.Default;
            var comparer3 = EqualityComparer<T3>.Default;

            int h0;
            h0 = comparer1.GetHashCode(item1);
            h0 = ((h0 << 5) + h0) ^ comparer2.GetHashCode(item2);
            h0 = ((h0 << 5) + h0) ^ comparer3.GetHashCode(item3);
            return h0;
        }

        public override string ToString() => $"({item1}, {item2}, {item3})";

        public void Deconstruct(out T1 ctxitem1, out T2 ctxItem2, out T3 ctxItem3)
        {
            ctxitem1 = item1;
            ctxItem2 = item2;
            ctxItem3 = item3;
        }
        
        public static implicit operator (T1,T2,T3) ([NotNull] SerializableTuple<T1,T2,T3> tuple) => (tuple.item1, tuple.item2, tuple.item3);
        [NotNull]
        public static implicit operator SerializableTuple<T1,T2,T3> ((T1,T2,T3) tuple) => new(tuple.Item1, tuple.Item2, tuple.Item3);
    }

    [Serializable]
    public class SerializableTuple<T1, T2, T3, T4> : IEquatable<SerializableTuple<T1, T2, T3, T4>>
    {
        [SerializeField] private T1 item1;
        public T1 Item1 => item1;

        [SerializeField] private T2 item2;
        public T2 Item2 => item2;

        [SerializeField] private T3 item3;
        public T3 Item3 => item3;

        [SerializeField] private T4 item4;
        public T4 Item4 => item4;

        public SerializableTuple()
        {

        }

        public SerializableTuple(T1 item1, T2 item2, T3 item3, T4 item4)
        {
            this.item1 = item1;
            this.item2 = item2;
            this.item3 = item3;
            this.item4 = item4;
        }

        public bool Equals(SerializableTuple<T1, T2, T3, T4> other)
        {
            bool isOtherNull = ReferenceEquals(other, null);
            bool isThisNull = ReferenceEquals(this, null);
            if (isOtherNull && isThisNull)
                return true;

            if (isOtherNull || isThisNull)
                return false;
            
            EqualityComparer<T1> comparer1 = EqualityComparer<T1>.Default;
            EqualityComparer<T2> comparer2 = EqualityComparer<T2>.Default;
            EqualityComparer<T3> comparer3 = EqualityComparer<T3>.Default;
            EqualityComparer<T4> comparer4 = EqualityComparer<T4>.Default;

            return comparer1.Equals(item1, other.item1) &&
                comparer2.Equals(item2, other.item2) &&
                comparer3.Equals(item3, other.item3) &&
                comparer4.Equals(item4, other.item4);
        }

        public override int GetHashCode()
        {
            var comparer1 = EqualityComparer<T1>.Default;
            var comparer2 = EqualityComparer<T2>.Default;
            var comparer3 = EqualityComparer<T3>.Default;
            var comparer4 = EqualityComparer<T4>.Default;

            int h0, h1;
            h0 = comparer1.GetHashCode(item1);
            h0 = ((h0 << 5) + h0) ^ comparer2.GetHashCode(item2);
            h1 = comparer3.GetHashCode(item3);
            h1 = ((h1 << 5) + h1) ^ comparer4.GetHashCode(item4);
            h0 = ((h0 << 5) + h0) ^ h1;
            return h0;
        }

        public override string ToString() => $"({item1}, {item2}, {item3}, {item4})";

        public void Deconstruct(out T1 ctxitem1, out T2 ctxItem2, out T3 ctxItem3, out T4 ctxItem4)
        {
            ctxitem1 = item1;
            ctxItem2 = item2;
            ctxItem3 = item3;
            ctxItem4 = item4;
        }
        
        public static implicit operator (T1,T2,T3,T4) ([NotNull] SerializableTuple<T1,T2,T3,T4> tuple) => (tuple.item1, tuple.item2, tuple.item3, tuple.item4);
        [NotNull]
        public static implicit operator SerializableTuple<T1,T2,T3,T4> ((T1,T2,T3,T4) tuple) => new(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4);
    }
}