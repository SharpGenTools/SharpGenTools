
#include "InterfaceNative.h"

class Implementation : public IInterface2
{
private:
	MyValue value;
public:
	Implementation()
	{
		value = MyValue{ 1, 3.0 };
	}

	Implementation(int intValue, double doubleValue)
	{
		value = {intValue, doubleValue};
	}

	virtual MyValue __stdcall GetValue() override
	{
		return value;
	}

	virtual MyValue __stdcall GetValue2() override
	{
		return value;
	}
};

extern "C" __declspec(dllexport) IInterface2 * __stdcall CreateInstance(void)
{
	return new Implementation();
}

extern "C" __declspec(dllexport) IInterface* __stdcall CreateInstance2(int i, double j)
{
	return new Implementation(i, j);
}


extern "C" __declspec(dllexport) bool __stdcall CloneInstance(IInterface* interface, IInterface** clonedInterface)
{
	auto value = interface->GetValue();
	*clonedInterface = new Implementation(value.I, value.J);
	return true;
}