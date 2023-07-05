using System.Collections.Generic;
using Core.Utils.Patterns;

namespace Core.Utils.Collections
{
    public class ListQueue<T> : List<T>
    {
        public Option<T> Dequeue()
        {
            if (Count == 0)
                return Option<T>.None;
            
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