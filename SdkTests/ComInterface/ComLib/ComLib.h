#define COMLIB_API __declspec(dllexport)

#include <Windows.h>

struct MyValue {
	int I;
	double J;
};

struct IInterface
{
	virtual MyValue __stdcall GetValue() = 0;
};

extern "C" COMLIB_API IInterface* __stdcall CreateInstance(void);
