using System.Collections.Generic;
using Core.Utils.Extensions;
using Core.Utils.Patterns;
using JetBrains.Annotations;

namespace Core.Utils.Collections
{
    public class ListStack<T> : List<T>
    {
        public Option<T> Peek() => Count > 0 ? Option<T>.Some(this[Count - 1]) : Option.None;
        
        public Option<T> Pop() => Count > 0 ? Option<T>.Some(this.TakeAt<T, List<T>>(Count - 1)) : Option.None;
        
        public void Push(T item) => Add(item);
        
        public void PushRange([NotNull] IEnumerable<T> items) => AddRange(items);
        
        public void PushRange([NotNull] params T[] items) => AddRange(items);
    }
}