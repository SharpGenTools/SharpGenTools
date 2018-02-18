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
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SharpGen.Runtime
{
    /// <summary>
    /// The ShadowContainer is the main container used to keep references to all native COM/C++ callbacks.
    /// It is stored in the property <see cref="ICallbackable.Shadow"/>.
    /// </summary>
    public class ShadowContainer : DisposeBase
    {
        private readonly Dictionary<Guid, CppObjectShadow> guidToShadow = new Dictionary<Guid, CppObjectShadow>();

        private static readonly Dictionary<Type, List<Type>> typeToShadowTypes = new Dictionary<Type, List<Type>>();

        private IntPtr guidPtr;
        public IntPtr[] Guids { get; }

        internal ShadowContainer(ICallbackable callbackable)
        {
            callbackable.Shadow = this;

            var type = callbackable.GetType();

            var guidList = new List<Guid>();

            var shadowedInterfaces = GetUninheritedShadowedInterfaces(type);

            // Associate all shadows with their interfaces.
            foreach (var item in shadowedInterfaces)
            {
                var shadowAttribute = ShadowAttribute.Get(item);

                // Initialize the shadow with the callback
                var shadow = (CppObjectShadow)Activator.CreateInstance(shadowAttribute.Type);
                shadow.Initialize(callbackable);

                guidToShadow.Add(Utilities.GetGuidFromType(item), shadow);
                if (item.GetTypeInfo().GetCustomAttribute<ExcludeFromTypeListAttribute>() != null)
                {
                    guidList.Add(Utilities.GetGuidFromType(item));
                }

                // Associate also inherited interface to this shadow
                var inheritList = item.GetTypeInfo().ImplementedInterfaces;

                foreach (var inheritInterface in inheritList)
                {
                    var inheritShadowAttribute = ShadowAttribute.Get(inheritInterface);
                    if (inheritShadowAttribute == null)
                        continue;

                    // Use same shadow as derived
                    guidToShadow.Add(Utilities.GetGuidFromType(inheritInterface), shadow);
                    if (inheritInterface.GetTypeInfo().GetCustomAttribute<ExcludeFromTypeListAttribute>() != null)
                    {
                        guidList.Add(Utilities.GetGuidFromType(item));
                    }
                }
            }

            var guidCount = guidList.Count;

            guidPtr = Marshal.AllocHGlobal(Unsafe.SizeOf<Guid>() * guidCount);

            unsafe
            {
                var i = 0;
                var pGuid = (Guid*)guidPtr;
                foreach (var guidKey in guidList)
                {
                    pGuid[i] = guidKey;
                    // Store the pointer
                    Guids[i] = new IntPtr(pGuid + i);
                    i++;
                }
            }
        }

        /// <summary>
        /// Gets a list of interfaces implemented by <paramref name="type"/> that aren't inherited by any other shadowed interfaces.
        /// </summary>
        /// <param name="type">The type for which to get the list.</param>
        /// <returns>The interface list.</returns>
        private static List<Type> GetUninheritedShadowedInterfaces(Type type)
        {
            // Cache reflection on interface inheritance
            lock (typeToShadowTypes)
            {
                if (!typeToShadowTypes.TryGetValue(type, out List<Type> cachedInterfaces))
                {
                    var interfaces = type.GetTypeInfo().ImplementedInterfaces.ToList();
                    interfaces = new List<Type>();
                    interfaces.AddRange(interfaces);
                    typeToShadowTypes.Add(type, interfaces);

                    // First pass to identify most detailed interfaces
                    foreach (var item in interfaces)
                    {
                        // Only process interfaces that are using shadow
                        var shadowAttribute = ShadowAttribute.Get(item);
                        if (shadowAttribute == null)
                        {
                            interfaces.Remove(item);
                            continue;
                        }

                        // Keep only final interfaces and not intermediate.
                        var inheritList = item.GetTypeInfo().ImplementedInterfaces;
                        foreach (var inheritInterface in inheritList)
                        {
                            interfaces.Remove(inheritInterface);
                        }
                    }
                    return interfaces;
                }
                return cachedInterfaces;
            }
        }

        public IntPtr Find(Type type)
        {
            return Find(Utilities.GetGuidFromType(type));
        }

        public IntPtr Find(Guid guidType)
        {
            var shadow = FindShadow(guidType);
            return (shadow == null) ? IntPtr.Zero : shadow.NativePointer;
        }

        public CppObjectShadow FindShadow(Guid guidType)
        {
            guidToShadow.TryGetValue(guidType, out CppObjectShadow shadow);
            return shadow;
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var comObjectCallbackNative in guidToShadow.Values)
                    comObjectCallbackNative.Dispose();
                guidToShadow.Clear();

                if (guidPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(guidPtr);
                    guidPtr = IntPtr.Zero;
                }
            }
        }
    }
}