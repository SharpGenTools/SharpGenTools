namespace SharpGen.Model
{
    public static class CsElementExtensions
    {
        public static CsInterface GetNativeImplementationOrThis(this CsInterface iface)
        {
            return iface.NativeImplementation ?? iface;
        }
    }
}
