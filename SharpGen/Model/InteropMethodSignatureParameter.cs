#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Model;

public sealed class InteropMethodSignatureParameter
{
    public InteropMethodSignatureParameter(InteropType interopType, CsMarshalCallableBase item, string? name = null)
    {
        InteropType = interopType ?? throw new ArgumentNullException(nameof(interopType));
        Item = item ?? throw new ArgumentNullException(nameof(item));
        Name = name ?? Wrap(item.Name ?? throw new ArgumentNullException(nameof(item)));
        InteropTypeSyntax = ParseTypeName(InteropType.TypeName);

        static string Wrap(string name) => name[0] == '@' ? $"_{name.Substring(1)}" : $"_{name}";
    }

    public InteropType InteropType { get; }
    public CsMarshalCallableBase Item { get; }
    public string Name { get; }
    public TypeSyntax InteropTypeSyntax { get; }

    private sealed class TypeEqualityComparer : IEqualityComparer<InteropMethodSignatureParameter>
    {
        public bool Equals(InteropMethodSignatureParameter? x, InteropMethodSignatureParameter? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            return x.GetType() == y.GetType() && x.InteropType.Equals(y.InteropType);
        }

        public int GetHashCode(InteropMethodSignatureParameter obj)
        {
            return obj.InteropType.GetHashCode();
        }
    }

    public static IEqualityComparer<InteropMethodSignatureParameter> TypeComparer { get; } =
        new TypeEqualityComparer();
}