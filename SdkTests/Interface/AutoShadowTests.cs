using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using SharpGen.Runtime;
using Xunit;

namespace Interface
{
    public class AutoShadowTests
    {   
        public AutoShadowTests()
        {
        }

        private static IDisposable SetupTests(bool supportExceptions, out CallbackInterfaceNative nativeView, out ManagedImplementation target)
        {
            target = supportExceptions ? new ExceptionEnabledManagedImplementation() : new ManagedImplementation();
            nativeView = new CallbackInterfaceNative(CppObject.ToCallbackPtr<CallbackInterface>(target));

            return new CompositeDisposable
            {
                target,
                nativeView
            };
        }

        [Fact]
        public void OutParameterCorrectlySet()
        {
            using (SetupTests(false, out var nativeView, out _))
            {
                nativeView.GetZero(out var zero);

                Assert.Equal(0, zero);
            }
        }

        [Fact]
        public void SimpleParameters()
        {
            using (SetupTests(false, out var nativeView, out _))
            {
                Assert.Equal(3, nativeView.Add(1, 2));
            }
        }

        [Fact]
        public void StringMarshalling()
        {
            using (SetupTests(false, out var nativeView, out _))
            {
                var str = "ABC";

                Assert.Equal('A', nativeView.GetFirstCharacter(str));
                Assert.Equal((byte)'A', nativeView.GetFirstAnsiCharacter(str));
            }
        }

        [Fact]
        public void RefParameter()
        {
            using (SetupTests(false, out var nativeView, out _))
            {
                var i = 4;

                nativeView.Increment(ref i);

                Assert.Equal(5, i);
            }
        }

        [Fact]
        public void LargeStruct()
        {
            using (SetupTests(false, out var nativeView, out _))
            {
                var result = nativeView.GetLargeStruct(4, 10);

                Assert.Equal(4, result.A);
                Assert.Equal(10, result.B);
            }
        }

        [Fact]
        public void MarshalledLargeStruct()
        {
            using (SetupTests(false, out var nativeView, out _))
            {
                var result = nativeView.GetLargeMarshalledStruct(3, 2, 1);

                Assert.Equal(3, result.I[0]);
                Assert.Equal(2, result.I[1]);
                Assert.Equal(1, result.I[2]);
            }
        }

        [Fact]
        public void MappedType()
        {
            using (SetupTests(false, out var nativeView, out _))
            {
                Assert.Equal(20, nativeView.MappedTypeTest(20));
            }
        }

        [Fact]
        public void InInterfaceParameter()
        {
            using (SetupTests(false, out var nativeView, out _))
            {
                using (var test = new ManagedImplementation())
                {
                    Assert.True(nativeView.AreEqual(test));
                }
            }
        }

        [Fact]
        public void OutInterfaceParameters()
        {
            using (SetupTests(false, out var nativeView, out _))
            {
                nativeView.CloneInstance(out var test);
                using (test)
                {
                    Assert.Equal(1, test.Add(0, 1));
                }
            }
        }

        [Fact]
        public void ExceptionsOnResultReturningMethods()
        {
            using (SetupTests(false, out var nativeView, out var target))
            {
                target.ThrowExceptionInClone = true;
                Assert.Throws<SharpGen.Runtime.SharpGenException>(() => nativeView.CloneInstance(out var inst));
            }
        }

        [Fact]
        public void ExceptionsRethrownOnManagedSideWhenSupportIsImplemented()
        {
            using (SetupTests(true, out var nativeView, out var target))
            {
                target.ThrowExceptionInClone = true;
                Assert.Throws<InvalidOperationException>(() => nativeView.CloneInstance(out var inst));
            }
        }

        [Fact]
        public void ReturnMappings()
        {
            using (SetupTests(false, out var nativeView, out var target))
            {
                IntPtr val = new IntPtr(5);

                Assert.Equal(val, nativeView.ModifyPointer(val, MethodOperation.PassThrough));

                Assert.Equal(new IntPtr(6), nativeView.ModifyPointer(val, 0));
            }
        }

        class ExceptionEnabledManagedImplementation : ManagedImplementation, IExceptionCallback
        {
            public void RaiseException(Exception e)
            {
                throw e;
            }
        }

        class ManagedImplementation : CallbackBase, CallbackInterface
        {
            public bool ThrowExceptionInClone { get; set; }

            public int Add(int i, int j)
            {
                return i + j;
            }

            public bool AreEqual(CallbackInterface rhs)
            {
                return Add(1, 1) == rhs.Add(1, 1);
            }

            public void CloneInstance(out CallbackInterface @out)
            {
                if (ThrowExceptionInClone)
                {
                    throw new InvalidOperationException();
                }
                @out = new ManagedImplementation();
            }

            public byte GetFirstAnsiCharacter(string str)
            {
                return Encoding.ASCII.GetBytes(str)[0];
            }

            public char GetFirstCharacter(string str)
            {
                return str[0];
            }

            public LargeStructWithMarshalling GetLargeMarshalledStruct(long a, long b, long c)
            {
                var result = new LargeStructWithMarshalling();

                result.I[0] = a;
                result.I[1] = b;
                result.I[2] = c;
                return result;
            }

            public LargeStruct GetLargeStruct(long a, long b)
            {
                return new LargeStruct
                {
                    A = a,
                    B = b
                };
            }

            public void GetZero(out int valueOut)
            {
                valueOut = 0;
            }

            public void Increment(ref int valueRef)
            {
                valueRef += 1;
            }

            public int MappedTypeTest(uint i)
            {
                return (int)i;
            }

            public IntPtr ModifyPointer(IntPtr ptr, MethodOperation op)
            {
                if (op != MethodOperation.PassThrough)
                {
                    return IntPtr.Add(ptr, 1);
                }
                return ptr;
            }

            public bool ArrayRelationAnd(bool[] arr)
            {
                return arr.Aggregate(true, (agg, val) => agg && val);
            }

            public int ArrayRelationSum(int[] arr)
            {
                return arr.Sum();
            }

            public long ArrayRelationSumStruct(LargeStructWithMarshalling[] arr)
            {
                return arr.SelectMany(x => x.I).Sum();
            }
        }
    }
}
