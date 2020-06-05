using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;

namespace SharpGen.Runtime
{
    public static partial class PlatformDetection
    {
        private const int ERROR_NO_TOKEN = unchecked((int) 0x800703F0);

        private static SafeAccessTokenHandle GetCurrentToken(out int hr)
        {
            hr = 0;
            var success = OpenThreadToken(out var safeTokenHandle);

            if (!success)
                hr = Marshal.GetHRForLastWin32Error();

            if (!success && hr == ERROR_NO_TOKEN)
                // No impersonation
                safeTokenHandle = GetCurrentProcessToken(out hr);

            return safeTokenHandle;
        }

        [DllImport(Advapi32, SetLastError = true)]
        private static extern bool OpenThreadToken(IntPtr threadHandle, TokenAccessLevels dwDesiredAccess,
                                                   bool bOpenAsSelf, out SafeAccessTokenHandle phThreadToken);

        [DllImport(Kernel32)]
        private static extern IntPtr GetCurrentThread();

        private static bool OpenThreadToken(out SafeAccessTokenHandle tokenHandle) =>
            OpenThreadToken(GetCurrentThread(), TokenAccessLevels.Query, true, out tokenHandle) ||
            OpenThreadToken(GetCurrentThread(), TokenAccessLevels.Query, false, out tokenHandle);

        [DllImport(Advapi32, SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr processToken, TokenAccessLevels desiredAccess,
                                                    out SafeAccessTokenHandle tokenHandle);

        [DllImport(Kernel32)]
        private static extern IntPtr GetCurrentProcess();

        [DllImport(Kernel32, SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);

        private static SafeAccessTokenHandle GetCurrentProcessToken(out int hr)
        {
            hr = 0;
            if (!OpenProcessToken(GetCurrentProcess(), TokenAccessLevels.Query, out var safeTokenHandle))
                hr = Marshal.GetHRForLastWin32Error();
            return safeTokenHandle;
        }

        [DllImport(Advapi32, CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool GetTokenInformation(SafeAccessTokenHandle tokenHandle,
                                                       TOKEN_INFORMATION_CLASS tokenInformationClass,
                                                       IntPtr tokenInformation,
                                                       uint tokenInformationLength,
                                                       out uint returnLength);

        private static unsafe bool HasAppContainerToken()
        {
            var dwIsAppContainerPtr = stackalloc int[1];

            using var safeTokenHandle = GetCurrentToken(out var hr);

            if (safeTokenHandle == null || safeTokenHandle.IsInvalid)
            {
#if NETSTANDARD1_1
                throw new SecurityException($"{nameof(HasAppContainerToken)}: Win32 HRESULT 0x{hr:X8}");
#else
                throw new SecurityException(new Win32Exception(hr).Message);
#endif
            }

            if (!GetTokenInformation(safeTokenHandle, TOKEN_INFORMATION_CLASS.TokenIsAppContainer,
                                     new IntPtr(dwIsAppContainerPtr), sizeof(int), out _))
            {
                hr = Marshal.GetHRForLastWin32Error();
#if NETSTANDARD1_1
                throw new SecurityException($"{nameof(HasAppContainerToken)}: Win32 HRESULT 0x{hr:X8}");
#else
                throw new Win32Exception(new Win32Exception(hr).Message);
#endif
            }

            return *dwIsAppContainerPtr != 0;
        }

        [Flags]
        private enum TokenAccessLevels
        {
            Query = 0x00000008
        }

        private sealed class SafeAccessTokenHandle : SafeHandle
        {
            private SafeAccessTokenHandle() : base(IntPtr.Zero, true)
            {
            }

            // 0 is an Invalid Handle
            public SafeAccessTokenHandle(IntPtr handle) : base(handle, true)
            {
            }

            public override bool IsInvalid => handle == IntPtr.Zero || handle == new IntPtr(-1);

            protected override bool ReleaseHandle() => CloseHandle(handle);
        }

        private enum TOKEN_INFORMATION_CLASS : uint
        {
            TokenUser = 1,
            TokenGroups,
            TokenPrivileges,
            TokenOwner,
            TokenPrimaryGroup,
            TokenDefaultDacl,
            TokenSource,
            TokenType,
            TokenImpersonationLevel,
            TokenStatistics,
            TokenRestrictedSids,
            TokenSessionId,
            TokenGroupsAndPrivileges,
            TokenSessionReference,
            TokenSandBoxInert,
            TokenAuditPolicy,
            TokenOrigin,
            TokenElevationType,
            TokenLinkedToken,
            TokenElevation,
            TokenHasRestrictions,
            TokenAccessInformation,
            TokenVirtualizationAllowed,
            TokenVirtualizationEnabled,
            TokenIntegrityLevel,
            TokenUIAccess,
            TokenMandatoryPolicy,
            TokenLogonSid,
            TokenIsAppContainer,
            TokenCapabilities,
            TokenAppContainerSid,
            TokenAppContainerNumber,
            TokenUserClaimAttributes,
            TokenDeviceClaimAttributes,
            TokenRestrictedUserClaimAttributes,
            TokenRestrictedDeviceClaimAttributes,
            TokenDeviceGroups,
            TokenRestrictedDeviceGroups,
            TokenSecurityAttributes,
            TokenIsRestricted,
            MaxTokenInfoClass
        }
    }
}