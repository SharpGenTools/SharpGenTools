using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SharpGen.Runtime
{
    public class CppObjectVtbl
    {
        private readonly List<Delegate> methods;
        private readonly int vtblSize;

        /// <summary>
        /// Default Constructor.
        /// </summary>
        /// <param name="numberOfCallbackMethods">number of methods to allocate in the VTBL</param>
        public CppObjectVtbl(int numberOfCallbackMethods)
        {
            vtblSize = numberOfCallbackMethods;
            // Allocate ptr to vtbl
            Pointer = Marshal.AllocHGlobal(IntPtr.Size * numberOfCallbackMethods);
            methods = new List<Delegate>();
        }

        /// <summary>
        /// Gets the pointer to the vtbl.
        /// </summary>
        public IntPtr Pointer { get; private set; }

        /// <summary>
        /// Add a method supported by this interface. This method is typically called from inherited constructor.
        /// </summary>
        /// <param name="method">the managed delegate method</param>
        /// <param name="index">the index in the vtable for this method.</param>
        protected unsafe void AddMethod(Delegate method, int index)
        {
            if (index >= vtblSize)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "The supplied method index is outside the range of the allocated space for the vtable");
            }

            IntPtr* vtableIndex = (IntPtr*)Pointer + index;

            methods.Add(method);
            *vtableIndex = Marshal.GetFunctionPointerForDelegate(method);
        }

        /// <summary>
        /// Add a method supported by this interface at the current end of the vtable. This method is typically called from inherited constructor.
        /// </summary>
        /// <param name="method">the managed delegate method</param>
        [Obsolete("Use AddMethod(Delegate,int) to explicitly specify the index of the delegate in the vtable.")]
        protected unsafe void AddMethod(Delegate method)
        {
            AddMethod(method, methods.Count);
        }
    }
}