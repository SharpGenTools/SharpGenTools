#nullable enable

using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;

namespace SharpGenTools.Sdk.Internal;

public ref struct CacheFile
{
    public enum CacheState
    {
        Hit,
        Miss,
        Absent
    }

    private bool? _isWriteNeeded = null;
    private CacheState _state = CacheState.Absent;
    private bool _hasWritten = false;
    private StreamWriter? _writer = null;
    private FileInfo? _file = null;

    public CacheFile()
    {
        Stream = new MemoryStream();
    }

    public CacheFile(FileInfo file) : this()
    {
        File = file;
    }

    public FileInfo File
    {
        get => _file ?? throw new InvalidOperationException();
        set
        {
            _file = value ?? throw new ArgumentNullException(nameof(value));
            Utilities.RequireAbsolutePath(value.FullName, nameof(value));
        }
    }

    private MemoryStream Stream { get; }

    public bool IsWriteNeeded
    {
        get
        {
            return _isWriteNeeded ??= ComputeIsWriteNeeded();
        }
    }

    public CacheState State
    {
        get
        {
            _isWriteNeeded ??= ComputeIsWriteNeeded();
            return _state;
        }
    }

    private bool ComputeIsWriteNeeded()
    {
        Debug.Assert(_isWriteNeeded is null);
        Debug.Assert(_writer is not null);
        Debug.Assert(!_hasWritten);

        _writer.Dispose();

        File.Refresh();

        if (!File.Exists)
        {
            _state = CacheState.Absent;
            return true;
        }

        ReadOnlySpan<byte> cachedHash = System.IO.File.ReadAllBytes(File.FullName);

        Stream.Position = 0;
        var success = Stream.TryGetBuffer(out var buffer);
        Debug.Assert(success);

        if (buffer.AsSpan().SequenceEqual(cachedHash))
        {
            _state = CacheState.Hit;
            return false;
        }

        _state = CacheState.Miss;
        return true;
    }

    public StreamWriter StreamWriter =>
        _writer is null
            ? _writer = new StreamWriter(Stream, SharpGenTask.DefaultEncoding, 1024, true)
            : throw new InvalidOperationException();

    public void Write()
    {
        Debug.Assert(_isWriteNeeded == true);
        Debug.Assert(!_hasWritten);

        Stream.Position = 0;

        if (File.DirectoryName is { } directory)
            Directory.CreateDirectory(directory);

        using var file = File.Open(FileMode.Create, FileAccess.Write);
        Stream.CopyTo(file);
        _hasWritten = true;
    }

    public byte[] ComputeHash(HashAlgorithm hash)
    {
        Debug.Assert(_writer is not null);

        _writer.Dispose();

        Stream.Position = 0;

        return hash.ComputeHash(Stream);
    }

    public void Dispose()
    {
        if (_writer is { } writer)
            writer.Dispose();
        Stream.Dispose();
    }
}