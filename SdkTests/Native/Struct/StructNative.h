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

STRUCTLIB_FUNC(SimpleStruct) GetSimpleStruct();

STRUCTLIB_FUNC(void) ForceMarshalTo(StructWithArray, TestUnion, BitField, AsciiTest, Utf16Test);
