using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SharpGen.Runtime;

public abstract partial class CallbackBase
{
    private static readonly Dictionary<Type, List<ImmediateShadowInterfaceInfo>> TypeToShadowTypes = new();

    private static Guid[] BuildGuidList(Type type)
    {
        List<Guid> guids = new();

        // Associate all shadows with their interfaces.
        foreach (var item in GetUninheritedShadowedInterfaces(type))
        {
            var itemType = item.Type;
            if (!ExcludeFromTypeListAttribute.Has(itemType))
                guids.Add(itemType.GUID);

            // Associate also inherited interface to this shadow
            foreach (var inheritInterface in item.ImplementedInterfaces)
            {
                if (!ExcludeFromTypeListAttribute.Has(inheritInterface))
                    guids.Add(inheritInterface.GUID);
            }
        }

#if NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_1 || NET472
        HashSet<Guid> guidSet = new(guids.Count);
#else
        HashSet<Guid> guidSet = new();
#endif

        return guids.Where(guid => guidSet.Add(guid)).ToArray();
    }


    /// <summary>
    /// Gets a list of interfaces implemented by <paramref name="type"/> that aren't inherited by any other shadowed interfaces.
    /// </summary>
    /// <param name="type">The type for which to get the list.</param>
    /// <returns>The interface list.</returns>
    private static List<ImmediateShadowInterfaceInfo> GetUninheritedShadowedInterfaces(Type type)
    {
        List<ImmediateShadowInterfaceInfo> list;
        var typeToShadowTypes = TypeToShadowTypes;

        // Cache reflection on interface inheritance
        lock (typeToShadowTypes)
            if (typeToShadowTypes.TryGetValue(type, out list))
                return list;

        list = BuildUninheritedShadowedInterfacesList(type);

        lock (typeToShadowTypes)
            typeToShadowTypes[type] = list;

        return list;
    }

    private static List<ImmediateShadowInterfaceInfo> BuildUninheritedShadowedInterfacesList(Type type)
    {
        HashSet<TypeInfo> removeQueue = new();
        List<ImmediateShadowInterfaceInfo> result = new();

        foreach (var implementedInterface in type.GetTypeInfo().ImplementedInterfaces)
        {
            var item = implementedInterface.GetTypeInfo();

            // Only process interfaces that are have vtbl
            if (!VtblAttribute.Has(item))
                continue;

            ImmediateShadowInterfaceInfo interfaceInfo = new(item);
            result.Add(interfaceInfo);

            // Keep only final interfaces and not intermediate.
            foreach (var @interface in interfaceInfo.ImplementedInterfaces)
                removeQueue.Add(@interface);
        }

        result.RemoveAll(item => removeQueue.Contains(item.Type));
        return result;
    }

    private readonly struct ImmediateShadowInterfaceInfo
    {
        public readonly TypeInfo Type;
        public readonly List<TypeInfo> ImplementedInterfaces;

        public ImmediateShadowInterfaceInfo(TypeInfo type)
        {
            Type = type;
            ImplementedInterfaces = new(6);

            foreach (var implementedInterface in type.ImplementedInterfaces)
            {
                var interfaceInfo = implementedInterface.GetTypeInfo();

                // If there is no Vtbl attribute then this isn't a native interface.
                if (!VtblAttribute.Has(interfaceInfo))
                    continue;

                ImplementedInterfaces.Add(interfaceInfo);
            }
        }
    }
}