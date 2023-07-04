using System.Collections.Generic;
using Utils.Extensions;
using Utils.Patterns;

namespace Utils.Collections
{
    public class ListStack<T> : List<T>
    {
        public Option<T> Peek() => Count > 0 ? Option<T>.Some(this[Count - 1]) : Option.None;
        
        public Option<T> Pop() => Count > 0 ? Option<T>.Some(this.TakeAt(Count - 1)) : Option.None;
        
        public void Push(T item) => Add(item);
        
        public void PushRange(IEnumerable<T> items) => AddRange(items);
        
        public void PushRange(params T[] items) => AddRange(items);
    }
}