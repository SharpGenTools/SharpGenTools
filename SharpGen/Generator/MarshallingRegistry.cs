using System;
using System.Collections.Generic;
using System.Linq;
using SharpGen.Generator.Marshallers;
using SharpGen.Logging;
using SharpGen.Model;

namespace SharpGen.Generator
{
    public sealed class MarshallingRegistry
    {
        private readonly Ioc ioc;

        public MarshallingRegistry(Ioc ioc)
        {
            this.ioc = ioc ?? throw new ArgumentNullException(nameof(ioc));

            ValueTypeMarshaller valueTypeMarshaller = new(ioc);
            Marshallers = new List<IMarshaller>
            {
                new InterfaceArrayMarshaller(ioc),
                new ArrayOfInterfaceMarshaller(ioc),
                new BoolToIntArrayMarshaller(ioc),
                new BoolToIntMarshaller(ioc),
                new InterfaceMarshaller(ioc),
                new PointerSizeMarshaller(ioc),
                new StringMarshaller(ioc),
                new RemappedTypeMarshaller(ioc),
                new StructWithNativeTypeMarshaller(ioc),
                new StructWithNativeTypeArrayMarshaller(ioc),
                new NullableInstanceMarshaller(ioc),
                new BitfieldMarshaller(ioc),
                new ValueTypeArrayMarshaller(ioc),
                new FallbackFieldMarshaller(ioc),
                valueTypeMarshaller
            };
            RefWrappingMarshallers = Marshallers.Select(x => new RefWrapperMarshaller(ioc, x)).ToArray();
            EnumWrappingMarshaller = new EnumParameterWrapperMarshaller(ioc, valueTypeMarshaller);
            RelationMarshallers = new Dictionary<Type, IRelationMarshaller>
            {
                [typeof(StructSizeRelation)] = new StructSizeRelationMarshaller(ioc),
                [typeof(LengthRelation)] = new LengthRelationMarshaller(ioc),
                [typeof(ConstantValueRelation)] = new ConstantValueRelationMarshaller(ioc)
            };
        }

        private IReadOnlyList<IMarshaller> Marshallers { get; }
        private IReadOnlyList<WrapperMarshallerBase> RefWrappingMarshallers { get; }
        private WrapperMarshallerBase EnumWrappingMarshaller { get; }

        private IReadOnlyDictionary<Type, IRelationMarshaller> RelationMarshallers { get; }

        public IMarshaller GetMarshaller(CsMarshalBase csElement)
        {
            var marshaller = GetMarshallers(csElement).FirstOrDefault(m => m.CanMarshal(csElement));
            if (marshaller != null)
                return marshaller;

            if (csElement.PublicType is CsUndefinedType || csElement.MarshalType is CsUndefinedType)
            {
                var logger = ioc.Logger;

                logger.Error(
                    LoggingCodes.CannotMarshalUnknownType,
                    $"The element '{csElement}' has an unknown type and cannot be marshalled accurately. Maybe you used a <bind> directive and didn't use a <define> to define the type for SharpGen?"
                );

                logger.Exit("Unable to generate code with unknown marshal type.");

                return null;
            }

            throw new InvalidOperationException($"No marshaller found for {csElement}");
        }

        private IEnumerable<IMarshaller> GetMarshallers(CsMarshalBase csElement)
        {
            if (RefWrapperMarshaller.IsApplicable(csElement))
                return RefWrappingMarshallers;
            if (EnumParameterWrapperMarshaller.IsApplicable(csElement))
                return Marshallers.Prepend(EnumWrappingMarshaller);
            return Marshallers;
        }

        public IRelationMarshaller GetRelationMarshaller(MarshallableRelation relation)
        {
            return RelationMarshallers[relation.GetType()];
        }
    }
}