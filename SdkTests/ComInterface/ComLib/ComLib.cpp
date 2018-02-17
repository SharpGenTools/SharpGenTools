// ComLib.cpp : Defines the exported functions for the DLL application.
//

#include "ComLib.h"

class ComClass : public IInterface
{
public:
	// Inherited via IComInterface
	virtual MyValue __stdcall GetValue() override
	{
		return MyValue{ 1, 3.0 };
	}
};

extern "C" COMLIB_API IInterface * __stdcall CreateInstance(void)
{
	return new ComClass();
}
