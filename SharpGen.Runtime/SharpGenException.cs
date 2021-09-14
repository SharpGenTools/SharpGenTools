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
using System.Globalization;
using System.Runtime.CompilerServices;

namespace SharpGen.Runtime
{
    /// <summary>
    ///     The base class for errors that occur in native code invoked by SharpGen.
    /// </summary>
    public partial class SharpGenException : Exception
    {
        private SharpGenException(ResultDescriptor descriptor, string? message, Exception? innerException)
            : base(FormatMessage(descriptor, message, null), innerException)
        {
            Descriptor = descriptor;
            HResult = descriptor.Result.Code;
        }

        private SharpGenException(ResultDescriptor descriptor, string? message, Exception? innerException, params object[]? args)
            : base(FormatMessage(descriptor, message, args), innerException)
        {
            Descriptor = descriptor;
            HResult = descriptor.Result.Code;
        }

        private static string FormatMessage(ResultDescriptor descriptor, string? message, object[]? args) =>
            message != null
                ? args is { Length: >0 } ? string.Format(CultureInfo.InvariantCulture, message, args) : message
                : descriptor.ToString();

        /// <summary>
        ///     Gets the <see cref="SharpGen.Runtime.Result">Result code</see> for the exception if one exists.
        ///     This value indicates the specific type of failure that occurred within the native code.
        /// </summary>
        public Result ResultCode
        {
            [MethodImpl(Utilities.MethodAggressiveOptimization)]
            get => Descriptor.Result;
        }

        /// <summary>
        ///     Gets the <see cref="SharpGen.Runtime.ResultDescriptor">Result descriptor</see> for the exception.
        ///     This value indicates the specific type of failure that occurred within the native code.
        /// </summary>
        private ResultDescriptor Descriptor
        {
            [MethodImpl(Utilities.MethodAggressiveOptimization)]
            get;
        }
    }
}