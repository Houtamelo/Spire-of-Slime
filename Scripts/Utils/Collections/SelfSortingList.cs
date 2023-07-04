using System.Collections.Generic;

namespace Utils.Collections
{
    public class SelfSortingList<T> : List<T>
    {
        private readonly IComparer<T> _comparer;

        public SelfSortingList(IComparer<T> comparer)
        {
            _comparer = comparer;
        }

        public new void Add(T item)
        {
            int index = BinarySearch(item: item, comparer: _comparer);
            if (index < 0)
                index = ~index;
            Insert(index: index, item: item);
        }
        
        public new void AddRange(IEnumerable<T> collection)
        {
            foreach (T item in collection)
                Add(item: item);
        }
        
        public new void Insert(int index, T item)
        {
            int newIndex = BinarySearch(item: item, comparer: _comparer);
            if (newIndex < 0)
                newIndex = ~newIndex;
            base.Insert(index: newIndex, item: item);
        }
        
        public new void InsertRange(int index, IEnumerable<T> collection)
        {
            foreach (T item in collection)
                Insert(index: index, item: item);
        }
        
        public new void Sort()
        {
            Sort(comparer: _comparer);
        }
        
        public new void Sort(IComparer<T> comparer)
        {
            Sort(index: 0, count: Count, comparer: comparer);
        }
    }
}