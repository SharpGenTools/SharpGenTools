namespace SharpGen.Runtime;

public readonly partial struct Result
{
    /// <unmanaged>S_OK</unmanaged>
    public static readonly Result Ok = new(0);

    /// <unmanaged>S_FALSE</unmanaged>
    public static readonly Result False = new(1);

    /// <unmanaged>E_ABORT</unmanaged>
    public static readonly Result Abort = new(0x80004004);

    /// <unmanaged>E_ACCESSDENIED</unmanaged>
    public static readonly Result AccessDenied = new(0x80070005);

    /// <unmanaged>E_FAIL</unmanaged>
    public static readonly Result Fail = new(0x80004005);

    /// <unmanaged>E_HANDLE</unmanaged>
    public static readonly Result Handle = new(0x80070006);

    /// <unmanaged>E_INVALIDARG</unmanaged>
    public static readonly Result InvalidArg = new(0x80070057);

    /// <unmanaged>E_NOINTERFACE</unmanaged>
    public static readonly Result NoInterface = new(0x80004002);

    /// <unmanaged>E_NOTIMPL</unmanaged>
    public static readonly Result NotImplemented = new(0x80004001);

    /// <unmanaged>E_OUTOFMEMORY</unmanaged>
    public static readonly Result OutOfMemory = new(0x8007000E);

    /// <unmanaged>E_POINTER</unmanaged>
    public static readonly Result InvalidPointer = new(0x80004003);

    /// <unmanaged>E_UNEXPECTED</unmanaged>
    public static readonly Result UnexpectedFailure = new(0x8000FFFF);

    /// <unmanaged>WAIT_ABANDONED</unmanaged>
    public static readonly Result WaitAbandoned = new(0x00000080);

    /// <unmanaged>WAIT_TIMEOUT</unmanaged>
    public static readonly Result WaitTimeout = new(0x00000102);

    /// <summary>
    /// The data necessary to complete this operation is not yet available.
    /// </summary>
    /// <unmanaged>E_PENDING</unmanaged>
    public static readonly Result Pending = new(0x8000000A);

    /// <summary>
    /// The data area passed to a system call is too small.
    /// </summary>
    /// <unmanaged>E_NOT_SUFFICIENT_BUFFER</unmanaged>
    public static readonly Result InsufficientBuffer = new(0x8007007A);
}