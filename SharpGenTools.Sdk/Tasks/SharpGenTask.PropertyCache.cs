#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.Build.Framework;
using SharpGenTools.Sdk.Internal;

namespace SharpGenTools.Sdk.Tasks;

public sealed partial class SharpGenTask
{
    private const int CacheFormatSignature = ('S' << 0) | ('G' << 8) | ('P' << 16) | ('C' << 24);
    private const int CacheFormatVersion = 1;
    private static readonly Encoding TextEncoding = Encoding.UTF8;

    private static HashAlgorithm CreateSettingsHash() => SHA256.Create();

    private bool GeneratePropertyCache()
    {
        List<string> parts = new(4)
        {
            "SharpGen"
        };

        if (!string.Equals(PlatformName, "AnyCPU", StringComparison.InvariantCultureIgnoreCase))
            parts.Add(PlatformName);

        if (!string.IsNullOrWhiteSpace(RuntimeIdentifier))
            parts.Add(RuntimeIdentifier);

        using MemoryStream stream = new();
        var currentHash = HashSettings(stream);

        parts.Add(currentHash);

        var profileName = string.Join("-", parts);

        workerLock = new Mutex(false, @"Global\" + profileName);
        var writeNeeded = false;

        try
        {
            while (!(workerLockAcquired = workerLock.WaitOne(TimeSpan.FromSeconds(5))) && !AbortExecution)
            {
                SharpGenLogger.Message($"Waiting for the worker to finish job {profileName}…");
            }
        }
        catch (AbandonedMutexException)
        {
            SharpGenLogger.Message($"The worker failed to complete job {profileName}.");
            workerLockAcquired = true;
            writeNeeded = true;
        }

        ProfilePath = Path.Combine(IntermediateOutputDirectory, profileName);

        if (!workerLockAcquired)
        {
            SharpGenLogger.Message($"Aborting the wait for job {profileName} to complete.");
            Debug.Assert(AbortExecution);
            return true;
        }

        var cacheItemSpec = PropertyCache;

        Utilities.RequireAbsolutePath(cacheItemSpec, nameof(PropertyCache));

        if (File.Exists(DirtyMarkerFile))
        {
            SharpGenLogger.Message("Dirty marker exists, requesting full regeneration.");
            writeNeeded = true;
        }
        else if (File.Exists(cacheItemSpec))
        {
            ReadOnlySpan<byte> cachedHash = File.ReadAllBytes(cacheItemSpec);

            stream.Position = 0;
            var success = stream.TryGetBuffer(out var buffer);
            Debug.Assert(success);

            if (buffer.AsSpan().SequenceEqual(cachedHash))
            {
                SharpGenLogger.Message("Properties match cached value.");
            }
            else
            {
                SharpGenLogger.Message("Properties mismatch, writing a new property cache file.");
                writeNeeded = true;
            }
        }
        else
        {
            SharpGenLogger.Message("Properties cache doesn't exist.");
            writeNeeded = true;
        }

        if (writeNeeded)
        {
            stream.Position = 0;
            Directory.CreateDirectory(ProfilePath);
            File.WriteAllBytes(DirtyMarkerFile, Array.Empty<byte>());
            using var file = File.Open(cacheItemSpec, FileMode.Create, FileAccess.Write);
            stream.CopyTo(file);
        }

        return writeNeeded;
    }

    private string HashSettings(Stream stream)
    {
        {
            using var writer = new StreamWriter(stream, TextEncoding, -1, true);

            writer.Write(CacheFormatSignature.ToString("X8"));
            writer.Write('.');
            writer.Write(CacheFormatVersion);
            writer.WriteLine();

            WriteStringArray(CastXmlArguments);
            WriteString(CastXmlExecutable);
            WriteStringArray(ConfigFiles);
            WriteString(ConsumerBindMappingConfigId);
            WriteBool(DocumentationFailuresAsErrors);
            WriteStringArray(ExtensionAssemblies);
            WriteStringArray(ExternalDocumentation);
            WriteTaskItems(GlobalNamespaceOverrides);
            WriteStringArray(Macros);
            WriteString(IntermediateOutputDirectory);
            WriteString(PlatformName);
            WriteString(RuntimeIdentifier);
            WriteStringArray(Platforms);
            WriteStringArray(SilenceMissingDocumentationErrorIdentifierPatterns);

            void WriteString(string? s, [CallerArgumentExpression("s")] string? name = null)
            {
                writer.Write(name);
                writer.Write(": ");
                writer.WriteLine(s ?? "<null>");
            }

            void WriteBool(bool v, [CallerArgumentExpression("v")] string? name = null)
            {
                writer.Write(name);
                writer.Write(": ");
                writer.WriteLine(v);
            }

            void WriteStringArray(IReadOnlyList<string>? items, [CallerArgumentExpression("items")] string? name = null)
            {
                writer.Write(name);
                writer.Write(":");

                if (items is null)
                {
                    writer.WriteLine(" <null>");
                    return;
                }

                writer.WriteLine();

                for (int i = 0, length = items.Count; i < length; i++)
                {
                    writer.Write("* ");
                    writer.WriteLine(items[i]);
                }
            }

            void WriteTaskItem(ITaskItem? item, [CallerArgumentExpression("item")] string? name = null)
            {
                writer.Write(name);
                writer.Write(": ");

                if (item is null)
                {
                    writer.WriteLine("<null>");
                    return;
                }

                writer.WriteLine(item.ItemSpec);

                foreach (DictionaryEntry entry in item.CloneCustomMetadata())
                {
                    writer.Write(name);
                    writer.Write("[");
                    writer.Write(entry.Key.ToString() ?? "<null>");
                    writer.Write("]: ");
                    writer.WriteLine(entry.Value?.ToString() ?? "<null>");
                }
            }

            void WriteTaskItems(IReadOnlyList<ITaskItem?>? items, [CallerArgumentExpression("items")] string? name = null)
            {
                writer.Write(name);
                writer.Write(":");

                if (items is null)
                {
                    writer.WriteLine(" <null>");
                    return;
                }

                writer.WriteLine();

                for (int i = 0, length = items.Count; i < length; i++)
                {
                    WriteTaskItem(items[i], $"{name}[{i}]");
                }
            }
        }

        stream.Position = 0;

        using var hash = CreateSettingsHash();
        return Base64UrlEncode(hash.ComputeHash(stream));
    }

    private static string Base64UrlEncode(byte[] input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        // Special-case empty input
        var count = input.Length;
        if (count == 0)
            return string.Empty;

        var numWholeOrPartialInputBlocks = checked(count + 2) / 3;
        var buffer = new char[checked(numWholeOrPartialInputBlocks * 4)];
        var numBase64Chars = Base64UrlEncode(input, buffer, count);

        return new string(buffer, 0, numBase64Chars);
    }

    private static int Base64UrlEncode(byte[] input, char[] output, int count)
    {
        // Use base64url encoding with no padding characters. See RFC 4648, Sec. 5.

        // Start with default Base64 encoding.
        var numBase64Chars = Convert.ToBase64CharArray(input, 0, count, output, 0);

        // Fix up '+' -> '-' and '/' -> '_'. Drop padding characters.
        for (var i = 0; i < numBase64Chars; i++)
        {
            switch (output[i])
            {
                case '+':
                    output[i] = '-';
                    break;
                case '/':
                    output[i] = '_';
                    break;
                case '=':
                    // We've reached a padding character; truncate the remainder.
                    return i;
            }
        }

        return numBase64Chars;
    }
}