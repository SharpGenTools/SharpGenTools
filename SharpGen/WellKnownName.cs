namespace SharpGen
{
    /// <summary> Types the SharpGen generator assumes are present in the SharpGen global namespace. </summary>
    public enum WellKnownName
    {
        /// <summary>Return code type</summary>
        Result,

        /// <summary>Function callbacks</summary>
        FunctionCallback,

        /// <summary>Pointer-sized types.</summary>
        /// <remarks>
        ///     This type exists because of a Mono bug with struct marshalling and calli with pointer members.
        ///     Parameters and return types with this will automatically be marshalled to void* by the generator.
        /// </remarks>
        PointerSize,

        /// <summary>Base class for all interface types</summary>
        CppObject,

        /// <summary>Base interface for all callback types</summary>
        ICallbackable,

        /// <summary>Helper class for bool-to-int shortcuts with arrays </summary>
        BooleanHelpers,

        /// <summary>Helper class for low level memory operations</summary>
        MemoryHelpers,

        /// <summary>Helper class for string marshalling</summary>
        StringHelpers,

        /// <summary>Utility class that enables speedup for passing arrays of interface objects</summary>
        InterfaceArray,

        /// <summary>
        ///     Base class for all shadow objects.
        /// </summary>
        CppObjectShadow,

        /// <summary>
        ///     Base class for all shadow virtual method tables.
        /// </summary>
        CppObjectVtbl,

        /// <summary>
        ///     Attribute that defines the shadow type for an interface.
        /// </summary>
        ShadowAttribute,

        /// <summary>
        ///     Static class that enables platform detection for SharpGen-generated code.
        /// </summary>
        PlatformDetection,

        /// <summary>
        ///     Native `long` type
        /// </summary>
        NativeLong,

        /// <summary>
        ///     Native `unsigned long` type
        /// </summary>
        NativeULong
    }
}