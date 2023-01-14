using System;
using System.Runtime.InteropServices;

namespace SharpGen.Runtime
{
    public static class ComUtilities
    {
        static ComUtilities() => ComActivationHelpers.CoInitialize();

        public static void CreateComInstance(Guid classGuid, ComContext context, Guid interfaceGuid, ComObject comObject)
        {
            if (comObject == null) throw new ArgumentNullException(nameof(comObject));

            var result = ComActivationHelpers.CreateComInstance(classGuid, context, interfaceGuid, out var pointer);
            result.CheckError();
            comObject.NativePointer = pointer;
        }

        public static bool TryCreateComInstance(Guid classGuid, ComContext context, Guid interfaceGuid, ComObject comObject)
        {
            if (comObject == null) throw new ArgumentNullException(nameof(comObject));

            var result = ComActivationHelpers.CreateComInstance(classGuid, context, interfaceGuid, out var pointer);
            comObject.NativePointer = pointer;
            return result.Success;
        }
    }
}
