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
        /// Converts bool array to integer pointers array.
        /// </summary>
        /// <param name="array">The bool array.</param>
        /// <param name="dest">The destination array of int pointers.</param>
        public unsafe static void ConvertToIntArray(bool[] array, int* dest)
        {
            for (int i = 0; i < array.Length; i++)
                dest[i] = array[i] ? 1 : 0;
        }

        /// <summary>
        /// Converts integer pointer array to bool array.
        /// </summary>
        /// <param name="array">The array of integer pointers.</param>
        /// <param name="length">Array size.</param>
        /// <returns>Converted array of bool.</returns>
        public static unsafe bool[] ConvertToBoolArray(int* array, int length)
        {
            var temp = new bool[length];
            for(int i = 0; i < temp.Length; i++)
                temp[i] = array[i] != 0;
            return temp;
        }
    }
}
