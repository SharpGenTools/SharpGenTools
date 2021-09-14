using System;

namespace SharpGen.Runtime.Win32
{
    public readonly partial struct PropertyKey
    {
        public PropertyKey(Guid formatId, int propertyId)
        {
            FormatId = formatId;
            PropertyId = propertyId;
        }
    }
}