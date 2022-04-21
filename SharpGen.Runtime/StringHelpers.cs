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

namespace SharpGen.Runtime;

/// <summary>
/// Utility class.
/// </summary>
public static class StringHelpers
{
    /// <summary>
    /// Converts a pointer to a null-terminating string up to maxLength characters to a .NET string.
    /// </summary>
    /// <param name="pointer">The pointer to an ANSI null string.</param>
    /// <param name="maxLength">Maximum length of the string.</param>
    /// <returns>The converted string.</returns>
    public static string PtrToStringAnsi(IntPtr pointer, int maxLength)
    {
        var managedString = Marshal.PtrToStringAnsi(pointer);
        if (managedString != null && managedString.Length > maxLength)
            managedString = managedString.Substring(0, maxLength);

        return managedString;
    }

    /// <summary>
    /// Converts a pointer to a null-terminating string up to maxLength characters to a .NET string.
    /// </summary>
    /// <param name="pointer">The pointer to an Unicode null string.</param>
    /// <param name="maxLength">Maximum length of the string.</param>
    /// <returns>The converted string.</returns>
    public static string PtrToStringUni(IntPtr pointer, int maxLength)
    {
        var managedString = Marshal.PtrToStringUni(pointer);
        if (managedString != null && managedString.Length > maxLength)
            managedString = managedString.Substring(0, maxLength);

        return managedString;
    }

    /// <summary>
    /// Converts a pointer to a BSTR data type string up to maxLength characters to a .NET string.
    /// </summary>
    /// <param name="pointer">The pointer to a BSTR string.</param>
    /// <param name="maxLength">Maximum length of the string.</param>
    /// <returns>The converted string.</returns>
    public static string PtrToStringBSTR(IntPtr pointer, int maxLength)
    {
        var managedString = Marshal.PtrToStringBSTR(pointer);
        if (managedString != null && managedString.Length > maxLength)
            managedString = managedString.Substring(0, maxLength);

        return managedString;
    }
}