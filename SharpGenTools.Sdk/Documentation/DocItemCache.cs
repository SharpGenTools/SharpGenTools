using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using SharpGen.Doc;
using SharpGenTools.Sdk.Internal;

namespace SharpGenTools.Sdk.Documentation
{
    internal class DocItemCache
    {
        private readonly object syncObject = new();

        internal static readonly JsonSerializerOptions JsonSerializerOptions = new()
        {
            Converters =
            {
                new DocItemConverter(),
                new DocSubItemConverter()
            }
        };

        public List<IDocItem> DocItems { get; } = new();

        public void Add(IDocItem item)
        {
            lock (syncObject)
            {
                DocItems.Add(item);
            }
        }

        public IDocItem Find(string name)
        {
            lock (syncObject)
            {
                return DocItems.FirstOrDefault(item => item.Names.Contains(name));
            }
        }

        public static DocItemCache Read(string file)
        {
            Utilities.RequireAbsolutePath(file, nameof(file));

            return JsonSerializer.Deserialize<DocItemCache>(File.ReadAllBytes(file), JsonSerializerOptions);
        }

        /// <summary>
        /// Writes this instance to the specified file.
        /// </summary>
        /// <param name="file">The file.</param>
        public void Write(string file)
        {
            Utilities.RequireAbsolutePath(file, nameof(file));

            using var output = File.Create(file);
            using var writer = new Utf8JsonWriter(output);

            lock (syncObject)
            {
                JsonSerializer.Serialize(writer, this, JsonSerializerOptions);
            }
        }

        /// <summary>
        /// Writes this instance to the specified file if it is dirty.
        /// </summary>
        /// <param name="file">The file.</param>
        public void WriteIfDirty(string file)
        {
            Utilities.RequireAbsolutePath(file, nameof(file));

            if (File.Exists(file))
            {
                bool touch;

                lock (syncObject)
                {
                    // Just checking items for being dirty is enough, since we don't support removing items from cache.
                    touch = DocItems.All(x => !x.IsDirty);
                }

                if (touch)
                {
                    // We need to touch the file regardless, since MSBuild depends on outputs being 
                    File.SetLastWriteTimeUtc(file, DateTime.UtcNow);

                    return;
                }
            }

            Write(file);
        }
    }
}