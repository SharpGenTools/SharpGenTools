// Copyright (c) Microsoft and Contributors. All rights reserved. Licensed under the University of Illinois/NCSA Open Source License. See LICENSE.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using ClangSharp.Interop;

namespace SharpGen.Platform.Clang.Abstractions
{
    public struct StructDesc<TCustomAttrGeneratorData>
    {
        public AccessSpecifier AccessSpecifier { get; set; }
        public string EscapedName { get; set; }
        public string NativeType { get; set; }
        public string NativeInheritance { get; set; }
        public LayoutDesc Layout { get; set; }
        public Guid? Uuid { get; set; }
        public StructFlags Flags { get; set; }
        public CXSourceLocation? Location { get; set; }

        public bool IsUnsafe
        {
            get => (Flags & StructFlags.IsUnsafe) != 0;
            set => Flags = value ? Flags | StructFlags.IsUnsafe : Flags & ~StructFlags.IsUnsafe;
        }

        public bool HasVtbl
        {
            get => (Flags & StructFlags.HasVtbl) != 0;
            set => Flags = value ? Flags | StructFlags.HasVtbl : Flags & ~StructFlags.HasVtbl;
        }

        public bool IsUnion
        {
            get => (Flags & StructFlags.IsUnion) != 0;
            set => Flags = value ? Flags | StructFlags.IsUnion : Flags & ~StructFlags.IsUnion;
        }

        public Action<TCustomAttrGeneratorData> WriteCustomAttrs { get; set; }
        public TCustomAttrGeneratorData CustomAttrGeneratorData { get; set; }

        public struct LayoutDesc
        {
            public long Alignment32 { get; set; }
            public long Alignment64 { get; set; }
            public long Size32 { get; set; }
            public long Size64 { get; set; }
            public long Pack { get; set; }
            public long MaxFieldAlignment { get; set; }
            public StructLayoutAttribute LayoutAttribute { get; set; }
            public LayoutKind Kind { get; set; }
        }
    }
}
