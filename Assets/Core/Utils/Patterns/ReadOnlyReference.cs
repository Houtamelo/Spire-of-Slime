using JetBrains.Annotations;

namespace Core.Utils.Patterns
{
    public class ReadOnlyReference<T>
    {
        public readonly T Value;
        
        public ReadOnlyReference(T obj) => Value = obj;

        public static implicit operator T([NotNull] ReadOnlyReference<T> cell) => cell.Value;
        
        [NotNull]
        public static implicit operator ReadOnlyReference<T>(T obj) => new(obj);
    }
}