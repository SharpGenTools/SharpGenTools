using System.Xml.Serialization;

namespace SharpGen.Config;

public enum StringMarshalType : byte
{
    [XmlEnum("hglobal")] GlobalHeap,
    [XmlEnum("com")] ComTaskAllocator,
    [XmlEnum("bstr")] BinaryString,
    [XmlEnum("hstring")] WindowsRuntimeString,
}