
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

	void __stdcall AddToThis(IInterface2* interfaces[], int numInstances) override
	{
		for (int i = 0; i < numInstances; ++i)
		{
			value.I += interfaces[i]->GetValue2().I;
			value.J += interfaces[i]->GetValue2().J;
		}
	}
};

class PropertyImplementation : public InterfaceWithProperties
{
	bool isTrue;
	int value;
	int value2;

public:
	PropertyImplementation(bool isTrue, int value, int value2)
		: isTrue(isTrue), value(value), value2(value2)
	{}

	virtual bool __stdcall IsTrue() override
	{
		return isTrue;
	}
	virtual RESULT __stdcall IsTrueOutProp(bool* value)
	{
		*value = isTrue;
		return 0;
	}

	virtual int __stdcall GetValue()
	{
		return value;
	}

	virtual void __stdcall SetValue(int value)
	{
		this->value = value;
	}
	virtual RESULT __stdcall GetValue2(int* value)
	{
		*value = value2;
		return 0;
	}
	virtual void __stdcall SetValue2(int value)
	{
		value2 = value;
	}

	virtual int __stdcall GetValuePersistent()
	{
		return value;
	}

	virtual RESULT __stdcall GetValue2Persistent(int* value)
	{
		*value = value2;
		return 0;
	}

	virtual InterfaceWithProperties* __stdcall GetSelfPersistent()
	{
		return this;
	}

	virtual RESULT __stdcall GetSelfOutPersistent(InterfaceWithProperties** self)
	{
		*self = this;
		return 0;
	}
};

class FastOutInterfaceImplementation : public FastOutInterface
{
	virtual void DoNothing() {}
};

struct PassThroughMethodTestImpl: public PassThroughMethodTest
{
	virtual size_t PassThrough(size_t test)
	{
		return test;
	}

	virtual long PassThroughLong(long test)
	{
		return test;
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

extern "C" __declspec(dllexport) InterfaceWithProperties* CreatePropertyTest(bool isTrue, int value, int value2)
{
	return new PropertyImplementation(isTrue, value, value2);
}


extern "C" __declspec(dllexport) void FastOutInterfaceTest(FastOutInterface** out)
{
	*out = new FastOutInterfaceImplementation();
}

extern "C" __declspec(dllexport) PassThroughMethodTest* GetPassThroughMethodTest()
{
	return new PassThroughMethodTestImpl();
}
