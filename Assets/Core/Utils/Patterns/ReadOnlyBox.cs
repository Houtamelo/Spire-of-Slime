using JetBrains.Annotations;

namespace Core.Utils.Patterns
{
    public class ReadOnlyBox<T> where T : struct
    {
        public readonly T Value;
        public ReadOnlyBox(T value) => Value = value;
        
        public static implicit operator T([NotNull] ReadOnlyBox<T> box) => box.Value;
    }
}