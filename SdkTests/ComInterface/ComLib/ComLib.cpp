// ComLib.cpp : Defines the exported functions for the DLL application.
//

#include "ComLib.h"

class ComClass : public IInterface2
{
public:
	// Inherited via IComInterface
	virtual MyValue __stdcall GetValue() override
	{
		return MyValue{ 1, 3.0 };
	}

	virtual MyValue __stdcall GetValue2() override
	{
		return MyValue{ 1, 3.0 };
	}
};

extern "C" COMLIB_API IInterface2 * __stdcall CreateInstance(void)
{
	return new ComClass();
}
