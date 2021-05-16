using System;
using SharpGen.Config;
using SharpGen.Model;

namespace SharpGen.Transform
{
    internal sealed class MethodOverloadBuilder
    {
        private readonly Ioc ioc;

        public MethodOverloadBuilder(Ioc ioc)
        {
            this.ioc = ioc ?? throw new ArgumentNullException(nameof(ioc));
        }

        private GlobalNamespaceProvider GlobalNamespace => ioc.GlobalNamespace;

        public CsMethod CreateInterfaceArrayOverload(CsMethod original)
        {
            // Create a new method and transforms all array of CppObject to InterfaceArray<CppObject>
            var newMethod = (CsMethod)original.Clone();
            foreach (var csParameter in newMethod.PublicParameters)
            {
                if (!csParameter.IsInInterfaceArrayLike)
                    continue;

                csParameter.PublicType = new CsInterfaceArray(
                    (CsInterface) csParameter.PublicType,
                    GlobalNamespace.GetTypeName(WellKnownName.InterfaceArray)
                );

                csParameter.MarshalType = TypeRegistry.IntPtr;
            }
            return newMethod;
        }

        public CsMethod CreateRawPtrOverload(CsMethod original)
        {
            // Create private method with raw pointers for arrays, with all arrays as pure IntPtr
            // In order to be able to generate method taking single element
            var rawMethod = (CsMethod)original.Clone();
            rawMethod.Visibility = Visibility.Private;
            foreach (var csSubParameter in rawMethod.PublicParameters)
            {
                if (csSubParameter.IsArray || csSubParameter.IsInterface || csSubParameter.HasPointer)
                {
                    csSubParameter.PublicType = csSubParameter.MarshalType = TypeRegistry.IntPtr;
                    csSubParameter.IsArray = false;
                    csSubParameter.Attribute = CsParameterAttribute.In;
                }
            }
            return rawMethod;
        }
    }
}
