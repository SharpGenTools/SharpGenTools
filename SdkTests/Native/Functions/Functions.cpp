#include "Functions.h"

class Implementation : public Interface
{
    void Method() {}
};

DECL(void) GetInterfaces(int numInstances, Interface** results)
{
    for (int i = 0; i < numInstances; ++i)
    {
        results[i] = new Implementation();
    }
}

DECL(void) GetInterfacesOptional(int numInstances, Interface** results)
{
    if (results != nullptr)
    {
        GetInterfaces(numInstances, results);
    }
}

DECL(void) GetIntArray(int numInts, int* results)
{
    for (int i = 0; i < numInts; ++i)
    {
        results[i] = i;
    }
}

DECL(wchar_t) GetFirstCharacter(wchar_t* string)
{
    return string[0];
}

DECL(char) GetFirstAnsiCharacter(char* string)
{
    return string[0];
}

DECL(void) BoolToIntTest(int in, int* out)
{
    *out = in;
}

DECL(void) BoolArrayTest(bool* in, bool* out, int numElements)
{
    for (int i = 0; i < numElements; ++i)
    {
        out[i] = in[i];
    }
}

DECL(void) StructMarshalling(StructWithMarshal in, StructWithStaticMarshal inStatic, StructWithMarshal* out, StructWithStaticMarshal* outStatic)
{
    *out = in;
    *outStatic = inStatic;
}

DECL(void) StructArrayMarshalling(StructWithMarshal in[1], StructWithStaticMarshal inStatic[1], StructWithMarshal out[1], StructWithStaticMarshal outStatic[1])
{
    out[0] = in[0];
    outStatic[0] = inStatic[0];
}

DECL(void) SetAllElements(StructWithMarshal* ref)
{
    ref->i[0] = 10;
    ref->i[1] = 10;
    ref->i[2] = 10;
}

DECL(int) FirstElementOrZero(StructWithMarshal* ref)
{
    if (ref != nullptr)
    {
        return ref->i[0];
    }
    return 0;
}

DECL(void) FastOutTest(Interface** out)
{
    *out = new Implementation();
}

DECL(MyEnum) PassThroughEnum(MyEnum testEnum)
{
    return testEnum;
}

DECL(void) Increment(int* cell)
{
    (*cell)++;
}

DECL(int) Add(int* lhs, int* rhs_opt)
{
    if (rhs_opt != nullptr)
    {
        return *lhs + *rhs_opt;
    }
    return *lhs;
}

DECL(const char*) GetName()
{
    return "Functions";
}


DECL(int) Sum(int numElements, SimpleStruct elements[])
{
    int sum = 0;
    if (elements == nullptr)
    {
        return sum;
    }
    
    for (int i = 0; i < numElements; ++i)
    {
        sum += elements[i].i;
    }
    return sum;
}


DECL(int) Product(int numElements, SimpleStruct elements[])
{
    int product = 1;
    for (int i = 0; i < numElements; ++i)
    {
        product *= elements[i].i;
    }
    return product;
}

DECL(long long) SumValues(LargeStruct val)
{
    return val.i[0] + val.i[1] + val.i[2];
}

DECL(PointerSize) PassThroughPointerSize(PointerSize param)
{
    return param;
}

DECL(void) StructArrayOut(StructWithMarshal in, StructWithMarshal out[])
{
    if (out != nullptr)
    {
        out[0] = in;
    }
}

DECL(int) SumInner(StructAsClass test[], int length)
{
    int sum = 0;
    for (int i = 0; i < length; i++)
    {
        sum += test[i].i;
    }
    return sum;
}

DECL(void) AddOne(SimpleStruct* param)
{
    if(param != nullptr) param->i++;
}

DECL(void) EnumOut(MyEnum *enumOut)
{
    *enumOut = TestValue;
}

DECL(MyEnum) FirstEnumElement(MyEnum test[])
{
    return test[0];
}

DECL(int) ArrayRelationSum(int length, SimpleStruct elements[])
{
    return Sum(length, elements);
}

DECL(void) ArrayRelationOutInitBoolArray(bool out[], int length)
{
    for(size_t i = 0; i < length; i++)
    {
        out[i] = true;
    }
}

DECL(void) ArrayRelationOutGetInterfacesWithRelation(int length, Interface** array)
{
    GetInterfaces(length, array);
}

DECL(void) ArrayRelationInInterfaceArray(int length, Interface* array[])
{
    for(size_t i = 0; i < length; i++)
    {
        array[i]->Method();
    }
}

DECL(int) ArrayRelationSumStructWithMarshal(int length, StructWithMarshal array[])
{
    int sum = 0;
    for(size_t i = 0; i < length; i++)
    {
        for(size_t j = 0; j < 3; j++)
        {
            sum += array[i].i[j];
        }
    }
    return sum;
}

DECL(bool) VerifyReservedParam(int reserved)
{
    return reserved == 42;
}

DECL(StructAsClassWrapper) GetWrapper()
{
    return {{1}};
}
