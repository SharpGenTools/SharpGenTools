#define STRUCTLIB_API __declspec(dllexport)
#define STRUCTLIB_FUNC(RET) extern "C" __declspec(dllexport) RET __stdcall

struct SimpleStruct
{
	int i;
	int j;
};

struct StructWithArray
{
	int i[3];
	double j;
};

union TestUnion
{
	int integer;
	float decimal;
};

union UnionWithArray
{
	unsigned long long bigInt;
	unsigned int parts[2];
};

struct BitField
{
	int firstBit : 1;
	int lastBits : 31;
};

struct AsciiTest
{
	char SmallString[10];
	char* LargeString;
};

struct Utf16Test
{
	wchar_t SmallString[10];
	wchar_t* LargeString;
};

struct NestedTest
{
	AsciiTest Ascii;
	Utf16Test Utf;
};

struct BitField2
{
	short lowerBits : 4;
	short reservedBits : 8;
	short upperBits: 4;
};

struct BoolToInt
{
	int test;
};

struct BoolToInt2
{
	int test;
};

struct BoolArray
{
	bool elements[3];
};

struct CustomNativeNew {};
struct CustomNativeNewNested
{
	CustomNativeNew Nested;
};

struct Interface
{
	virtual int One()=0;
};

struct StructWithInterface
{
	Interface* test;
};

struct PointerSizeMember
{
	size_t pointerSize;
};

struct StructSizeRelation
{
	int cbSize;
	int field1;
	int field2;
	long long field3;
};

struct ReservedRelation
{
	int field1;
	int field2;
	int reserved;
};

static_assert(sizeof(wchar_t) == 2, "Wide character isn't wide.");

STRUCTLIB_FUNC(SimpleStruct) GetSimpleStruct();

STRUCTLIB_FUNC(StructWithArray) PassThroughArray(StructWithArray param);

STRUCTLIB_FUNC(TestUnion) PassThroughUnion(TestUnion param);

STRUCTLIB_FUNC(UnionWithArray) PassThroughUnion2(UnionWithArray param);

STRUCTLIB_FUNC(BitField) PassThroughBitfield(BitField param);

STRUCTLIB_FUNC(AsciiTest) PassThroughAscii(AsciiTest param);

STRUCTLIB_FUNC(Utf16Test) PassThroughUtf(Utf16Test param);

STRUCTLIB_FUNC(NestedTest) PassThroughNested(NestedTest param);

STRUCTLIB_FUNC(BoolToInt2) PassThroughBoolToInt(BoolToInt2 param);

STRUCTLIB_FUNC(BoolArray) PassThroughBoolArray(BoolArray param);

STRUCTLIB_FUNC(bool) VerifyReservedBits(BitField2 param);

STRUCTLIB_FUNC(void) CustomNativeNewTest(CustomNativeNew param);

STRUCTLIB_FUNC(StructWithInterface) GetStructWithInterface();

STRUCTLIB_FUNC(StructWithInterface) PassThroughStructWithInterface(StructWithInterface param); 

STRUCTLIB_FUNC(PointerSizeMember) PassThroughPointerSizeMember(PointerSizeMember param);