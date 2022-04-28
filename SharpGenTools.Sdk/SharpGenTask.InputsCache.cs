using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using SharpGenTools.Sdk.Internal;

namespace SharpGenTools.Sdk;

public sealed partial class SharpGenTask
{
    private const string InputsCacheMarkerFiles = "# Files";
    private const string InputsCacheMarkerEnvironmentVariables = "# Environment variables";
    private readonly List<string> inputsCacheListFiles = new();
    private readonly List<string> inputsCacheListEnvironmentVariables = new();
    private readonly HashSet<string> inputsCacheSetFiles = new();
    private readonly HashSet<string> inputsCacheSetEnvironmentVariables = new();
    private readonly List<string> inputsCacheMetadataFiles = new();
    private readonly List<string> inputsCacheMetadataEnvironmentVariables = new();
    private bool inputsCacheImmutable;
    private static readonly Func<string, string> ComputeInputsCacheFileMetadataFunc = ComputeInputsCacheFileMetadata;
    private static readonly Func<string, string> ComputeInputsCacheEnvironmentVariableMetadataFunc = ComputeInputsCacheEnvironmentVariableMetadata;

    private void GenerateInputsCache()
    {
        inputsCacheImmutable = true;

        using CacheFile cacheFile = new(new FileInfo(InputsCache));

        {
            using var writer = cacheFile.StreamWriter;

            writer.WriteLine(InputsCacheMarkerFiles);
            for (int i = 0, count = inputsCacheListFiles.Count; i < count; i++)
            {
                writer.Write(inputsCacheMetadataFiles[i]);
                writer.Write(' ');
                writer.WriteLine(inputsCacheListFiles[i]);
            }

            writer.WriteLine(InputsCacheMarkerEnvironmentVariables);
            for (int i = 0, count = inputsCacheListEnvironmentVariables.Count; i < count; i++)
            {
                writer.Write(inputsCacheListEnvironmentVariables[i]);
                writer.Write(' ');
                writer.WriteLine(inputsCacheMetadataEnvironmentVariables[i]);
            }
        }

        SharpGenLogger.Message(
            cacheFile.State switch
            {
                CacheFile.CacheState.Hit => "Input file cache is already up-to-date.",
                CacheFile.CacheState.Miss => "Input file cache is out-of-date.",
                CacheFile.CacheState.Absent => "Input file cache doesn't exist.",
                _ => throw new ArgumentOutOfRangeException()
            }
        );

        if (cacheFile.IsWriteNeeded)
            cacheFile.Write();
    }

    private static string ComputeInputsCacheFileMetadata(string path)
    {
        FileInfo file = new(path);
        Debug.Assert(file.Exists);

        return $"{file.CreationTimeUtc.Ticks} {file.LastWriteTimeUtc.Ticks} {file.Length}";
    }

    private static string ComputeInputsCacheEnvironmentVariableMetadata(string name) =>
        Environment.GetEnvironmentVariable(name) is { } value ? $"true {value}" : "false";

    private void AddInputsCacheItem(string item, List<string> itemList, HashSet<string> itemSet,
                                    List<string> metadataList, Func<string, string> metadataFunc)
    {
        Debug.Assert(!inputsCacheImmutable);

        if (string.IsNullOrWhiteSpace(item))
            return;

        var metadata = metadataFunc(item);
        if (itemSet.Add(item))
        {
            itemList.Add(item);
            metadataList.Add(metadata);
        }
        else
        {
            bool Predicate(string x) => string.Equals(x, item);

            var index = itemList.FindIndex(Predicate);
            Debug.Assert(index != -1);
            Debug.Assert(metadataList[index] == metadata);
        }
    }

    private void AddInputsCacheFile(string path) => AddInputsCacheItem(
        Utilities.FixFilePath(path, Utilities.EmptyFilePathBehavior.Ignore),
        inputsCacheListFiles, inputsCacheSetFiles, inputsCacheMetadataFiles,
        ComputeInputsCacheFileMetadataFunc
    );

    private void AddInputsCacheEnvironmentVariable(string name) => AddInputsCacheItem(
        name, inputsCacheListEnvironmentVariables, inputsCacheSetEnvironmentVariables,
        inputsCacheMetadataEnvironmentVariables, ComputeInputsCacheEnvironmentVariableMetadataFunc
    );

    private void AddInputsCacheFiles(IEnumerable<string> paths)
    {
        foreach (var path in paths)
            AddInputsCacheFile(path);
    }

    private bool IsInputsCacheValid()
    {
        if (!File.Exists(InputsCache))
            return false;

        bool files = false, env = false;

        foreach (var line in File.ReadLines(InputsCache, DefaultEncoding))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (string.Equals(line, InputsCacheMarkerFiles))
            {
                files = true;
                env = false;
            }
            else if (string.Equals(line, InputsCacheMarkerEnvironmentVariables))
            {
                files = false;
                env = true;
            }
            else if (files)
            {
                var items = line.Split(SpaceSeparator, 4);
                long creation = long.Parse(items[0]),
                     modified = long.Parse(items[1]),
                     size = long.Parse(items[2]);
                FileInfo file = new(items[3]);

                if (!file.Exists)
                    return false;

                if (file.Length != size)
                    return false;

                if (file.CreationTimeUtc.Ticks != creation)
                    return false;

                if (file.LastWriteTimeUtc.Ticks != modified)
                    return false;
            }
            else if (env)
            {
                var items = line.Split(SpaceSeparator, 3);

                if (!bool.TryParse(items[1], out var nonNull))
                    return false;

                var value = Environment.GetEnvironmentVariable(items[0]);

                if (value is not null != nonNull)
                    return false;

                if (nonNull && value != items[2])
                    return false;
            }
            else
            {
                // Unsupported block, either some future SharpGen version wrote this file,
                // or the file data is simply corrupt. Either way, invalidate the cache.
                return false;
            }
        }

        return true;
    }
}