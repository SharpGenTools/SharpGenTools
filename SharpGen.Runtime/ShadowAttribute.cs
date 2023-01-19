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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SharpGen.Runtime;

/// <summary>
/// Shadow attribute used to associate a C++ callable managed interface to its Shadow implementation.
/// </summary>
[AttributeUsage(AttributeTargets.Interface)]
public sealed class ShadowAttribute : Attribute
{
    /// <summary>
    /// Type of the associated shadow
    /// </summary>
#if NET6_0_OR_GREATER
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
    public Type Type { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="ShadowAttribute"/> class.
    /// </summary>
    /// <param name="holder">Type of the associated shadow</param>
    public ShadowAttribute(
#if NET6_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
        Type holder)
    {
        Type = holder ?? throw new ArgumentNullException(nameof(holder));

        Debug.Assert(typeof(CppObjectShadow).GetTypeInfo().IsAssignableFrom(holder.GetTypeInfo()));
#if !NETSTANDARD1_3
        Debug.Assert(holder.GetTypeInfo().GetConstructor(Type.EmptyTypes) is not null);
#endif
    }

    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    internal static ShadowAttribute Get(Type type) => Get(type.GetTypeInfo());
    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    internal static ShadowAttribute Get(TypeInfo type) => type.GetCustomAttribute<ShadowAttribute>();
    internal static bool Has(Type type) => Get(type.GetTypeInfo()) != null;
    internal static bool Has(TypeInfo type) => Get(type) != null;
}