namespace Functions
{
    public partial struct StructWithStaticMarshal
    {
        internal static void __MarshalTo(ref StructWithStaticMarshal @this, ref StructWithStaticMarshal.__Native @ref)
        {
            @this.__MarshalTo(ref @ref);
        }

        internal static void __MarshalFrom(ref StructWithStaticMarshal @this, ref StructWithStaticMarshal.__Native @ref)
        {
            @this.__MarshalFrom(ref @ref);
        }

        internal static void __MarshalFree(ref StructWithStaticMarshal @this, ref StructWithStaticMarshal.__Native @ref)
        {
            @this.__MarshalFree(ref @ref);
        }
    }
}