using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Build.Framework;
using SharpGenTools.Sdk.Internal;

namespace SharpGenTools.Sdk.Tasks
{
    public sealed class SharpPropertyCacheTask : SharpGenTaskBase
    {
        [Required] public ITaskItem PropertyCache { get; set; }

        private const int CacheFormatSignature = ('S' << 0) | ('G' << 8) | ('P' << 16) | ('C' << 24);
        private const int CacheFormatVersion = 1;
        private static readonly Encoding TextEncoding = Encoding.UTF8;

        private static HashAlgorithm CreateSettingsHash() => SHA512.Create();

        public override bool Execute()
        {
            PrepareExecute();

            var cacheItemSpec = PropertyCache.ItemSpec;
            Utilities.RequireAbsolutePath(cacheItemSpec, nameof(PropertyCache));

            var currentHash = HashSettings();

            bool writeNeeded;

            if (File.Exists(cacheItemSpec))
            {
                ReadOnlySpan<byte> currentHashSpan = currentHash;
                ReadOnlySpan<byte> cachedHash = File.ReadAllBytes(cacheItemSpec);

                if (currentHashSpan.SequenceEqual(cachedHash))
                {
                    SharpGenLogger.Message("Properties hash matches cached value.");
                    writeNeeded = false;
                }
                else
                {
                    SharpGenLogger.Message("Properties hash mismatch, writing a new property cache file.");
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
                File.WriteAllBytes(cacheItemSpec, currentHash);
            }

            return true;
        }

        private byte[] HashSettings()
        {
            using var stream = new MemoryStream();
            WriteProperties(stream);

            stream.Position = 0;

            using var hash = CreateSettingsHash();
            return hash.ComputeHash(stream);
        }

        private void WriteProperties(Stream stream)
        {
            using var writer = new BinaryWriter(stream, TextEncoding, true);

            writer.Write(CacheFormatSignature);
            writer.Write(CacheFormatVersion);
            WriteStringArray(CastXmlArguments);
            WriteTaskItem(CastXmlExecutable);
            WriteTaskItems(ConfigFiles);
            WriteString(ConsumerBindMappingConfigId);
            WriteTaskItem(DocumentationCache);
            WriteBool(DocumentationFailuresAsErrors);
            WriteTaskItems(ExtensionAssemblies);
            WriteTaskItems(ExternalDocumentation);
            WriteString(GeneratedCodeFolder);
            WriteTaskItems(GlobalNamespaceOverrides);
            WriteTaskItem(InputsCache);
            WriteStringArray(Macros);
            WriteString(OutputPath);
            WriteTaskItems(Platforms);
            WriteTaskItems(SilenceMissingDocumentationErrorIdentifierPatterns);
            WriteTaskItem(ConsumerBindMappingConfig);
            WriteBool(UseFunctionPointersInVtbl);

            void WriteString(string s)
            {
                if (s == null)
                {
                    writer.Write(0);
                    return;
                }

                writer.Write(s);
            }

            void WriteStringArray(IReadOnlyList<string> strings)
            {
                var length = strings.Count;
                writer.Write(length);
                for (var i = 0; i < length; i++)
                {
                    WriteString(strings[i]);
                }
            }

            void WriteTaskItem(ITaskItem item)
            {
                if (item == null)
                {
                    writer.Write(0);
                    return;
                }

                writer.Write(item.ItemSpec);
                var metadata = item.CloneCustomMetadata();
                var length = metadata.Count;
                writer.Write(length);
            }

            void WriteTaskItems(IReadOnlyList<ITaskItem> items)
            {
                var length = items.Count;
                writer.Write(length);
                for (var i = 0; i < length; i++)
                {
                    WriteTaskItem(items[i]);
                }
            }

            void WriteBool(bool v) => writer.Write(v);
        }
    }
}