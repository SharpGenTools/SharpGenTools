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
using System.Reflection;

namespace SharpGen.Runtime
{
    /// <summary>
    /// Shadow attribute used to associate a COM callbackable interface to its Shadow implementation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface)]
    public sealed class ShadowAttribute : Attribute
    {
        /// <summary>
        /// Type of the associated shadow
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="ShadowAttribute"/> class.
        /// </summary>
        /// <param name="typeOfTheAssociatedShadow">Type of the associated shadow</param>
        public ShadowAttribute(Type typeOfTheAssociatedShadow)
        {
            Type = typeOfTheAssociatedShadow ?? throw new ArgumentNullException(nameof(typeOfTheAssociatedShadow));

            if (!typeof(CppObjectShadow).GetTypeInfo().IsAssignableFrom(typeOfTheAssociatedShadow.GetTypeInfo()))
                throw new ArgumentOutOfRangeException(
                    nameof(typeOfTheAssociatedShadow),
                    $"Shadows must inherit from {nameof(CppObjectShadow)}"
                );
        }

        /// <summary>
        /// Get <see cref="ShadowAttribute"/> from type.
        /// </summary>
        /// <returns>The associated shadow attribute or null if no shadow attribute were found</returns>
        public static ShadowAttribute Get(Type type) => type.GetTypeInfo().GetCustomAttribute<ShadowAttribute>();

        /// <summary>
        /// Checks if <see cref="ShadowAttribute"/> is present on the specified type.
        /// </summary>
        /// <returns>true if shadow attribute was found on the specified type</returns>
        public static bool Has(Type type) => Get(type) != null;
    }
}
