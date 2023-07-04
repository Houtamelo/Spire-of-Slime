using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using ListPool;
using NetFabric.Hyperlinq;
using UnityEngine;
using Utils.Collections;
using Random = System.Random;

namespace Utils.Extensions
{
    public static class CollectionExtensions
    {
        private static readonly Random Random = new();
        private static readonly StringBuilder Builder = new();

        public static void RemoveWhere<TKey, TValue>(this Dictionary<TKey, TValue> source, Predicate<KeyValuePair<TKey, TValue>> predicate)
        {
            using (Lease<KeyValuePair<TKey, TValue>> lease = source.AsValueEnumerable().ToArray(ArrayPool<KeyValuePair<TKey, TValue>>.Shared))
            {
                foreach (KeyValuePair<TKey, TValue> pair in lease)
                    if (predicate(pair))
                        source.Remove(pair.Key);
            }
        }

        public static FixedEnumerable<T> FixedEnumerate<T>(this ICollection<T> source) where T : IEquatable<T> => new(source);

        public static bool None<T>(this IEnumerable<T> source, Predicate<T> predicate)
        {
            foreach (T element in source)
                if (predicate.Invoke(element))
                    return false;
            
            return true;
        }
        
        public static void DoForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (T element in source)
                action.Invoke(element);
        }

        public static void ReInsert<T>(this IList<T> source, T element, int targetIndex)
        {
            int currentIndex = source.IndexOf(element);
            targetIndex = Mathf.Clamp(targetIndex, 0, source.Count - 1);
            if (currentIndex == -1 || currentIndex == targetIndex)
                return;
            
            source.RemoveAt(currentIndex);
            source.Insert(index: targetIndex, item: element);
        }
        
        public static void ReInsert<T>(this IList<T> source, int currentIndex, int targetIndex)
        {
            targetIndex = Mathf.Clamp(targetIndex, 0, source.Count - 1);
            if (currentIndex == -1 || currentIndex == targetIndex)
                return;
            
            T element = source.TakeAt(currentIndex);
            source.Insert(index: targetIndex, item: element);
        }

        public static Patterns.Option<int> IndexOf<T>(this T[] source, T value)
        {
            int result = Array.IndexOf(array: source, value: value);
            return result != -1 ? result : Patterns.Option<int>.None;
        }

        public static Patterns.Option<T> FindType<T>([NotNull] this IEnumerable source)
        {
            foreach (object variable in source)
                if (variable is T casted)
                    return casted;

            return Patterns.Option<T>.None;
        }

        public static Patterns.Option<T> Find<T>(this IEnumerable<T> source, Predicate<T> predicate)
        {
            foreach (T element in source)
                if (predicate.Invoke(obj: element))
                    return element;

            return Patterns.Option<T>.None;
        }

        public static bool IsNullOrEmpty<_>(this IEnumerable<_> source) => source == null || !source.Any();

        public static bool IsNullOrEmpty<_, T>(this Dictionary<_, T> source) => source == null || source.Count == 0;
        
        public static bool HasElements<_>(this IEnumerable<_> source) => source != null && source.Any();
        
        public static bool HasElements<_, T>(this Dictionary<_, T> source) => source is { Count: > 0 };
        
        public static bool HasElements<_>(this ICollection<_> source) => source is { Count: > 0 };

        public static bool Remove<_, T>(this Dictionary<_, T> source, Predicate<T> predicate)
        {
            _[] keys = source.Keys.ToArray();
            bool any = false;
            foreach (_ key in keys)
            {
                if (predicate.Invoke(obj: source[key: key]))
                {
                    any = any || source.Remove(key);
                }
            }

            return any;
        }

        public static T GetRandom<T>(this IEnumerable<T> source)
        {
            T[] enumerable1 = source as T[] ?? source.ToArray();
            if (enumerable1.IsNullOrEmpty())
                throw new ArgumentException(message: $"IEnumerable<{nameof(T)}> is null or empty");

            int count = enumerable1.Length;
            return enumerable1[Random.Next(maxValue: count)];
        }

        public static T TakeAt<T>(this IList<T> source, int index)
        {
            T element = source[index];
            source.RemoveAt(index);
            return element;
        }

        public static T TakeRandom<T>(this List<T> source)
        {
            int index = Random.Next(source.Count);
            T variable = source[index];
            source.RemoveAt(index);
            return variable;
        }

        public static T TakeFirst<T>(this IList<T> collection)
        {
            T value = collection[index: 0];
            collection.RemoveAt(index: 0);
            return value;
        }

        public static UncheckedDictionaryEnumerator<TKey, TValue> UncheckedEnumerate<TKey, TValue>(this Dictionary<TKey, TValue> source) => new(source);

        public static void Replace<TKey, TSource>(this Dictionary<TKey, TSource> source, Dictionary<TKey, TSource> replaceWith)
        {
            foreach ((TKey item1, TSource item2) in replaceWith)
                source[key: item1] = item2;
        }

        public static bool Remove<T>(this HashSet<T> source, Predicate<T> predicate)
        {
            bool removed = false;
            foreach (T variable in source.ToArray())
                if (predicate.Invoke(obj: variable))
                    removed = removed || source.Remove(variable);

            return removed;
        }

        public static void Remove<T>(this ICollection<T> source, IEnumerable<T> elementsToRemove)
        {
            foreach (T variable in elementsToRemove)
                source.Remove(item: variable);
        }

        public static void Add<T>(this ICollection<T> source, IEnumerable<T> elementsToAdd)
        {
            foreach (T variable in elementsToAdd) 
                source.Add(item: variable);
        }

        public static bool EnqueueIfDoesNotContain<T>(this Queue<T> source, T element)
        {
            if (!source.Contains(item: element))
            {
                source.Enqueue(item: element);
                return true;
            }

            return false;
        }

        public static T GetWeightedRandom<T>([NotNull] this ICollection<(T element, float points)> source)
        {
            float pointsSum = 0f;
            foreach ((_, float weight) in source)
                pointsSum += weight;

            if (pointsSum <= 0)
                return source.First().element;
            
            float numericValue = UnityEngine.Random.value * pointsSum;
            foreach ((T element, float weight) in source)
            {
                numericValue -= weight;
                if (!(numericValue <= 0))
                    continue;
                
                return element;
            }

            return source.First().element;
        }
        
        public static T GetWeightedRandom<T>([NotNull] this ValueListPool<(T element, float points)> source)
        {
            if (source.Count == 0)
                return default;
            
            float pointsSum = 0f;
            foreach ((_, float weight) in source)
                pointsSum += weight;

            if (pointsSum <= 0)
                return source[0].element;
            
            float numericValue = UnityEngine.Random.value * pointsSum;
            foreach ((T element, float weight) in source)
            {
                numericValue -= weight;
                if (!(numericValue <= 0))
                    continue;
                
                return element;
            }

            return source[0].element;
        }

        public static T GetWeightedRandom<T>([NotNull] this Lease<(T element, float points)> source)
        {
            float ratioSum = 0f;
            foreach ((_, float weight) in source)
                ratioSum += weight;

            if (ratioSum <= 0)
                return source.Rented[0].element;
            
            float numericValue = UnityEngine.Random.value * ratioSum;
            foreach ((T element, float weight) in source)
            {
                numericValue -= weight;
                if (!(numericValue <= 0))
                    continue;
                
                return element;
            }

            return source.Rented[0].element;
        }

        public static (T1, T2) GetWeightedRandom<T1, T2>([NotNull] this Lease<(T1 typeOne, T2 typeTwo, float points)> source)
        {
            float pointsSum = GetPointsSum(source);
            if (pointsSum <= 0)
            {
                (T1 typeOne, T2 typeTwo, _) = source.Rented[0];
                return (typeOne, typeTwo);
            }
            
            float numericValue = UnityEngine.Random.value * pointsSum;
            foreach ((T1 typeOne, T2 typeTwo, float weight) in source)
            {
                numericValue -= weight;
                if (numericValue > 0)
                    continue;
                
                return (typeOne, typeTwo);
            }

            {
                (T1 typeOne, T2 typeTwo, _) = source.Rented[0];
                return (typeOne, typeTwo);
            }
        }

        public static float GetPointsSum<T1, T2>([NotNull] this Lease<(T1 typeOne, T2 typeTwo, float points)> source)
        {
            float pointsSum = 0f;
            foreach ((_, _, float weight) in source)
                pointsSum += weight;
            
            return pointsSum;
        }

        public static string ElementsToString<TKey, TValue>(this Dictionary<TKey, TValue> source)
        {
            Builder.Clear();
            foreach ((TKey key, TValue value) in source)
                Builder.AppendLine(value: $"{key} = {value}");

            return Builder.ToString();
        }
        
        public static string ElementsToString<T>(this IReadOnlyCollection<T> source)
        {
            Builder.Clear();
            foreach (T element in source)
                Builder.AppendLine(value: element.ToString());

            return Builder.ToString();
        }

        public static string ElementsToString<T>(this IEnumerable<T> source)
        {
            Builder.Clear();
            foreach (T element in source)
                Builder.AppendLine(value: element.ToString());
            
            return Builder.ToString();
        }
        
        public static string ElementsToString<T>(this IEnumerable<T> source, Func<T, string> selector)
        {
            Builder.Clear();
            foreach (T element in source)
                Builder.AppendLine(value: selector.Invoke(element));

            return Builder.ToString();
        }

        public static bool AddIfDoesNotContain<T>(this List<T> source, T element)
        {
            if (!source.Contains(element))
            {
                source.Add(element);
                return true;
            }

            return false;
        }

        public static void Remove<TKey, TValue>(this Dictionary<TKey, TValue> source, IEnumerable<TKey> keys)
        {
            foreach (TKey key in keys)
                source.Remove(key);
        }

        public static void Remove<TKey, TValue>(this Dictionary<TKey, TValue> source, ICollection<TValue> elements) where TValue : IEquatable<TValue>
        {
            using UncheckedDictionaryEnumerator<TKey, TValue> enumerator = source.UncheckedEnumerate();
            {
                foreach ((TKey key, TValue equatable) in enumerator)
                    foreach (TValue value in elements)
                        if (equatable.Equals(other: value))
                        {
                            source.Remove(key);
                            break;
                        }
            }
        }

        public static void EnsureUniqueness<T>(this IList<T> source) where T : IEquatable<T>
        {
            for (int i = 0; i < source.Count; i++)
            {
                T outer = source[index: i];
                for (int j = i + 1; j < source.Count; j++)
                {
                    T inner = source[index: j];
                    if (inner.Equals(other: outer))
                    {
                        source.RemoveAt(index: j);
                        j--;
                    }
                }
            }
        }

        public static void Shuffle<T>(this IList<T> source)  
        {
            int n = source.Count;
            while (n > 1)
            {
                n--;
                int k = Random.Next(maxValue: n + 1);
                (source[index: k], source[index: n]) = (source[index: n], source[index: k]);
            }  
        }

        public static int GetWeightedRandomIndex(this float[] source)
        {
            var total = 0f;
            for (int i = 0; i < source.Length; i++)
                total += source[i];
            
            double randomPoint = Random.NextDouble() * total;
            
            for (int i = 0; i < source.Length; i++)
            {
                if (randomPoint < source[i])
                    return i;
                
                randomPoint -= source[i];
            }
            
            return source.Length - 1;
        }

        public static SafeDictionaryEnumerator<TKey, TValue> FixedEnumerate<TKey, TValue>(this IDictionary<TKey, TValue> source)
        {
            return new SafeDictionaryEnumerator<TKey, TValue>(source);
        }

        [MustUseReturnValue]
        public static Lease<T> ToSingleElementLease<T>(this T element)
        {
            Lease<T> lease = ArrayPool<T>.Shared.Lease(1);
            lease.Rented[0] = element;
            return lease;
        }

        public static bool Contains<T>(this IReadOnlyList<T> source, IEnumerable<T> elementsToContain)
        {
            foreach (T element in elementsToContain)
                if (!source.Contains(element))
                    return false;

            return true;
        }
        
        public struct UncheckedDictionaryEnumerator<TKey, TValue> : IDisposable
        {
            private readonly Lease<KeyValuePair<TKey, TValue>> _lease;
            private int _index;
            private KeyValuePair<TKey, TValue> _current;

            public UncheckedDictionaryEnumerator(Dictionary<TKey, TValue> dictionary)
            {
                _lease = dictionary.AsValueEnumerable().ToArray(ArrayPool<KeyValuePair<TKey, TValue>>.Shared);
                _index = 0;
                _current = default;
            }
            
            public bool MoveNext()
            {
                if (_index >= _lease.Length)
                    return false;
                
                _current = _lease.ElementAt(_index);
                _index++;
                return true;
            }
            
            public void Reset()
            {
                _index = 0;
                _current = default;
            }
            
            public KeyValuePair<TKey, TValue> Current => _current;

            public void Dispose()
            {
                _lease.Dispose();
            }

            public UncheckedDictionaryEnumerator<TKey, TValue> GetEnumerator() { return this; }
        }
    }
}