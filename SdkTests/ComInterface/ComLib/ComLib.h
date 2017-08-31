// The following ifdef block is the standard way of creating macros which make exporting 
// from a DLL simpler. All files within this DLL are compiled with the COMLIB_EXPORTS
// symbol defined on the command line. This symbol should not be defined on any project
// that uses this DLL. This way any other project whose source files include this file see 
// COMLIB_API functions as being imported from a DLL, whereas this DLL sees symbols
// defined with this macro as being exported.
#ifdef COMLIB_EXPORTS
#define COMLIB_API __declspec(dllexport)
#else
#define COMLIB_API __declspec(dllexport)
#endif

#include <Windows.h>
#include <Unknwn.h>

struct MyValue {
	int I;
	double J;
};

struct COMLIB_API IComInterface : public IUnknown
{
	virtual MyValue __stdcall GetValue() = 0;
};

extern COMLIB_API int nComLib;

extern "C" COMLIB_API IComInterface* __stdcall CreateInstance(void);
