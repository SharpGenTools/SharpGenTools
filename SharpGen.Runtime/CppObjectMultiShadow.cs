#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace SharpGen.Runtime;

internal sealed class CppObjectMultiShadow
{
    private readonly GCHandle[] _shadows;

    public CppObjectMultiShadow(GCHandle[] shadows)
    {
        _shadows = shadows ?? throw new ArgumentNullException(nameof(shadows));

#if DEBUG
        foreach (var handle in _shadows)
        {
            Debug.Assert(handle.IsAllocated);
            Debug.Assert(handle.Target is CppObjectShadow or CppObjectMultiShadow);
        }
#endif
    }

    public T? ToShadow<T>() where T : CppObjectShadow
    {
        foreach (var handle in _shadows)
        {
            switch (handle.Target)
            {
                case T shadow:
                    return shadow;
                case CppObjectMultiShadow multiShadow when multiShadow.ToShadow<T>() is { } shadow:
                    return shadow;
            }
        }

        return null;
    }

    public bool ToCallback<
#if NET6_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
#elif NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
#endif
    T>([NotNullWhen(true)] out T? value) where T : ICallbackable
    {
        foreach (var handle in _shadows)
        {
            switch (handle.Target)
            {
                case CppObjectShadow shadow:
                    value = shadow.ToCallback<T>();
                    return true;
                case CppObjectMultiShadow multiShadow when multiShadow.ToShadow<CppObjectShadow>() is { } shadow:
                    value = shadow.ToCallback<T>();
                    return true;
            }
        }

        value = default;
        return false;
    }

    internal void AddShadowsToSet(HashSet<CppObjectShadow> shadows)
    {
        foreach (var handle in _shadows)
        {
            if (!handle.IsAllocated)
                continue;

            switch (handle.Target)
            {
                case CppObjectShadow shadow:
                    shadows.Add(shadow);
                    break;
                case CppObjectMultiShadow multiShadow:
                    multiShadow.AddShadowsToSet(shadows);
                    break;
            }
        }
    }
}