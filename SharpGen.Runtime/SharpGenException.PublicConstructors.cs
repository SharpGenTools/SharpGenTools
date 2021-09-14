#nullable enable

using System;

namespace SharpGen.Runtime
{
    public partial class SharpGenException
    {
        private const string GenericSharpGenExceptionMessage = "A SharpGen exception occurred.";

        /// <summary>
        ///     Initializes a new instance of the <see cref="SharpGenException" /> class.
        /// </summary>
        public SharpGenException() : this(ResultDescriptor.Find(Result.Fail), GenericSharpGenExceptionMessage)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SharpGenException" /> class.
        /// </summary>
        /// <param name="innerException">The exception that caused this exception.</param>
        /// <param name="message">The message describing the exception.</param>
        public SharpGenException(Exception innerException, string? message = null)
            : this(ResultDescriptor.Find(innerException.HResult), message, innerException)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SharpGenException" /> class.
        /// </summary>
        /// <param name="innerException">The exception that caused this exception.</param>
        /// <param name="message">The message describing the exception.</param>
        /// <param name="args">The message formatting arguments</param>
        public SharpGenException(Exception innerException, string? message = null, params object[]? args)
            : this(ResultDescriptor.Find(innerException.HResult), message, innerException, args)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SharpGenException" /> class.
        /// </summary>
        /// <param name="result">The error result code.</param>
        /// <param name="message">The message describing the exception.</param>
        /// <param name="innerException">The exception that caused this exception.</param>
        public SharpGenException(Result result, string? message = null, Exception? innerException = null)
            : this(ResultDescriptor.Find(result), message, innerException)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SharpGenException" /> class.
        /// </summary>
        /// <param name="result">The error result code.</param>
        /// <param name="message">The message describing the exception.</param>
        /// <param name="innerException">The exception that caused this exception.</param>
        /// <param name="args">The message formatting arguments</param>
        public SharpGenException(Result result, string? message = null, Exception? innerException = null, params object[]? args)
            : this(ResultDescriptor.Find(result), message, innerException, args)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SharpGenException" /> class.
        /// </summary>
        /// <param name="message">The message describing the exception.</param>
        /// <param name="innerException">The exception that caused this exception.</param>
        public SharpGenException(string message, Exception? innerException = null)
            : this(ResultDescriptor.Find(innerException?.HResult ?? Result.Fail), message, innerException)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SharpGenException" /> class.
        /// </summary>
        /// <param name="message">The message describing the exception.</param>
        /// <param name="innerException">The exception that caused this exception.</param>
        /// <param name="args">The message formatting arguments</param>
        public SharpGenException(string message, Exception? innerException = null, params object[]? args)
            : this(ResultDescriptor.Find(innerException?.HResult ?? Result.Fail), message, innerException, args)
        {
        }
    }
}