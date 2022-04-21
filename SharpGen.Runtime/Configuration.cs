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

using System.Collections.Generic;
using SharpGen.Runtime.Diagnostics;

namespace SharpGen.Runtime;

/// <summary>
/// Global configuration.
/// </summary>
public static class Configuration
{
    internal static bool ObjectTrackerImmutable;
    private static bool _enableObjectTracking;
    private static bool _enableReleaseOnFinalizer;
    private static bool _useThreadStaticObjectTracking;

    private static void UpdateIfMutable<T>(ref T field, T newValue, bool immutable)
    {
        if (EqualityComparer<T>.Default.Equals(field, newValue))
            return;

        if (immutable)
            throw new SharpGenException("Configuration is immutable after the first use");

        field = newValue;
    }

    /// <summary>
    /// Enables or disables object tracking. Default is disabled (false).
    /// </summary>
    /// <remarks>
    /// Object Tracking is used to track C++ object lifecycle creation/dispose. When this option is enabled
    /// objects can be tracked using <see cref="ObjectTracker"/>. Using Object tracking has a significant
    /// impact on performance and should be used only while debugging.
    /// </remarks>
    public static bool EnableObjectTracking
    {
        get => _enableObjectTracking;
        set => UpdateIfMutable(ref _enableObjectTracking, value, ObjectTrackerImmutable);
    }

    /// <summary>
    /// Enables or disables release of <see cref="CppObject"/> on finalizer. Default is disabled (false).
    /// </summary>
    public static bool EnableReleaseOnFinalizer
    {
        get => _enableReleaseOnFinalizer;
        set => UpdateIfMutable(ref _enableReleaseOnFinalizer, value, ObjectTrackerImmutable);
    }

    /// <summary>
    /// By default all objects in the process are tracked.
    /// Use this property to track objects per thread.
    /// </summary>
    public static bool UseThreadStaticObjectTracking
    {
        get => _useThreadStaticObjectTracking;
        set => UpdateIfMutable(ref _useThreadStaticObjectTracking, value, ObjectTrackerImmutable);
    }
}