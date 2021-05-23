namespace SharpGen.Runtime
{
    public readonly partial struct Result
    {
        private const string GeneralModule = "General";

        /// <unmanaged>S_OK</unmanaged>
        public static readonly Result Ok = new(0x00000000);

        /// <unmanaged>S_FALSE</unmanaged>
        public static readonly Result False = new(0x00000001);

        /// <unmanaged>E_ABORT</unmanaged>
        public static readonly ResultDescriptor Abort =
            new(new Result(0x80004004), GeneralModule, "E_ABORT", "Operation aborted");

        /// <unmanaged>E_ACCESSDENIED</unmanaged>
        public static readonly ResultDescriptor AccessDenied =
            new(new Result(0x80070005), GeneralModule, "E_ACCESSDENIED", "General access denied error");

        /// <unmanaged>E_FAIL</unmanaged>
        public static readonly ResultDescriptor Fail =
            new(new Result(0x80004005), GeneralModule, "E_FAIL", "Unspecified error");

        /// <unmanaged>E_HANDLE</unmanaged>
        public static readonly ResultDescriptor Handle =
            new(new Result(0x80070006), GeneralModule, "E_HANDLE", "Invalid handle");

        /// <unmanaged>E_INVALIDARG</unmanaged>
        public static readonly ResultDescriptor InvalidArg =
            new(new Result(0x80070057), GeneralModule, "E_INVALIDARG", "Invalid Arguments");

        /// <unmanaged>E_NOINTERFACE</unmanaged>
        public static readonly ResultDescriptor NoInterface =
            new(new Result(0x80004002), GeneralModule, "E_NOINTERFACE", "No such interface supported");

        /// <unmanaged>E_NOTIMPL</unmanaged>
        public static readonly ResultDescriptor NotImplemented =
            new(new Result(0x80004001), GeneralModule, "E_NOTIMPL", "Not implemented");

        /// <unmanaged>E_OUTOFMEMORY</unmanaged>
        public static readonly ResultDescriptor OutOfMemory =
            new(new Result(0x8007000E), GeneralModule, "E_OUTOFMEMORY", "Out of memory");

        /// <unmanaged>E_POINTER</unmanaged>
        public static readonly ResultDescriptor InvalidPointer =
            new(new Result(0x80004003), GeneralModule, "E_POINTER", "Invalid pointer");

        /// <unmanaged>E_UNEXPECTED</unmanaged>
        public static readonly ResultDescriptor UnexpectedFailure =
            new(new Result(0x8000FFFF), GeneralModule, "E_UNEXPECTED", "Catastrophic failure");

        /// <unmanaged>WAIT_ABANDONED</unmanaged>
        public static readonly ResultDescriptor WaitAbandoned =
            new(new Result(0x00000080), GeneralModule, "WAIT_ABANDONED", "WaitAbandoned");

        /// <unmanaged>WAIT_TIMEOUT</unmanaged>
        public static readonly ResultDescriptor WaitTimeout =
            new(new Result(0x00000102), GeneralModule, "WAIT_TIMEOUT", "WaitTimeout");

        /// <summary>
        /// The data necessary to complete this operation is not yet available.
        /// </summary>
        /// <unmanaged>E_PENDING</unmanaged>
        public static readonly ResultDescriptor Pending =
            new(new Result(0x8000000A), GeneralModule, "E_PENDING", "Pending");

        /// <summary>
        /// The data area passed to a system call is too small.
        /// </summary>
        /// <unmanaged>E_NOT_SUFFICIENT_BUFFER</unmanaged>
        public static readonly ResultDescriptor InsufficientBuffer =
            new(new Result(0x8007007A), GeneralModule, "E_NOT_SUFFICIENT_BUFFER", "Insufficient Buffer");
    }
}