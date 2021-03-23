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
using System.Runtime.InteropServices;

namespace SharpGen.Runtime
{
    /// <summary>
    ///     A boolean value stored on 4 bytes (instead of 1 in .NET).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 4)]
    public readonly struct RawBool : IEquatable<RawBool>
    {
        private readonly int boolValue;

        /// <summary>
        ///     Initializes a new instance of the <see cref="RawBool" /> class.
        /// </summary>
        /// <param name="boolValue">if set to <c>true</c> [bool value].</param>
        public RawBool(bool boolValue) => this.boolValue = boolValue ? 1 : 0;

        public override bool Equals(object obj)
        {
            return obj switch
            {
                null => false,
                bool b => Equals(new RawBool(b)),
                RawBool rawBool => Equals(rawBool),
                _ => false
            };
        }

        public bool Equals(RawBool other) => boolValue == other.boolValue;
        public override int GetHashCode() => boolValue;
        public static bool operator ==(RawBool left, RawBool right) => left.Equals(right);
        public static bool operator !=(RawBool left, RawBool right) => !left.Equals(right);
        public static implicit operator bool(RawBool booleanValue) => booleanValue.boolValue != 0;
        public static implicit operator RawBool(bool boolValue) => new(boolValue);
        public override string ToString() => $"{boolValue != 0}";
    }
}