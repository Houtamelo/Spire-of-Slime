using System.Collections.Generic;

namespace Utils.Collections
{
    public class ListQueue<T> : List<T>
    {
        public Patterns.Option<T> Dequeue()
        {
            if (Count == 0)
                return Patterns.Option<T>.None;
            
            T value = this[0];
            RemoveAt(0);
            return value;
        }
        
        public void Enqueue(T value)
        {
            Add(value);
        }
    }
}