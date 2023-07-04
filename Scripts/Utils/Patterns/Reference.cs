namespace Utils.Patterns
{
    public class Reference<T>
    {
        public T Value;
        
        public Reference(T obj)
        {
            Value = obj;
        }
        
        public static implicit operator T(Reference<T> cell) => cell.Value;
        
        public static implicit operator Reference<T>(T obj) => new(obj);
    }
}