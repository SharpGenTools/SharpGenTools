// ComLib.cpp : Defines the exported functions for the DLL application.
//

#include "ComLib.h"

class ComClass : public IComInterface
{
public:
	ComClass()
	{
		count = 1;
	}
private:
	int count;
public:
	// Inherited via ComInterface
	virtual HRESULT __stdcall QueryInterface(REFIID riid, void ** ppvObject) override
	{
		return E_NOTIMPL;
	}
	virtual ULONG __stdcall AddRef(void) override
	{
		return ++count;
	}
	virtual ULONG __stdcall Release(void) override
	{
		if (--count == 0)
		{
			delete this;
		}
		return count;
	}

	// Inherited via IComInterface
	virtual MyValue __stdcall GetValue() override
	{
		return MyValue{ 1, 3.0 };
	}
};

extern "C" COMLIB_API IComInterface * __stdcall CreateInstance(void)
{
	return new ComClass();
}
