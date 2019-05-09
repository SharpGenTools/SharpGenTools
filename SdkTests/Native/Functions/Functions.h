
#define DECL(RetType) extern "C" __declspec(dllexport) RetType __stdcall

#define TestConstant 2018

enum MyEnum { TestValue = 1 };

struct Interface
{
	virtual void Method() = 0;
};

struct SimpleStruct
{
	int i;
};

struct StructWithMarshal
{
	int i[3];
};

struct StructWithStaticMarshal
{
	int i[3];
};

struct LargeStruct
{
	long long i[3];
};

struct PointerSize
{
	void* ptr;
};

struct StructAsClass
{
	int i;
};

struct StructAsClassWrapper
{
	StructAsClass wrapped;
};

DECL(void) GetInterfaces(int numInstances, Interface** results);

DECL(void) GetInterfacesOptional(int numInstances, Interface** results);

DECL(void) GetIntArray(int numInts, int* results);

DECL(wchar_t) GetFirstCharacter(wchar_t* string);

DECL(char) GetFirstAnsiCharacter(char* string);

DECL(void) BoolToIntTest(int in, int* out);

DECL(void) BoolArrayTest(bool* in, bool* out, int numElements);

DECL(void) StructMarshalling(StructWithMarshal in, StructWithStaticMarshal inStatic, StructWithMarshal* out, StructWithStaticMarshal* outStatic);

DECL(void) StructArrayMarshalling(StructWithMarshal in[1], StructWithStaticMarshal inStatic[1], StructWithMarshal out[1], StructWithStaticMarshal outStatic[1]);

DECL(void) SetAllElements(StructWithMarshal* ref);

DECL(int) FirstElementOrZero(StructWithMarshal* ref);

DECL(void) FastOutTest(Interface** out);

DECL(MyEnum) PassThroughEnum(MyEnum testEnum);

DECL(void) Increment(int* cell);

DECL(int) Add(int* lhs, int* rhs_opt);

DECL(const char*) GetName();

DECL(int) Sum(int numElements, SimpleStruct elements[]);

DECL(int) Product(int numElements, SimpleStruct elements[]);

DECL(long long) SumValues(LargeStruct val);

DECL(PointerSize) PassThroughPointerSize(PointerSize param);

DECL(void) StructArrayOut(StructWithMarshal in, StructWithMarshal out[]);

DECL(int) SumInner(StructAsClass test[], int length);

DECL(void) AddOne(SimpleStruct* param);

DECL(void) EnumOut(MyEnum* test);

DECL(MyEnum) FirstEnumElement(MyEnum test[]);

DECL(int) ArrayRelationSum(int length, SimpleStruct array[]);

DECL(void) ArrayRelationOutInitBoolArray(bool array[], int length);

DECL(void) ArrayRelationOutGetInterfacesWithRelation(int length, Interface** array);

DECL(void) ArrayRelationInInterfaceArray(int length, Interface* array[]);

DECL(int) ArrayRelationSumStructWithMarshal(int length, StructWithMarshal array[]);

DECL(bool) VerifyReservedParam(int reserved);

DECL(StructAsClassWrapper) GetWrapper();
