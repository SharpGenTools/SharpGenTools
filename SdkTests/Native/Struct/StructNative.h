#define STRUCTLIB_API __declspec(dllexport)
#define STRUCTLIB_FUNC(RET) extern "C" __declspec(dllexport) RET __stdcall

STRUCTLIB_API struct SimpleStruct
{
	int i;
	int j;
};

STRUCTLIB_API struct IntToBoolArray
{
	int i[3];
};

STRUCTLIB_FUNC(SimpleStruct) GetSimpleStruct();

STRUCTLIB_FUNC(IntToBoolArray) GetIntToBoolArray();

STRUCTLIB_FUNC(bool) And(IntToBoolArray array);