#define DECL(RetType) extern "C" __declspec(dllexport) RetType __stdcall

enum MyEnum {};

struct Interface
{
	virtual void Method() = 0;
};

struct StructWithMarshal
{
	int i[3];
};

struct StructWithStaticMarshal
{
	int i[3];
};

DECL(void) GetInterfaces(int numInstances, Interface** results);

DECL(void) GetIntArray(int numInts, int* results);

DECL(wchar_t) GetFirstCharacter(wchar_t* string);

DECL(void) BoolToIntTest(int in, int* out);

DECL(void) StructMarshalling(StructWithMarshal in, StructWithStaticMarshal inStatic, StructWithMarshal* out, StructWithStaticMarshal* outStatic);

DECL(void) StructArrayMarshalling(StructWithMarshal in[1], StructWithStaticMarshal inStatic[1], StructWithMarshal out[1], StructWithStaticMarshal outStatic[1]);

DECL(void) FastOutTest(Interface** out);

DECL(MyEnum) PassThroughEnum(MyEnum testEnum);
