namespace Utils.Patterns
{
    public class ReadOnlyReference<T>
    {
        public readonly T Value;
        
        public ReadOnlyReference(T obj)
        {
            Value = obj;
        }
        
        public static implicit operator T(ReadOnlyReference<T> cell) => cell.Value;
        
        public static implicit operator ReadOnlyReference<T>(T obj) => new(obj);
    }
}