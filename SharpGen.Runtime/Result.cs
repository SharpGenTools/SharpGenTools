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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace SharpGen.Runtime
{
    /// <summary>
    /// Result structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly partial struct Result : IEquatable<Result>
    {
        /// <summary>
        /// Gets the HRESULT error code.
        /// </summary>
        /// <value>The HRESULT error code.</value>
        public int Code { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Result"/> struct.
        /// </summary>
        /// <param name="code">The HRESULT error code.</param>
        public Result(int code) => Code = code;

        /// <summary>
        /// Initializes a new instance of the <see cref="Result"/> struct.
        /// </summary>
        /// <param name="code">The HRESULT error code.</param>
        public Result(uint code) => Code = unchecked((int)code);

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

        /// <summary>
        /// Performs an implicit conversion from <see cref="Result"/> to <see cref="System.Int32"/>.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator int(Result result) => result.Code;

        /// <summary>
        /// Performs an implicit conversion from <see cref="Result"/> to <see cref="System.UInt32"/>.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator uint(Result result) => unchecked((uint)result.Code);

        /// <summary>
        /// Performs an implicit conversion from <see cref="System.Int32"/> to <see cref="Result"/>.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator Result(int result) => new(result);

        /// <summary>
        /// Performs an implicit conversion from <see cref="System.UInt32"/> to <see cref="Result"/>.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator Result(uint result) => new(result);

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
        /// </returns>
        public bool Equals(Result other) => Code == other.Code;

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns>
        /// 	<c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj) => obj is Result res && Equals(res);

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode() => Code;

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(Result left, Result right) => left.Code == right.Code;

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(Result left, Result right) => left.Code != right.Code;

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString() => $"Result: {Code}";

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
    }
}