using SharpGen.Config;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGen.Transform
{
    class MethodOverloadBuilder
    {
        private readonly TypeRegistry typeRegistry;
        private readonly GlobalNamespaceProvider globalNamespace;

        public MethodOverloadBuilder(GlobalNamespaceProvider globalNamespace, TypeRegistry typeRegistry)
        {
            this.globalNamespace = globalNamespace;
            this.typeRegistry = typeRegistry;
        }

        public CsMethod CreateInterfaceArrayOverload(CsMethod original)
        {
            // Create a new method and transforms all array of CppObject to InterfaceArray<CppObject>
            var newMethod = (CsMethod)original.Clone();
            foreach (var csSubParameter in newMethod.Parameters)
            {
                if (csSubParameter.IsInInterfaceArrayLike)
                {
                    csSubParameter.PublicType = new CsInterfaceArray((CsInterface)csSubParameter.PublicType, globalNamespace.GetTypeName(WellKnownName.InterfaceArray));
                    csSubParameter.MarshalType = typeRegistry.ImportType(typeof(IntPtr));
                }
            }
            return newMethod;
        }

        public CsMethod CreateRawPtrOverload(CsMethod original)
        {
            // Create private method with raw pointers for arrays, with all arrays as pure IntPtr
            // In order to be able to generate method taking single element
            var rawMethod = (CsMethod)original.Clone();
            rawMethod.Visibility = Visibility.Private;
            foreach (var csSubParameter in rawMethod.Parameters)
            {
                if (csSubParameter.IsArray || csSubParameter.IsInterface || csSubParameter.HasPointer)
                {
                    csSubParameter.PublicType = typeRegistry.ImportType(typeof(IntPtr));
                    csSubParameter.MarshalType = typeRegistry.ImportType(typeof(IntPtr));
                    csSubParameter.IsArray = false;
                    csSubParameter.Attribute = CsParameterAttribute.In;
                }
            }
            return rawMethod;
        }
    }
}
