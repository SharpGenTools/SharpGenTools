using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SharpGen.Runtime
{
    [DebuggerTypeProxy(typeof(CppObjectVtblDebugView))]
    public class CppObjectVtbl
    {
        // We need to store the original delegate instances, because Marshal.GetFunctionPointerForDelegate
        // doesn't create GC roots for the source delegates.
        private readonly Delegate[] delegates;
        private uint vtblMaxIndex;

        /// <summary>
        /// Default Constructor.
        /// </summary>
        /// <param name="numberOfCallbackMethods">number of methods to allocate in the VTBL</param>
        public CppObjectVtbl(int numberOfCallbackMethods)
        {
            // Allocate ptr to vtbl
            Pointer = Marshal.AllocHGlobal(IntPtr.Size * numberOfCallbackMethods);
            delegates = new Delegate[numberOfCallbackMethods];
        }

        /// <summary>
        /// Gets the pointer to the vtbl.
        /// </summary>
        public IntPtr Pointer { get; }

        /// <summary>
        /// Add a method supported by this interface. This method is typically called from inherited constructor.
        /// </summary>
        /// <param name="method">the managed delegate method</param>
        /// <param name="index">the index in the vtable for this method.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void AddMethod(Delegate method, int index) => AddMethod(method, (uint) index);

        /// <summary>
        /// Add a method supported by this interface. This method is typically called from inherited constructor.
        /// </summary>
        /// <param name="method">the managed delegate method</param>
        /// <param name="index">the index in the vtable for this method.</param>
        protected unsafe void AddMethod(Delegate method, uint index)
        {
            if (index > vtblMaxIndex)
                vtblMaxIndex = index;

            delegates[index] = method;
            *((IntPtr*) Pointer + index) = Marshal.GetFunctionPointerForDelegate(method);
        }

        /// <summary>
        /// Add a method supported by this interface. This method is typically called from inherited constructor.
        /// </summary>
        /// <param name="method">the unmanaged function pointer</param>
        /// <param name="index">the index in the vtable for this method.</param>
        protected unsafe void AddMethod(void* method, uint index)
        {
            if (index > vtblMaxIndex)
                vtblMaxIndex = index;

            delegates[index] = null;
            *((void**) Pointer + index) = method;
        }

        /// <summary>
        /// Add a method supported by this interface at the current end of the vtable. This method is typically called from inherited constructor.
        /// </summary>
        /// <param name="method">the managed delegate method</param>
        [Obsolete("Use AddMethod(Delegate,int) to explicitly specify the index of the delegate in the vtable.")]
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        protected void AddMethod(Delegate method) => AddMethod(method, vtblMaxIndex + 1);

        protected static T ToShadow<T>(IntPtr thisPtr) where T : CppObjectShadow =>
            CppObjectShadow.ToShadow<T>(thisPtr);

        public override string ToString() => $"0x{Pointer.ToInt64():X} @ Count={delegates.Length}";

        protected sealed class CppObjectVtblDebugView
        {
            private readonly CppObjectVtbl vtbl;

            public CppObjectVtblDebugView(CppObjectVtbl vtbl) => this.vtbl = vtbl;

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public unsafe object[] Items
            {
                get
                {
                    var delegates = vtbl.delegates;
                    var vtblPointer = vtbl.Pointer;
                    var length = delegates.Length;
                    var items = new object[length];

                    for (uint i = 0; i < length; i++)
                        items[i] = delegates[i] ?? (object) new FunctionPointerItem(*((IntPtr*) vtblPointer + i));

                    return items;
                }
            }

            private readonly struct FunctionPointerItem
            {
                private readonly IntPtr pointer;

                public FunctionPointerItem(IntPtr pointer) => this.pointer = pointer;

                public override string ToString() => $"0x{pointer.ToInt64():X}";
            }
        }
    }
}