
#include "InterfaceNative.h"

class Implementation : public IInterface2
{
public:
	virtual MyValue __stdcall GetValue() override
	{
		return MyValue{ 1, 3.0 };
	}

	virtual MyValue __stdcall GetValue2() override
	{
		return MyValue{ 1, 3.0 };
	}
};

extern "C" __declspec(dllexport) IInterface2 * __stdcall CreateInstance(void)
{
	return new Implementation();
}
