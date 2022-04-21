using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SharpGen.Runtime;
using Xunit;

namespace SharpGen.UnitTests.Runtime;

public class VtblTests
{
    [Fact]
    public void CanRoundTripCallThroughNativeVtblToManagedObject()
    {
        using var callback = new CallbackImpl();

        var callbackPtr = MarshallingHelpers.ToCallbackPtr<ICallback>(callback);
        Assert.NotEqual(IntPtr.Zero, callbackPtr);

        var methodPtr = Marshal.ReadIntPtr(Marshal.ReadIntPtr(callbackPtr));
        var delegateObject = Marshal.GetDelegateForFunctionPointer<CallbackShadow.CallbackVbtl.IncrementDelegate>(methodPtr);
        Assert.Equal(3, delegateObject(callbackPtr, 2));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void VtblCausesNoOutOfBounds(uint method)
    {
        OutOfBoundsVbtl vtbl = new(method);
        Assert.NotEqual(IntPtr.Zero, vtbl.Pointer);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void VtblCausesOutOfBounds(uint method)
    {
        void Func()
        {
            OutOfBoundsVbtl vtbl = new(method);
            Assert.NotEqual(IntPtr.Zero, vtbl.Pointer);
        }

        Assert.Throws<IndexOutOfRangeException>(Func);
    }

    [DebuggerTypeProxy(typeof(CppObjectVtblDebugView))]
    private sealed class OutOfBoundsVbtl : CppObjectVtbl
    {
        public unsafe OutOfBoundsVbtl(uint outOfBounds) : base(1)
        {
            var @delegate = new IncrementDelegate(IncrementImpl);
            var fnPtr = (delegate* managed<IntPtr, int, int>)&IncrementImpl;
#pragma warning disable 618
            AddMethod(@delegate, 0);

            switch (outOfBounds)
            {
                case 1:
                    AddMethod(@delegate, 1);
                    break;
                case 2:
                    AddMethod(@delegate, 42);
                    break;
                case 3:
                    AddMethod(fnPtr, 42u);
                    break;
                case 4:
                    AddMethod(fnPtr, 0u);
                    break;
            }
#pragma warning restore 618
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int IncrementDelegate(IntPtr thisObj, int param);

        private static int IncrementImpl(IntPtr thisObj, int param)
        {
            return ToCallback<ICallback>(thisObj).Increment(param);
        }
    }
}