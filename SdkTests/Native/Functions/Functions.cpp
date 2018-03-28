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

DECL(void) BoolToIntTest(int in, int* out)
{
    *out = in;
}

DECL(void) StructMarshalling(StructWithMarshal in, StructWithStaticMarshal inStatic, StructWithMarshal* out, StructWithStaticMarshal* outStatic)
{
    *out = in;
    *outStatic = inStatic;
}

DECL(int) SumOfLastElements(StructWithMarshal in[1], StructWithStaticMarshal inStatic[1])
{
    return in[0].i[2] + inStatic[0].i[2];
}

DECL(void) FastOutTest(Interface** out)
{
    *out = new Implementation();
}

DECL(MyEnum) PassThroughEnum(MyEnum testEnum)
{
    return testEnum;
}