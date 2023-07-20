using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using Core.Utils.Extensions;
using JetBrains.Annotations;
using NetFabric.Hyperlinq;

namespace Core.Utils.Collections.Extensions
{
	public static class DictionaryExtensions
    {
        private static readonly StringBuilder Builder = new();
        
		public static void Remove<TKey, TValue>(this Dictionary<TKey, TValue> source, [NotNull] TKey[] keys)
        {
            foreach (TKey key in keys)
                source.Remove(key);
        }

        public static void RemoveValues<TKey, TValue>(this Dictionary<TKey, TValue> source, TValue[] values)
        {
            EqualityComparer<TValue> comparer = EqualityComparer<TValue>.Default;

            foreach ((TKey key, TValue equatable) in source.FixedEnumerate())
            {
                foreach (TValue value in values)
                {
                    if (comparer.Equals(equatable, value))
                    {
                        source.Remove(key);
                        break;
                    }
                }
            }
        }

        public static bool RemoveValue<TKey, TValue>(this Dictionary<TKey, TValue> source, TValue elementToRemove)
        {
            EqualityComparer<TValue> comparer = EqualityComparer<TValue>.Default;
            bool any = false;
            foreach ((TKey key, TValue value) in source.FixedEnumerate())
            {
                if (comparer.Equals(value, elementToRemove))
                {
                    any = true;
                    source.Remove(key);
                }
            }
            
            return any;
        }
        
        public static void RemoveWhere<TKey, TValue>(this Dictionary<TKey, TValue> source, Predicate<KeyValuePair<TKey, TValue>> predicate)
        {
            foreach (KeyValuePair<TKey, TValue> pair in source.FixedEnumerate())
            {
                if (predicate(pair))
                    source.Remove(pair.Key);
            }
        }
        
        public static void Replace<TKey, TValue, TCollection>(this TCollection source, [NotNull] Dictionary<TKey, TValue> replaceWith) where TCollection : class, IDictionary<TKey, TValue>
        {
            foreach ((TKey key, TValue value) in replaceWith)
                source[key] = value;
        }

        public static FixedEnumerator<KeyValuePair<TKey, TValue>> FixedEnumerate<TKey, TValue>([NotNull] this Dictionary<TKey, TValue> source) 
            => FixedEnumerator<KeyValuePair<TKey, TValue>>.FromDictionary(source);
        
        [NotNull]
        public static string ElementsToString<TKey, TValue>([NotNull] this Dictionary<TKey, TValue> source)
        {
            Builder.Clear();
            foreach ((TKey key, TValue value) in source)
                Builder.Append('(', key.ToString(), value.ToString(), ')');

            return Builder.ToString();
        }
        
        public static void DoForEach<TKey, TValue>([NotNull] this Dictionary<TKey, TValue>.ValueCollection source, Action<TValue> action)
        {
            foreach (TValue variable in source)
                action(variable);
        }
	}
}