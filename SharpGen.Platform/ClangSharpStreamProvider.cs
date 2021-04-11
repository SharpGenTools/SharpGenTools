using System;
using System.Collections.Generic;
using System.IO;

namespace SharpGen.Platform
{
    internal sealed class ClangSharpStreamProvider : IDisposable
    {
        private readonly List<Item> streams = new(32);

        public IReadOnlyList<Item> Streams => streams;

        public Stream GetOutputStream(bool isTestOutput, string name, string extension)
        {
            const int capacity = 32 * 1024;
            var stream = new MemoryStream(capacity);
            streams.Add(new Item(stream, isTestOutput, name));
            return stream;
        }

        public sealed class Item
        {
            public Item(MemoryStream stream, bool isTestOutput, string name)
            {
                Stream = stream;
                IsTestOutput = isTestOutput;
                Name = name;
            }

            public MemoryStream Stream { get; }
            public bool IsTestOutput { get; }
            public string Name { get; }
        }

        public void Dispose()
        {
            foreach (var stream in streams)
                stream.Stream.Dispose();
            streams.Clear();
        }
    }
}