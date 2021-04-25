
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

typedef int RESULT;

struct InterfaceWithProperties
{
    virtual bool __stdcall IsTrue() = 0;
    virtual RESULT __stdcall IsTrueOutProp(bool* value) = 0;
    virtual int __stdcall GetValue() = 0;
    virtual void __stdcall SetValue(int value) = 0;
    virtual RESULT __stdcall GetValue2(int* value) = 0;
    virtual void __stdcall SetValue2(int value) = 0;

    virtual int __stdcall GetValuePersistent() = 0;
    virtual RESULT __stdcall GetValue2Persistent(int* value) = 0;

    virtual InterfaceWithProperties* __stdcall GetSelfPersistent() = 0;
    virtual RESULT __stdcall GetSelfOutPersistent(InterfaceWithProperties** child) = 0;
};

extern "C" __declspec(dllexport) IInterface2* __stdcall CreateInstance(void);

extern "C" __declspec(dllexport) IInterface* __stdcall CreateInstance2(int i, double j);

extern "C" __declspec(dllexport) bool __stdcall CloneInstance(IInterface* iface, IInterface** cloned);

extern "C" __declspec(dllexport) InterfaceWithProperties* CreatePropertyTest(bool isTrue, int value, int value2);

struct LargeStruct
{
    long long a;
    long long b;
};

struct LargeStructWithMarshalling
{
    long long i[3];
};

enum MethodOperation : unsigned int
{
    PassThrough = 1u
};

struct CallbackInterface
{
    virtual void GetZero(int* ppValue) = 0;
    virtual void Increment(int* pValue) = 0;
    virtual LargeStruct GetLargeStruct(long long a, long long b) = 0;
    virtual LargeStructWithMarshalling GetLargeMarshalledStruct(long long a, long long b, long long c) = 0;
    virtual wchar_t GetFirstCharacter(wchar_t* str) = 0;
    virtual char GetFirstAnsiCharacter(char* str) = 0;
    virtual RESULT CloneInstance(CallbackInterface** out) = 0;
    virtual bool AreEqual(CallbackInterface* rhs) = 0;
    virtual int Add(int i, int j) = 0;
    virtual int MappedTypeTest(int i) = 0;
    virtual RESULT ModifyPointer(void* ptr, MethodOperation op, void** out) = 0;
    virtual bool ArrayRelationAnd(bool array[], int length) = 0;
    virtual int ArrayRelationSum(int array[], int length) = 0;
    virtual long long ArrayRelationSumStruct(LargeStructWithMarshalling array[], int length) = 0;
};

struct FastOutInterface {
    virtual void DoNothing() = 0;
};

extern "C" __declspec(dllexport) void FastOutInterfaceTest(FastOutInterface** out);

struct PassThroughMethodTest {
    virtual size_t PassThrough(size_t value) = 0;
    virtual long PassThroughLong(long value) = 0;
};

extern "C" __declspec(dllexport) PassThroughMethodTest* GetPassThroughMethodTest();

#define HRESULT unsigned int
#define STDMETHODCALLTYPE __stdcall
#define LARGE_INTEGER long long
#define ULARGE_INTEGER unsigned long long
#define ULONG unsigned long
#define REFIID void*
#define STATSTG void
#define DWORD unsigned int
struct IStream {
    virtual HRESULT STDMETHODCALLTYPE QueryInterface(
        /* [in] */ REFIID riid,
        /* [iid_is][out] */ void * *ppvObject) = 0;
    virtual ULONG STDMETHODCALLTYPE AddRef(void) = 0;
    virtual ULONG STDMETHODCALLTYPE Release(void) = 0;
    virtual /* [local] */ HRESULT STDMETHODCALLTYPE Read(
        /* [annotation] */ void *pv,
        /* [annotation][in] */ ULONG cb,
        /* [annotation] */ ULONG *pcbRead) = 0;
    virtual /* [local] */ HRESULT STDMETHODCALLTYPE Write(
        /* [annotation] */ const void *pv,
        /* [annotation][in] */ ULONG cb,
        /* [annotation] */ ULONG *pcbWritten) = 0;
    virtual /* [local] */ HRESULT STDMETHODCALLTYPE Seek(
        /* [in] */ LARGE_INTEGER dlibMove,
        /* [in] */ DWORD dwOrigin,
        /* [annotation] */ ULARGE_INTEGER *plibNewPosition) = 0;
    virtual HRESULT STDMETHODCALLTYPE SetSize(
        /* [in] */ ULARGE_INTEGER libNewSize) = 0;
    virtual /* [local] */ HRESULT STDMETHODCALLTYPE CopyTo(
        /* [annotation][unique][in] */ IStream *pstm,
        /* [in] */ ULARGE_INTEGER cb,
        /* [annotation] */ ULARGE_INTEGER *pcbRead,
        /* [annotation] */ ULARGE_INTEGER *pcbWritten) = 0;
    virtual HRESULT STDMETHODCALLTYPE Commit(
        /* [in] */ DWORD grfCommitFlags) = 0;
    virtual HRESULT STDMETHODCALLTYPE Revert(void) = 0;
    virtual HRESULT STDMETHODCALLTYPE LockRegion(
        /* [in] */ ULARGE_INTEGER libOffset,
        /* [in] */ ULARGE_INTEGER cb,
        /* [in] */ DWORD dwLockType) = 0;
    virtual HRESULT STDMETHODCALLTYPE UnlockRegion(
        /* [in] */ ULARGE_INTEGER libOffset,
        /* [in] */ ULARGE_INTEGER cb,
        /* [in] */ DWORD dwLockType) = 0;
    virtual HRESULT STDMETHODCALLTYPE Stat(
        /* [out] */ STATSTG *pstatstg,
        /* [in] */ DWORD grfStatFlag) = 0;
    virtual HRESULT STDMETHODCALLTYPE Clone(
        /* [out] */ IStream **ppstm) = 0;
};
