namespace Core.Utils.Patterns
{
    public interface IDeepCloneable<T>
    {
        T DeepClone();
    }
}