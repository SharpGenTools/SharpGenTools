// Copyright (c) 2010-2014 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGen.Runtime;

/// <summary>
/// Descriptor used to provide detailed message for a particular <see cref="Result"/>.
/// </summary>
[SuppressMessage("ReSharper", "ConvertToAutoProperty")]
internal readonly struct ResultDescriptor : IEquatable<ResultDescriptor>
{
    private static readonly ConcurrentDictionary<int, ResultDescriptor> Descriptors = new();
    // .NET Native has issues with <...> in property backing fields in structs
    private readonly Result _result;
    private readonly string? _module;
    private readonly string? _nativeApiCode;
    private readonly string? _apiCode;
    private readonly string? _description;
    private const string UnknownText = "Unknown";

    static ResultDescriptor()
    {
        const string generalModule = "General";

        Register(Result.Abort, generalModule, "E_ABORT", "Operation aborted");
        Register(Result.AccessDenied, generalModule, "E_ACCESSDENIED", "General access denied error");
        Register(Result.Fail, generalModule, "E_FAIL", "Unspecified error");
        Register(Result.Handle, generalModule, "E_HANDLE", "Invalid handle");
        Register(Result.InvalidArg, generalModule, "E_INVALIDARG", "Invalid arguments");
        Register(Result.NoInterface, generalModule, "E_NOINTERFACE", "No such interface supported");
        Register(Result.NotImplemented, generalModule, "E_NOTIMPL", "Not implemented");
        Register(Result.OutOfMemory, generalModule, "E_OUTOFMEMORY", "Out of memory");
        Register(Result.InvalidPointer, generalModule, "E_POINTER", "Invalid pointer");
        Register(Result.UnexpectedFailure, generalModule, "E_UNEXPECTED", "Catastrophic failure");
        Register(Result.WaitAbandoned, generalModule, "WAIT_ABANDONED", "WaitAbandoned");
        Register(Result.WaitTimeout, generalModule, "WAIT_TIMEOUT", "WaitTimeout");
        Register(Result.Pending, generalModule, "E_PENDING", "Pending");
        Register(Result.InsufficientBuffer, generalModule, "E_NOT_SUFFICIENT_BUFFER", "Insufficient buffer");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResultDescriptor"/> class.
    /// </summary>
    /// <param name="code">The HRESULT error code.</param>
    /// <param name="module">The module (ex: SharpDX.Direct2D1).</param>
    /// <param name="apiCode">The API code (ex: D2D1_ERR_...).</param>
    /// <param name="description">The description of the result code if any.</param>
    private ResultDescriptor(Result code, string? module = null, string? nativeApiCode = null, string? apiCode = null, string? description = null)
    {
        _result = code;
        _module = module;
        _nativeApiCode = nativeApiCode;
        _apiCode = apiCode;
        _description = description ?? GetDescriptionFromResultCode(code.Code);
    }

    public Result Result => _result;
    private int Code => Result.Code;
    public string Module => _module ?? UnknownText;
    public string NativeApiCode => _nativeApiCode ?? UnknownText;
    public string ApiCode => _apiCode ?? UnknownText;
    public string Description => _description ?? UnknownText;

    /// <inheritdoc/>
    public override string ToString()
    {
        List<FormattableString> items = new(4)
        {
            $"HRESULT: [0x{Result.Code:X}]"
        };

        if (_module is {Length: >0} module)
            items.Add($"Module: [{module}]");

        var nativeApiCode = _nativeApiCode;
        var apiCode = _apiCode;
        var hasApiCode = apiCode is { Length: >0 };
        switch (nativeApiCode is { Length: >0 })
        {
            case true when hasApiCode:
                items.Add($"ApiCode: [{nativeApiCode}/{apiCode}]");
                break;
            case true:
                items.Add($"ApiCode: [{nativeApiCode}]");
                break;
            case false when hasApiCode:
                items.Add($"ApiCode: [{apiCode}]");
                break;
            case false:
                break;
        }

        if (_description is {Length: >0} description)
            items.Add($"Message: [{description}]");

        StringBuilder builder = new(256);

        foreach (var item in items)
        {
            if (builder.Length != 0)
                builder.Append(", ");
            builder.AppendFormat(item.Format, item.GetArguments());
        }

        return builder.ToString();
    }

    public bool Equals(ResultDescriptor other) => Result.Equals(other.Result);
    public override bool Equals(object? obj) => obj is ResultDescriptor other && Result.Equals(other.Result);
    public override int GetHashCode() => Result.GetHashCode();
    public static bool operator ==(ResultDescriptor left, ResultDescriptor right) => left.Equals(right);
    public static bool operator !=(ResultDescriptor left, ResultDescriptor right) => !left.Equals(right);

    public static implicit operator Result(ResultDescriptor result) => result.Result;
    public static explicit operator int(ResultDescriptor result) => result.Result.Code;
    public static explicit operator uint(ResultDescriptor result) => unchecked((uint)result.Result.Code);

    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    public static ResultDescriptor Find(Result result) => Find(result.Code);

    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    public static ResultDescriptor Find(int result) =>
        Descriptors.GetOrAdd(result, static result => new(result));

    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    public static void Register(Result result, string? module = null, string? nativeApiCode = null,
                                string? apiCode = null, string? description = null) =>
        Register(result.Code, module, nativeApiCode, apiCode, description);

    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    public static void Register(int result, string? module = null, string? nativeApiCode = null,
                                string? apiCode = null, string? description = null) =>
        Descriptors[result] = new(result, module, nativeApiCode, apiCode, description);

    private static string? GetDescriptionFromResultCode(int resultCode)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            const int FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x00000100;
            const int FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
            const int FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;
            const int flags = FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM |
                              FORMAT_MESSAGE_IGNORE_INSERTS;

            var buffer = IntPtr.Zero;
            if (FormatMessage(flags, IntPtr.Zero, resultCode, 0, ref buffer, 0, IntPtr.Zero) == 0)
                return null;

            var description = Marshal.PtrToStringUni(buffer);
            Marshal.FreeHGlobal(buffer);
            return description?.Length > 0 ? description : null;
        }

        return null;
    }

    [DllImport("kernel32.dll", EntryPoint = "FormatMessageW", CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = true, ExactSpelling = true)]
    private static extern uint FormatMessage(int dwFlags, IntPtr lpSource, int dwMessageId, int dwLanguageId,
                                             ref IntPtr lpBuffer, int nSize, IntPtr arguments);
}