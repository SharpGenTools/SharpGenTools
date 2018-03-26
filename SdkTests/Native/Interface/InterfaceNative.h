
struct GUID {
    unsigned long  Data1;
    unsigned short Data2;
    unsigned short Data3;
    unsigned char  Data4[ 8 ];
};

struct MyValue
{
	int I;
	double J;
};

struct IInterface
{
	virtual MyValue __stdcall GetValue() = 0;
};

struct IInterface2 : public IInterface
{
	virtual MyValue __stdcall GetValue2() = 0;
	virtual void __stdcall AddToThis(IInterface2* interfaces[], int numInstances) = 0;
};

struct IInterfaceWithGuid
{
	virtual void __stdcall Method() = 0;
};

// {16410F4E-B4AB-4B33-B9A3-7FC8FA15F4F4}
extern "C" const GUID IID_IInterfaceWithGuid =
	{0x16410f4e, 0xb4ab, 0x4b33, {0xb9, 0xa3, 0x7f, 0xc8, 0xfa, 0x15, 0xf4, 0xf4}};

struct ILargeInterface
{
	virtual int __stdcall Method1() = 0;
	virtual int __stdcall Method2() = 0;
	virtual int __stdcall Method3() = 0;
};

extern "C" __declspec(dllexport) IInterface2* __stdcall CreateInstance(void);

extern "C" __declspec(dllexport) IInterface* __stdcall CreateInstance2(int i, double j);

extern "C" __declspec(dllexport) bool __stdcall CloneInstance(IInterface* iface, IInterface** cloned);
