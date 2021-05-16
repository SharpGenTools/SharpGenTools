using System;
using System.Reflection;

namespace SharpGen.Runtime
{
    [AttributeUsage(AttributeTargets.Interface)]
    public sealed class ExcludeFromTypeListAttribute : Attribute
    {
        /// <summary>
        /// Get <see cref="ExcludeFromTypeListAttribute"/> from type.
        /// </summary>
        /// <returns>The associated attribute or null if no attribute were found</returns>
        public static ExcludeFromTypeListAttribute Get(Type type) =>
            type.GetTypeInfo().GetCustomAttribute<ExcludeFromTypeListAttribute>();

        /// <summary>
        /// Check presence of <see cref="ExcludeFromTypeListAttribute"/> on the specified type.
        /// </summary>
        /// <returns>true if attribute was found on the specified type</returns>
        public static bool Has(Type type) => Get(type) != null;
    }
}
