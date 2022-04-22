using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SharpGen.Runtime;

public unsafe ref struct TypeDataRegistrationHelper
{
    private List<IntPtr[]> _pointers;
    private uint _size;
    private static readonly uint PointerSize = (uint) IntPtr.Size;

    private void InitializeIfNeeded()
    {
        if (_pointers is not null)
            return;

        _pointers = new List<IntPtr[]>(6);
        _size = 0;
    }

    public void Add<T>() where T : ICallbackable => Add(TypeDataStorage.Storage<T>.SourceVtbl);

    public void Add(IntPtr[] vtbl)
    {
        InitializeIfNeeded();

        Debug.Assert(vtbl is not null);

        _pointers.Add(vtbl);
        _size += (uint) vtbl.Length;
    }

    public void Register<T>() where T : ICallbackable
    {
        InitializeIfNeeded();

        var nativePointer = (void**) MemoryHelpers.AllocateMemory(PointerSize * _size);
        var offset = 0;
        var pointers = _pointers;
        for (int i = 0, count = pointers.Count; i < count; ++i)
        {
            var item = pointers[i];
            var length = item.Length;
            item.CopyTo(new Span<IntPtr>(nativePointer + offset, length));
            offset += length;
        }

        TypeDataStorage.Register<T>(nativePointer);
        pointers.Clear();
        _size = 0;
    }
}