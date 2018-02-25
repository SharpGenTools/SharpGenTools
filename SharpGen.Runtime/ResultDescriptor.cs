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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SharpGen.Runtime
{
    /// <summary>
    /// Descriptor used to provide detailed message for a particular <see cref="Result"/>.
    /// </summary>
    public sealed class ResultDescriptor
    {
        private static readonly ConcurrentDictionary<Result, ResultDescriptor> Descriptors = new ConcurrentDictionary<Result, ResultDescriptor>();
        private const string UnknownText = "Unknown";

        /// <summary>
        /// Initializes a new instance of the <see cref="ResultDescriptor"/> class.
        /// </summary>
        /// <param name="code">The HRESULT error code.</param>
        /// <param name="module">The module (ex: SharpDX.Direct2D1).</param>
        /// <param name="apiCode">The API code (ex: D2D1_ERR_...).</param>
        /// <param name="description">The description of the result code if any.</param>
        public ResultDescriptor(Result code, string module, string nativeApiCode, string apiCode, string description = null)
        {
            Result = code;
            Module = module;
            NativeApiCode = nativeApiCode;
            ApiCode = apiCode;
            Description = description ?? GetDescriptionFromResultCode(Code) ?? UnknownText;

            Descriptors.TryAdd(code, this);
        }

        /// <summary>
        /// Gets the result.
        /// </summary>
        public Result Result { get; }

        /// <summary>
        /// Gets the HRESULT error code.
        /// </summary>
        /// <value>The HRESULT error code.</value>
        public int Code => Result.Code;

        /// <summary>
        /// Gets the module
        /// </summary>
        public string Module { get; }

        /// <summary>
        /// Gets the native API code
        /// </summary>
        public string NativeApiCode { get;  }

        /// <summary>
        /// Gets the API code
        /// </summary>
        public string ApiCode { get; }

        /// <summary>
        /// Gets the description of the result code if any.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Determines whether the specified <see cref="ResultDescriptor"/> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="ResultDescriptor"/> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="ResultDescriptor"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(ResultDescriptor other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return other.Result.Equals(this.Result);
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj is ResultDescriptor descriptor)
                return Equals(descriptor);
            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Result.GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Format("HRESULT: [0x{0:X}], Module: [{1}], ApiCode: [{2}/{3}], Message: {4}", this.Result.Code, this.Module, this.NativeApiCode, this.ApiCode, this.Description);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="ResultDescriptor"/> to <see cref="SharpDX.Result"/>.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator Result(ResultDescriptor result)
        {
            return result.Result;
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="SharpDX.ResultDescriptor"/> to <see cref="System.Int32"/>.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator int(ResultDescriptor result)
        {
            return result.Result.Code;
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="SharpDX.ResultDescriptor"/> to <see cref="System.UInt32"/>.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator uint(ResultDescriptor result)
        {
            return unchecked((uint)result.Result.Code);
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(ResultDescriptor left, Result right)
        {
            if (left == null)
                return false;
            return left.Result.Code == right.Code;
        }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(ResultDescriptor left, Result right)
        {
            if (left == null)
                return false;
            return left.Result.Code != right.Code;
        }

        /// <summary>
        /// Finds the specified result descriptor.
        /// </summary>
        /// <param name="result">The result code.</param>
        /// <returns>A descriptor for the specified result</returns>
        public static ResultDescriptor Find(Result result)
        {
            ResultDescriptor descriptor;

            if (!Descriptors.TryGetValue(result, out descriptor))
            {
                descriptor = new ResultDescriptor(result, UnknownText, UnknownText, UnknownText);
            }

            return descriptor;
        }

        private static string GetDescriptionFromResultCode(int resultCode)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                const int FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x00000100;
                const int FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
                const int FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;

                var buffer = IntPtr.Zero;
                FormatMessageW(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS, IntPtr.Zero, resultCode, 0, ref buffer, 0, IntPtr.Zero);
                var description = Marshal.PtrToStringUni(buffer);
                Marshal.FreeHGlobal(buffer);
                return description; 
            }
            return null;
        }

        [DllImport("kernel32.dll", EntryPoint = "FormatMessageW")]
        private static extern uint FormatMessageW(int dwFlags, IntPtr lpSource, int dwMessageId, int dwLanguageId, ref IntPtr lpBuffer, int nSize, IntPtr Arguments);
    }
}