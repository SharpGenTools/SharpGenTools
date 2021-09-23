using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SharpGen.Runtime
{
    [DebuggerTypeProxy(typeof(CppObjectVtblDebugView))]
    public abstract class CppObjectVtbl
    {
        /// <summary>
        /// Default Constructor.
        /// </summary>
        /// <param name="numberOfCallbackMethods">number of methods to allocate in the VTBL</param>
        protected CppObjectVtbl(int numberOfCallbackMethods)
        {
            // Allocate ptr to vtbl
            Pointer = Marshal.AllocHGlobal(IntPtr.Size * numberOfCallbackMethods);
            Count = (uint) numberOfCallbackMethods;
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
        [MethodImpl(Utilities.MethodAggressiveOptimization)]
        [Obsolete("Use uint overload."), EditorBrowsable(EditorBrowsableState.Advanced)]
        protected void AddMethod(Delegate method, int index) => AddMethod(method, (uint) index);

        /// <summary>
        /// Add a method supported by this interface. This method is typically called from inherited constructor.
        /// </summary>
        /// <param name="method">the managed delegate method</param>
        /// <param name="index">the index in the vtable for this method.</param>
        protected unsafe void AddMethod(Delegate method, uint index)
        {
            CheckIndex(index);

            ((IntPtr*) Pointer)[index] = Marshal.GetFunctionPointerForDelegate(method);

            // Marshal.GetFunctionPointerForDelegate doesn't create GC roots for the source delegates.
            GCHandle.Alloc(method);
        }

        /// <summary>
        /// Add a method supported by this interface. This method is typically called from inherited constructor.
        /// </summary>
        /// <param name="method">the unmanaged function pointer</param>
        /// <param name="index">the index in the vtable for this method.</param>
        protected unsafe void AddMethod(void* method, uint index)
        {
            CheckIndex(index);

            ((void**) Pointer)[index] = method;
        }

        [MethodImpl(Utilities.MethodAggressiveOptimization)]
        private void CheckIndex(uint index)
        {
            if (index < Count) return;
            throw new IndexOutOfRangeException(
                $"{GetType().Name}{{{nameof(Pointer)}={Utilities.FormatPointer(Pointer)}, {nameof(Count)}={Count}}}[{index}]"
            );
        }

        [MethodImpl(Utilities.MethodAggressiveOptimization)]
        protected static T ToShadow<T>(IntPtr thisPtr) where T : CppObjectShadow =>
            CppObjectShadow.ToShadow<T>(thisPtr);

        [MethodImpl(Utilities.MethodAggressiveOptimization)]
        protected static TCallback ToCallback<TCallback>(IntPtr thisPtr) where TCallback : ICallbackable =>
            CppObjectShadow.ToCallback<TCallback>(thisPtr);

        public override string ToString() => $"{Utilities.FormatPointer(Pointer)} @ Count={Count}";

        private uint Count { get; }

        protected sealed class CppObjectVtblDebugView
        {
            private readonly CppObjectVtbl vtbl;

            public CppObjectVtblDebugView(CppObjectVtbl vtbl) => this.vtbl = vtbl;

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public unsafe FunctionPointerItem[] Items
            {
                get
                {
                    var vtblPointer = (IntPtr*) vtbl.Pointer;
                    var count = vtbl.Count;
                    var items = new FunctionPointerItem[count];

                    for (uint i = 0; i < count; i++)
                        items[i] = new FunctionPointerItem(vtblPointer[i]);

                    return items;
                }
            }

            public readonly struct FunctionPointerItem
            {
                private readonly string value;

                public FunctionPointerItem(IntPtr pointer) => value = Utilities.FormatPointer(pointer);

                public override string ToString() => value;
            }
        }
    }
}