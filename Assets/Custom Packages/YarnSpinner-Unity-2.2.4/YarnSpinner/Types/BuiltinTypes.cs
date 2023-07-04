namespace Yarn
{
    using System.Collections.Generic;

    /// <summary>
    /// Contains the built-in types available in the Yarn language.
    /// </summary>
    public static class BuiltinTypes
    {
        /// <summary>An undefined type.</summary>
        /// <remarks>This value is not valid except during compilation. It
        /// is used to represent values that have not yet been assigned a
        /// type by the type system.</remarks>
        public const IType Undefined = null;

        /// <summary>Gets the type representing strings.</summary>
        public static IType String { get; } = new StringType();

        /// <summary>Gets the type representing numbers.</summary>
        public static IType Number { get; } = new NumberType();

        /// <summary>Gets the type representing boolean values.</summary>
        public static IType Boolean { get; } = new BooleanType();

        /// <summary>Gets the type representing any value.</summary>
        public static IType Any { get; } = new AnyType();

        /// <summary>
        /// Gets a dictionary that maps CLR types to their corresponding
        /// Yarn types.
        /// </summary>
        public static IReadOnlyDictionary<System.Type, IType> TypeMappings { get; } = new Dictionary<System.Type, IType>
        {
            { typeof(string), String },
            { typeof(bool), Boolean },
            { typeof(int), Number },
            { typeof(float), Number },
            { typeof(double), Number },
            { typeof(sbyte), Number },
            { typeof(byte), Number },
            { typeof(short), Number },
            { typeof(ushort), Number },
            { typeof(uint), Number },
            { typeof(long), Number },
            { typeof(ulong), Number },
            { typeof(decimal), Number },
            { typeof(object), Any },
        };

        /// <summary>
        /// Gets a <see cref="IEnumerable{T}"/> containing all built-in
        /// properties defined in this class.
        /// </summary>
        /// <value>The list of built-in type objects.</value>
        public static IEnumerable<IType> AllBuiltinTypes
        {
            get
            {
                // Find all static properties of BuiltinTypes that are
                // public
                var propertyInfos = typeof(BuiltinTypes)
                    .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

                List<IType> result = new List<IType>();

                foreach (var propertyInfo in propertyInfos)
                {
                    // If the type of this property is IType, then this is
                    // a built-in type!
                    if (propertyInfo.PropertyType == typeof(IType)) {
                        // Get that value.
                        var builtinType = (IType)propertyInfo.GetValue(null);

                        // If it's not null (i.e. the undefined type), then
                        // add it to the type objects we're returning!
                        if (builtinType != null) {
                            result.Add(builtinType);
                        }
                    }
                }

                return result;
            }
        }
    }
}
