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
    public static class StringHelpers
    {
        /// <summary>
        /// Converts a pointer to a null-terminating string up to maxLength characters to a .Net string.
        /// </summary>
        /// <param name="pointer">The pointer to an ANSI null string.</param>
        /// <param name="maxLength">Maximum length of the string.</param>
        /// <returns>The converted string.</returns>
        public static string PtrToStringAnsi(IntPtr pointer, int maxLength)
        {
            string managedString = Marshal.PtrToStringAnsi(pointer); // copy null-terminating unmanaged text from pointer to a managed string
            if (managedString != null && managedString.Length > maxLength)
                managedString = managedString.Substring(0, maxLength);

            return managedString;
        }

        /// <summary>
        /// Converts a pointer to a null-terminating string up to maxLength characters to a .Net string.
        /// </summary>
        /// <param name="pointer">The pointer to an Unicode null string.</param>
        /// <param name="maxLength">Maximum length of the string.</param>
        /// <returns>The converted string.</returns>
        public static string PtrToStringUni(IntPtr pointer, int maxLength)
        {
            string managedString = Marshal.PtrToStringUni(pointer); // copy null-terminating unmanaged text from pointer to a managed string
            if (managedString != null && managedString.Length > maxLength)
                managedString = managedString.Substring(0, maxLength);

            return managedString;
        }
    }
}
