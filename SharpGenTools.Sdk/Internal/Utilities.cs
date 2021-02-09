using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using SharpGenTools.Sdk.Internal.Roslyn;

namespace SharpGenTools.Sdk.Internal
{
    public static class Utilities
    {
        public enum EmptyFilePathBehavior
        {
            Ignore,
            Throw
        }

        public static string FixFilePath(string path, EmptyFilePathBehavior emptyBehavior)
        {
            return string.IsNullOrEmpty(path)
                ? emptyBehavior switch
                {
                    EmptyFilePathBehavior.Ignore => path,
                    EmptyFilePathBehavior.Throw => throw new ArgumentException(
                        "Value cannot be null or empty.",
                        nameof(path)
                    ),
                    _ => throw new InvalidEnumArgumentException(
                        nameof(emptyBehavior),
                        (int) emptyBehavior,
                        typeof(EmptyFilePathBehavior)
                    )
                }
                : ToPlatformSlash(path);

            static string ToPlatformSlash(string s)
            {
                var separator = Path.DirectorySeparatorChar;

                return s.Replace(separator == '/' ? '\\' : '/', separator);
            }
        }

#nullable enable

        internal static void RequireAbsolutePath(string path, string argumentName)
        {
            if (path == null)
                throw new ArgumentNullException(argumentName);

            if (!PathUtilities.IsAbsolute(path))
                throw new ArgumentException("Expected absolute path", argumentName);
        }

        /// <summary>
        /// Converts a array to an immutable array.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items">The sequence to convert</param>
        /// <returns></returns>
        /// <remarks>If the sequence is null, this will return the default (null) array.</remarks>
        public static ImmutableArray<T> AsImmutableOrNull<T>(this T[]? items)
        {
            return items == null ? default : ImmutableArray.Create(items);
        }

        internal static string? TryNormalizeAbsolutePath(string path)
        {
            Debug.Assert(PathUtilities.IsAbsolute(path));

            try
            {
                return Path.GetFullPath(path);
            }
            catch
            {
                return null;
            }
        }

        internal static Stream OpenRead(string fullPath)
        {
            Debug.Assert(PathUtilities.IsAbsolute(fullPath));

            try
            {
                return new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch (Exception e) when (e is IOException)
            {
                throw new IOException(e.Message, e);
            }
        }
    }
}
