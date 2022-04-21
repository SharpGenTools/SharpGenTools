namespace SharpGen.Model;

public static class CsElementExtensions
{
    public static CsInterface GetNativeImplementationOrThis(this CsInterface iface) =>
        iface.NativeImplementation ?? iface;

    public static string GetNativeImplementationQualifiedName(this CsTypeBase type) =>
        type is CsInterface iface ? iface.GetNativeImplementationOrThis().QualifiedName : type.QualifiedName;
}