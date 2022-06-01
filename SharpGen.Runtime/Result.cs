// Copyright (c) 2010-2014 SharpGen.Runtime - Alexandre Mutel
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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SharpGen.Runtime;

/// <summary>
/// Result structure.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
[SuppressMessage("ReSharper", "ConvertToAutoProperty")]
public readonly partial struct Result : IComparable, IComparable<Result>, IEquatable<Result>, IFormattable
{
    // .NET Native has issues with <...> in property backing fields in structs
    private readonly int _code;

    /// <summary>
    /// Gets the HRESULT error code.
    /// </summary>
    /// <value>The HRESULT error code.</value>
    public int Code => _code;

    /// <param name="code">The HRESULT error code.</param>
    public Result(int code) => _code = code;

    /// <param name="code">The HRESULT error code.</param>
    public Result(uint code) => _code = unchecked((int) code);

    /// <param name="code">The HRESULT error code.</param>
    public Result(long code) => _code = unchecked((int) code);

    /// <param name="code">The HRESULT error code.</param>
    public Result(ulong code) => _code = unchecked((int) code);

    /// <summary>
    /// Gets a value indicating whether this <see cref="Result"/> is success.
    /// </summary>
    /// <value><c>true</c> if success; otherwise, <c>false</c>.</value>
    public bool Success => Code >= 0;

    /// <summary>
    /// Gets a value indicating whether this <see cref="Result"/> is failure.
    /// </summary>
    /// <value><c>true</c> if failure; otherwise, <c>false</c>.</value>
    public bool Failure => Code < 0;

    public static bool operator ==(Result left, Result right) => left.Code == right.Code;
    public static bool operator !=(Result left, Result right) => left.Code != right.Code;
    public static bool operator <(Result left, Result right) => left.CompareTo(right) < 0;
    public static bool operator >(Result left, Result right) => left.CompareTo(right) > 0;
    public static bool operator <=(Result left, Result right) => left.CompareTo(right) <= 0;
    public static bool operator >=(Result left, Result right) => left.CompareTo(right) >= 0;

    public static explicit operator sbyte(Result value) => unchecked((sbyte)value.Code);
    public static explicit operator short(Result value) => unchecked((short)value.Code);
    public static explicit operator long(Result value) => value.Code;
    public static explicit operator int(Result value) => value.Code;
    public static explicit operator nint(Result value) => value.Code;
    public static explicit operator byte(Result value) => unchecked((byte)value.Code);
    public static explicit operator ushort(Result value) => unchecked((ushort)value.Code);
    public static explicit operator uint(Result value) => unchecked((uint)value.Code);
    public static explicit operator ulong(Result value) => unchecked((ulong)value.Code);
    public static explicit operator nuint(Result value) => (nuint)value.Code;

    public static explicit operator Result(sbyte value) => new(value);
    public static explicit operator Result(short value) => new(value);
    public static implicit operator Result(int value) => new(value);
    public static explicit operator Result(long value) => new((int)value);
    public static explicit operator Result(nint value) => new((int)value);
    public static explicit operator Result(byte value) => new(value);
    public static explicit operator Result(ushort value) => new(value);
    public static implicit operator Result(uint value) => new(value);
    public static explicit operator Result(ulong value) => new((int)value);
    public static explicit operator Result(nuint value) => new((int)value);

    public bool Equals(Result other) => Code == other.Code;
    public override bool Equals(object? obj) => obj is Result res && Equals(res);
    public override int GetHashCode() => Code;
    public override string ToString() => Code.ToString("X8");
    /// <inheritdoc />
    public string ToString(string? format, IFormatProvider? formatProvider) => Code.ToString(format, formatProvider);

    public int CompareTo(Result other) => Code.CompareTo(other.Code);

    public int CompareTo(object? obj)
    {
        if (ReferenceEquals(null, obj)) return 1;
        return obj is Result other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(Result)}");
    }

    [DoesNotReturn]
    private void ThrowFailureException() => throw new SharpGenException(this);

    /// <summary>
    /// Checks the error.
    /// </summary>
    public void CheckError()
    {
        if (Failure)
            ThrowFailureException();
    }

    /// <summary>
    /// Checks the error.
    /// </summary>
    public void CheckError(Result allowedFail)
    {
        var code = Code;
        if (code < 0 && code != allowedFail.Code)
            ThrowFailureException();
    }

    /// <summary>
    /// Checks the error.
    /// </summary>
    public void CheckError(Result allowedFail1, Result allowedFail2)
    {
        var code = Code;
        if (code >= 0)
            return;

        if (code != allowedFail1.Code && code != allowedFail2.Code)
            ThrowFailureException();
    }

    /// <summary>
    /// Checks the error.
    /// </summary>
    public void CheckError(params Result[] allowedFails)
    {
        var code = Code;
        if (code < 0 && !allowedFails.Contains(code))
            ThrowFailureException();
    }

    /// <summary>
    /// Checks the error.
    /// </summary>
    public void CheckError(IEnumerable<Result> allowedFails)
    {
        var code = Code;
        if (code < 0 && !allowedFails.Contains(code))
            ThrowFailureException();
    }

    /// <summary>
    /// Gets a <see cref="Result"/> from an <see cref="Exception"/>.
    /// </summary>
    /// <param name="ex">The exception</param>
    /// <returns>The associated result code</returns>
    public static Result GetResultFromException(Exception ex) => new(ex.HResult);

    public string Module => ResultDescriptor.Find(Code).Module;
    public string NativeApiCode => ResultDescriptor.Find(Code).NativeApiCode;
    public string ApiCode => ResultDescriptor.Find(Code).ApiCode;
    public string Description => ResultDescriptor.Find(Code).Description;

    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    public static void Register(Result result, string? module = null, string? nativeApiCode = null,
                                string? apiCode = null, string? description = null) =>
        ResultDescriptor.Register(result.Code, module, nativeApiCode, apiCode, description);

    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    public static void Register(int result, string? module = null, string? nativeApiCode = null,
                                string? apiCode = null, string? description = null) =>
        ResultDescriptor.Register(result, module, nativeApiCode, apiCode, description);

    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    public static void Register(uint result, string? module = null, string? nativeApiCode = null,
                                string? apiCode = null, string? description = null) =>
        ResultDescriptor.Register(result, module, nativeApiCode, apiCode, description);

    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    public static void Register(long result, string? module = null, string? nativeApiCode = null,
                                string? apiCode = null, string? description = null) =>
        ResultDescriptor.Register(unchecked((int) result), module, nativeApiCode, apiCode, description);

    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    public static void Register(ulong result, string? module = null, string? nativeApiCode = null,
                                string? apiCode = null, string? description = null) =>
        ResultDescriptor.Register(unchecked((int) result), module, nativeApiCode, apiCode, description);
}