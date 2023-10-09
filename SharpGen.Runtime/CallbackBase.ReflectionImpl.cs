#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SharpGen.Runtime;

public abstract unsafe partial class CallbackBase
{
    protected virtual Guid[] BuildGuidList() => GetTypeInfo().Guids;

    private GCHandle CreateShadow(
#if NET6_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
#endif
        TypeInfo type)
    {
        var shadow = (CppObjectShadow) Activator.CreateInstance(type.AsType())!;

        // Initialize the shadow with the callback
        shadow.Initialize(ThisHandle);

        return GCHandle.Alloc(shadow, GCHandleType.Normal);
    }

#if NET6_0_OR_GREATER
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2062", Justification = $"{nameof(ShadowAttribute.Type)} is already marked `DynamicallyAccessedMemberTypes.PublicConstructors` and the existing check via `Debug.Assert(holder.GetTypeInfo().GetConstructor(Type.EmptyTypes)` will ensure correctness.")]
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2111", Justification = "Same as above.")]
#endif
    protected virtual void InitializeCallableWrappers(IDictionary<Guid, nint> ccw)
    {
        // Associate all shadows with their interfaces.
        var typeInfo = GetTypeInfo();

        var interfaces = typeInfo.Vtbls;
        if (interfaces.Length == 0)
            return;

        // Lazy solution: a single shadow for the whole hierarchy.
        // There are limitations to this approach in multi-inheritance scenarios,
        // when there are multiple shadows inheriting one, and they are in separate vtbl trees.
        // Then ToShadow methods.
        var shadowTypes = typeInfo.Shadows;

        var shadowHandle = shadowTypes.Length switch
        {
            0 => ThisHandle,
            1 => CreateShadow(shadowTypes[0]),
            _ => CreateMultiInheritanceShadow(shadowTypes.Select(CreateShadow).ToArray())
        };

        foreach (var item in interfaces)
        {
            Debug.Assert(VtblAttribute.Has(item.Type));

            var success = TypeDataStorage.GetTargetVtbl(item.Type, out var vtbl);
            Debug.Assert(success);

            var wrapper = CreateCallableWrapper(vtbl, shadowHandle);

            ccw[item.Type.GUID] = wrapper;

            // Associate also inherited interface to this shadow
            foreach (var inheritInterface in item.ImplementedInterfaces)
            {
                var guid = inheritInterface.GUID;

                // If we have the same GUID as an already added interface,
                // then there's already an accurate shadow for it, so we have nothing to do.
                if (ccw.ContainsKey(guid))
                    continue;

                // Use same CCW as derived
                ccw[guid] = wrapper;
            }
        }
    }
}