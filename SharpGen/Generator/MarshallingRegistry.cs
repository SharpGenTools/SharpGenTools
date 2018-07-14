using Microsoft.CodeAnalysis;
using SharpGen.Generator.Marshallers;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpGen.Generator
{
    public class MarshallingRegistry
    {
        public MarshallingRegistry(GlobalNamespaceProvider globalNamespace)
        {
            Marshallers = new List<IMarshaller>
            {
                new InterfaceArrayMarshaller(globalNamespace),
                new ArrayOfInterfaceMarshaller(globalNamespace),
                new BoolToIntArrayMarshaller(globalNamespace),
                new BoolToIntMarshaller(globalNamespace),
                new EnumMarshaller(globalNamespace),
                new InterfaceMarshaller(globalNamespace),
                new PointerSizeMarshaller(globalNamespace),
                new StringMarshaller(globalNamespace),
                new RemappedTypeMarshaller(globalNamespace),
                new StructWithNativeTypeMarshaller(globalNamespace),
                new StructWithNativeTypeArrayMarshaller(globalNamespace),
                new NullableInstanceMarshaller(globalNamespace),
                new BitfieldMarshaller(globalNamespace),
                new ValueTypeArrayFieldMarshaller(globalNamespace),
                new FallbackFieldMarshaller(globalNamespace),
                new ValueTypeArrayMarshaller(globalNamespace),
                new ValueTypeMarshaller(globalNamespace)
            };
        }

        private IReadOnlyList<IMarshaller> Marshallers { get; }

        public IMarshaller GetMarshaller(CsMarshalBase csElement)
        {
            var marshaller = Marshallers.FirstOrDefault(m => m.CanMarshal(csElement));
            if (marshaller == null)
            {
                throw new InvalidOperationException($"No marshaller found for {csElement}");
            }
            return marshaller;
        }

        public SyntaxToken GetMarshalStorageLocationIdentifier(CsMarshalBase csElement)
        {
            return MarshallerBase.GetMarshalStorageLocationIdentifier(csElement);
        }
    }
}
