using System;
using System.Linq;
using SharpGen.Runtime;
using Xunit;

namespace Functions
{
    public class RelationTests
    {
        [Fact]
        public void ValueType()
        {
            var test = new []
            {
                new SimpleStruct {I = 1},
                new SimpleStruct {I = 2},
                new SimpleStruct {I = 3}
            };
            
            Assert.Equal(6, NativeFunctions.Sum(test));
        }
        
        [Fact]
        public void OutBoolArray()
        {
            var array = new bool[5];
            NativeFunctions.InitBoolArray(array);
            Assert.All(array, x => Assert.True(x));
        }

        [Fact]
        public void InterfaceOutArrays()
        {
            int num = 3;
            Interface[] results = new Interface[num];
            NativeFunctions.GetInterfacesWithRelation(results);
            foreach (var result in results)
            {
                result.Method();
            }
        }

        [Fact]
        public void InterfaceInArrays()
        {
            Interface[] results = new Interface[4];
            NativeFunctions.GetInterfacesWithRelation(results);
            NativeFunctions.InInterfaceArray(results);
        }

        [Fact]
        public void StructWithMarshallingArrays()
        {
            int seed = 37;
            var rng = new Random(seed);
            StructWithMarshal[] array = new StructWithMarshal[5];
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array[i].I.Length; j++)
                {
                    array[i].I[j] = rng.Next();
                }
            }

            int result = NativeFunctions.SumStructWithMarshal(array);
            Assert.Equal(array.SelectMany(x => x.I).Aggregate((x, a) => x + a), result);
        }

        [Fact]
        public void ReservedParameter()
        {
            Assert.True(NativeFunctions.VerifyReservedParam());
        }
    }
}