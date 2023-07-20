using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.Utils.Collections;
using Core.Utils.Collections.Extensions;
using JetBrains.Annotations;
using ListPool;
using NetFabric.Hyperlinq;
using UnityEngine;
using Random = System.Random;

namespace Core.Utils.Extensions
{
    public static class ClassCollectionExtensions
    {
        private static readonly Random Random = new();
        private static readonly StringBuilder Builder = new();
        
        public static bool IsNullOrEmpty<TCollection>([CanBeNull] this TCollection source) where TCollection : class, ICollection
            => source == null || source.Count == 0;

        public static bool IsNullOrEmpty<TCollection, TValue>([CanBeNull] this TCollection source) where TCollection : class, ICollection<TValue>
            => source == null || source.Count == 0;
        
        public static bool HasElements<TCollection>(this TCollection source) where TCollection : class, ICollection
            => source is { Count: > 0 };

        public static bool HasElements<TCollection, TValue>(this TCollection source) where TCollection : class, ICollection<TValue>
            => source is { Count: > 0 };

        public static T TakeAt<T, TCollection>([NotNull] this TCollection source, int index) where TCollection : class, IList<T>
        {
            T element = source[index];
            source.RemoveAt(index);
            return element;
        }

        public static T TakeRandom<T, TCollection>([NotNull] this TCollection source) where TCollection : class, IList<T>
        {
            int index = Random.Next(source.Count);
            T variable = source[index];
            source.RemoveAt(index);
            return variable;
        }

        public static T TakeFirst<T, TCollection>([NotNull] this TCollection collection) where TCollection : class, IList<T>
        {
            T value = collection[0];
            collection.RemoveAt(index: 0);
            return value;
        }
        
        public static void Remove<T, TCollection>(this TCollection source, [NotNull] T[] elementsToRemove) where TCollection : class, ICollection<T>
        {
            foreach (T variable in elementsToRemove)
                source.Remove(variable);
        }
    }
}