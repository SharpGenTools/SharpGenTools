// The following ifdef block is the standard way of creating macros which make exporting 
// from a DLL simpler. All files within this DLL are compiled with the STRUCTLIB_EXPORTS
// symbol defined on the command line. This symbol should not be defined on any project
// that uses this DLL. This way any other project whose source files include this file see 
// STRUCTLIB_API functions as being imported from a DLL, whereas this DLL sees symbols
// defined with this macro as being exported.
#define STRUCTLIB_API __declspec(dllexport)
#define STRUCTLIB_FUNC(RET) extern "C" STRUCTLIB_API RET __stdcall

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