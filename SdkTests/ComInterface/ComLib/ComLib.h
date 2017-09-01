#define COMLIB_API __declspec(dllexport)

#include <Windows.h>

struct MyValue {
	int I;
	double J;
};

struct IComInterface : public IUnknown
{
	virtual MyValue __stdcall GetValue() = 0;
};

extern "C" COMLIB_API IComInterface* __stdcall CreateInstance(void);
