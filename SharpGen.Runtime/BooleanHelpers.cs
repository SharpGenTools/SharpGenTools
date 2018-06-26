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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;

using System.Reflection;
using System.Runtime.CompilerServices;

namespace SharpGen.Runtime
{
    /// <summary>
    /// Utility class.
    /// </summary>
    public static class BooleanHelpers
    {
        /// <summary>
        /// Converts bool array to an array of integers.
        /// </summary>
        /// <param name="array">The bool array.</param>
        /// <param name="dest">The destination array of integers.</param>
        public unsafe static void ConvertToIntArray(Span<bool> array, byte* dest)
        {
            fixed(void* src = array)
            {
                Unsafe.CopyBlockUnaligned(dest, src, (uint)array.Length);
            }
        }

        /// <summary>
        /// Converts bool array to an array of integers.
        /// </summary>
        /// <param name="array">The bool array.</param>
        /// <param name="dest">The destination array of integers.</param>
        public unsafe static void ConvertToIntArray(Span<bool> array, short* dest)
        {
            for (int i = 0; i < array.Length; i++)
                dest[i] = (short)(array[i] ? 1 : 0);
        }

        /// <summary>
        /// Converts bool array to an array of integers.
        /// </summary>
        /// <param name="array">The bool array.</param>
        /// <param name="dest">The destination array of integers.</param>
        public unsafe static void ConvertToIntArray(Span<bool> array, int* dest)
        {
            for (int i = 0; i < array.Length; i++)
                dest[i] = array[i] ? 1 : 0;
        }

        /// <summary>
        /// Converts bool array to an array of integers.
        /// </summary>
        /// <param name="array">The bool array.</param>
        /// <param name="dest">The destination array of integers.</param>
        public unsafe static void ConvertToIntArray(Span<bool> array, long* dest)
        {
            for (int i = 0; i < array.Length; i++)
                dest[i] = array[i] ? 1 : 0;
        }

        /// <summary>
        /// Converts integer array to bool array.
        /// </summary>
        /// <param name="src">A pointer to the array of integers.</param>
        /// <param name="array">The target bool array to fill.</param>
        public static unsafe void ConvertToBoolArray(byte* src, Span<bool> array)
        {
            fixed (void* dest = array)
            {
                Unsafe.CopyBlockUnaligned(dest, src, (uint)array.Length);
            }
        }

        /// <summary>
        /// Converts integer array to bool array.
        /// </summary>
        /// <param name="src">A pointer to the array of integers.</param>
        /// <param name="array">The target bool array to fill.</param>
        public static unsafe void ConvertToBoolArray(short* src, Span<bool> array)
        {
            for (int i = 0; i < array.Length; i++)
                array[i] = src[i] != 0;
        }

        /// <summary>
        /// Converts integer array to bool array.
        /// </summary>
        /// <param name="src">A pointer to the array of integers.</param>
        /// <param name="array">The target bool array to fill.</param>
        public static unsafe void ConvertToBoolArray(int* src, Span<bool> array)
        {
            for (int i = 0; i < array.Length; i++)
                array[i] = src[i] != 0;
        }

        /// <summary>
        /// Converts integer array to bool array.
        /// </summary>
        /// <param name="src">A pointer to the array of integers.</param>
        /// <param name="array">The target bool array to fill.</param>
        public static unsafe void ConvertToBoolArray(long* src, Span<bool> array)
        {
            for (int i = 0; i < array.Length; i++)
                array[i] = src[i] != 0;
        }
    }
}
