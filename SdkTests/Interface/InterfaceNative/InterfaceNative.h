
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
};

extern "C" __declspec(dllexport) IInterface2* __stdcall CreateInstance(void);

extern "C" __declspec(dllexport) IInterface* __stdcall CreateInstance2(int i, double j);

extern "C" __declspec(dllexport) bool __stdcall CloneInstance(IInterface* interface, IInterface** cloned);
