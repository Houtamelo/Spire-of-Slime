namespace Yarn
{
    using MethodCollection = System.Collections.Generic.IReadOnlyDictionary<string, System.Delegate>;

    /// <summary>
    /// Represents any type. This type is used in circumstances when a type
    /// is known to have a value, but the specific type is not known or
    /// required to be known.
    /// </summary>
    /// <remarks>
    /// This is the parent type of all types.
    /// </remarks>
    public class AnyType : IType
    {
        /// <inheritdoc/>
        public string Name => "Any";

        /// <inheritdoc/>
        public IType Parent { get => null; }

        /// <inheritdoc/>
        public string Description { get => "Any type."; }

        /// <inheritdoc/>
        public MethodCollection Methods => null;
    }
}
