#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using SharpGen.Runtime.Trimming;

namespace SharpGen.Runtime;

public abstract partial class CallbackBase
{
    private readonly struct ImmediateShadowInterfaceInfo
    {
#if NET6_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
#endif
        public readonly TypeInfo Type;
        public readonly List<TypeInfo> ImplementedInterfaces;

        public ImmediateShadowInterfaceInfo(
#if NET6_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
#endif
            TypeInfo type)
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

    // Cache reflection on interface inheritance
    private class CallbackTypeInfo
    {
#if NET6_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
#endif
        private readonly TypeInfo type;
        private ImmediateShadowInterfaceInfo[]? _vtbls;
        private TypeInfo[]? _shadows;
        private Guid[]? _guids;

        public CallbackTypeInfo(
#if NET6_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
#endif
            Type type) : this(type.GetTypeInfo())
        {
        }

        private CallbackTypeInfo(
#if NET6_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
#endif
            TypeInfo type)
        {
            this.type = type ?? throw new ArgumentNullException(nameof(type));
        }

        /// <summary>
        /// Gets a list of implemented interfaces that aren't inherited by any other <c>[Vtbl]</c> interfaces.
        /// </summary>
        /// <returns>The interface list.</returns>
        public ImmediateShadowInterfaceInfo[] Vtbls
        {
            get
            {
                lock (this)
                    if (_vtbls is { } vtbls)
                        return vtbls;

                var list = BuildVtblList();

                lock (this)
                    _vtbls = list;

                return list;
            }
        }

        public TypeInfo[] Shadows
        {
            get
            {
                lock (this)
                    if (_shadows is { } shadows)
                        return shadows;

                var list = BuildShadowList();

                lock (this)
                    _shadows = list;

                return list;
            }
        }

        public Guid[] Guids
        {
            get
            {
                lock (this)
                    if (_guids is { } guids)
                        return guids;

                var list = BuildGuidList();

                lock (this)
                    _guids = list;

                return list;
            }
        }

        private ImmediateShadowInterfaceInfo[] BuildVtblList()
        {
            HashSet<TypeInfo> removeQueue = new();
            List<ImmediateShadowInterfaceInfo> result = new();

            foreach (var implementedInterface in type.ImplementedInterfaces)
            {
                var item = implementedInterface.GetTypeInfoWithNestedPreservedInterfaces();

                // Only process interfaces that have vtbl
                if (!VtblAttribute.Has(item))
                    continue;

                ImmediateShadowInterfaceInfo interfaceInfo = new(item);
                result.Add(interfaceInfo);

                // Keep only final interfaces and not intermediate.
                foreach (var @interface in interfaceInfo.ImplementedInterfaces)
                    removeQueue.Add(@interface);
            }

            result.RemoveAll(item => removeQueue.Contains(item.Type));
            return result.ToArray();
        }

        private Guid[] BuildGuidList()
        {
            List<Guid> guids = new();

            // Associate all shadows with their interfaces.
            foreach (var item in Vtbls)
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

        private TypeInfo[] BuildShadowList()
        {
            List<TypeInfo> shadows = new(), result;

            foreach (var implementedInterface in type.ImplementedInterfaces)
            {
                var attribute = ShadowAttribute.Get(implementedInterface);

                // Only process interfaces that have Shadow attribute
                if (attribute is null)
                    continue;

                shadows.Add(attribute.Type.GetTypeInfo());
            }

            var count = shadows.Count;
            result = new List<TypeInfo>(count);

            // Retain only shadows which have no other subtypes (none of the other shadows are children)
            for (var i = 0; i < count; i++)
            {
                var item = shadows[i];

                var any = false;
                for (var j = 0; j < count; j++)
                {
                    if (i == j)
                        continue;

                    if (item.IsAssignableFrom(shadows[j]))
                    {
                        any = true;
                        break;
                    }
                }

                if (!any)
                    result.Add(item);
            }

            return result.ToArray();
        }
    }
}