using System;
using System.Diagnostics;

namespace SharpGen.Runtime.Diagnostics
{
    public static partial class ObjectTracker
    {
        /// <summary>
        /// Function which provides stack trace for object tracking.
        /// </summary>
        public static Func<string> StackTraceProvider { get; set; } = GetStackTrace;

        /// <summary>
        /// Gets default stack trace.
        /// </summary>
        public static string GetStackTrace()
        {
#if NETSTANDARD1_1
            try
            {
                throw new GetStackTraceException();
            }
            catch (GetStackTraceException ex)
            {
                return ex.StackTrace;
            }
#elif NETSTANDARD1_3
            return Environment.StackTrace;
#else
            return new StackTrace(3).ToString();
#endif
        }

#if NETSTANDARD1_1
        private sealed class GetStackTraceException : Exception
        {
        }
#endif
    }
}