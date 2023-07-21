using System.Collections.Generic;
using Core.Utils.Patterns;

namespace Core.Utils.Collections
{
    public class ListQueue<T> : List<T>
    {
        public Option<T> Dequeue()
        {
            if (Count == 0)
                return Option.None;
            
            T value = this[0];
            RemoveAt(0);
            return value;
        }
        
        public void Enqueue(T value)
        {
            Add(value);
        }
        
        public Option<T> Peek()
        {
            if (Count == 0)
                return Option.None;
            
            return this[0];
        }
    }
}