using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Core.Utils.Collections
{
    public class IndexableHashSet<T> : List<T> 
    {
        public new void Add([CanBeNull] T item)
        {
            if (Contains(item) == false)
            {
                base.Add(item);
            }
            else
            {
                Debug.Log($"Trying to add duplicate element to SafeListSet, element: {(item != null ? item.ToString() : "null")}");
                Debug.Break();
            }
        }
        
        public new void AddRange([NotNull] IEnumerable<T> collection)
        {
            foreach (T item in collection)
                Add(item);
        }
        
        public new void Insert(int index, T item)
        {
            if (Contains(item) == false)
                base.Insert(index, item);
        }
        
        public new void InsertRange(int index, [NotNull] IEnumerable<T> collection)
        {
            foreach (T item in collection)
                Insert(index, item);
        }

        public IndexableHashSet([NotNull] ICollection<T> source) : base(source.Count)
        {
            foreach (T element in source)
                Add(element);
        }
        
        public IndexableHashSet() : base() {}
        
        public IndexableHashSet(int capacity) : base(capacity) {}

        public IndexableHashSet([NotNull] IEnumerable<T> enumerable) : base()
        {
            foreach (T element in enumerable)
                Add(element);
        }
    }
}