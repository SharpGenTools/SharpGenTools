using System;
using System.ComponentModel;
using System.IO;
using Microsoft.Build.Utilities;

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

        public static TaskItem CreateTaskItem(string path) =>
            new TaskItem(FixFilePath(path, EmptyFilePathBehavior.Throw));
    }
}
