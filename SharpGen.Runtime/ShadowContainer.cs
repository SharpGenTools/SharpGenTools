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
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SharpGen.Runtime;

/// <summary>
/// The ShadowContainer is the main container used to keep references to all native COM/C++ callbacks.
/// It is stored in the property <see cref="CallbackBase.Shadow"/>.
/// </summary>
internal sealed class ShadowContainer : DisposeBase
{
    private static readonly Dictionary<Type, List<Type>> TypeToShadowTypes = new();
    private readonly IDictionary<Guid, CppObjectShadow> guidToShadow;
    private readonly IntPtr guidPtr;
    private readonly IntPtr[] guids;

    public IntPtr[] Guids => guids;

    public unsafe ShadowContainer(ICallbackable callbackable)
    {
        guidToShadow = new Dictionary<Guid, CppObjectShadow>();
        List<Guid> guidList = new();

        // Associate all shadows with their interfaces.
        foreach (var item in GetUninheritedShadowedInterfaces(callbackable.GetType()))
        {
            var shadowAttribute = ShadowAttribute.Get(item);

            // Initialize the shadow with the callback
            var shadow = (CppObjectShadow) Activator.CreateInstance(shadowAttribute.Type);
            shadow.Initialize(callbackable);

            guidToShadow.Add(GuidFromType(item), shadow);
            if (ExcludeFromTypeListAttribute.Has(item))
                guidList.Add(GuidFromType(item));

            // Associate also inherited interface to this shadow
            var inheritList = item.GetTypeInfo().ImplementedInterfaces;

            foreach (var inheritInterface in inheritList)
            {
                // If there isn't a Shadow attribute then this isn't a native interface.
                if (!ShadowAttribute.Has(inheritInterface))
                    continue;

                var guid = GuidFromType(inheritInterface);

                // If we have the same GUID as an already added interface,
                // then there's already an accurate shadow for it, so we have nothing to do.
                if (guidToShadow.ContainsKey(guid))
                    continue;

                // Use same shadow as derived
                guidToShadow.Add(guid, shadow);
                if (ExcludeFromTypeListAttribute.Has(inheritInterface))
                    guidList.Add(guid);
            }
        }

        var guidCount = guidList.Count;

        guids = new IntPtr[guidCount];
        guidPtr = Marshal.AllocHGlobal(Unsafe.SizeOf<Guid>() * guidCount);

        var pGuid = (Guid*) guidPtr;
        for (var i = 0; i < guidCount; i++)
        {
            pGuid[i] = ((IReadOnlyList<Guid>) guidList)[i];
            // Store the pointer
            guids[i] = new IntPtr(pGuid + i);
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
        lock (TypeToShadowTypes)
        {
            if (TypeToShadowTypes.TryGetValue(type, out var cachedInterfaces))
                return cachedInterfaces;

            var interfaces = type.GetTypeInfo().ImplementedInterfaces.ToList();
            TypeToShadowTypes.Add(type, interfaces);

            List<Type> interfacesToRemove = new();

            // First pass to identify most detailed interfaces
            foreach (var item in interfaces)
            {
                // Only process interfaces that are using shadow
                if (!ShadowAttribute.Has(item))
                {
                    interfacesToRemove.Add(item);
                    continue;
                }

                // Keep only final interfaces and not intermediate.
                interfacesToRemove.AddRange(item.GetTypeInfo().ImplementedInterfaces);
            }

            foreach (var toRemove in interfacesToRemove)
                interfaces.Remove(toRemove);

            return interfaces;
        }
    }

    public IntPtr Find(Type type) => Find(GuidFromType(type));

    internal static Guid GuidFromType(Type type) => type.GetTypeInfo().GUID;

    public IntPtr Find(Guid guidType)
    {
        var shadow = FindShadow(guidType);
        return shadow?.NativePointer ?? IntPtr.Zero;
    }

    public CppObjectShadow FindShadow(Guid guidType)
    {
        guidToShadow.TryGetValue(guidType, out var shadow);
        return shadow;
    }

    protected override bool IsDisposed => guidPtr == IntPtr.Zero;

    protected override void Dispose(bool disposing)
    {
        if (!disposing)
            return;

        foreach (var comObjectCallbackNative in guidToShadow.Values)
            comObjectCallbackNative.Dispose();
        guidToShadow.Clear();

        Marshal.FreeHGlobal(guidPtr);
    }
}