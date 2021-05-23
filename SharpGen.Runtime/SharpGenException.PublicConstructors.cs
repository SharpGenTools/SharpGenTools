#nullable enable

using System;
using System.Globalization;

namespace SharpGen.Runtime
{
    public partial class SharpGenException
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SharpGenException" /> class.
        /// </summary>
        public SharpGenException() : this(Result.Fail, "A SharpGen exception occurred.")
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SharpGenException" /> class.
        /// </summary>
        /// <param name="result">The result code that caused this exception.</param>
        /// <param name="innerException">The exception that caused this exception.</param>
        public SharpGenException(Result result, Exception? innerException = null)
            : this(ResultDescriptor.Find(result), innerException: innerException)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SharpGenException" /> class.
        /// </summary>
        /// <param name="result">The error result code.</param>
        /// <param name="message">The message describing the exception.</param>
        /// <param name="innerException">The exception that caused this exception.</param>
        public SharpGenException(Result result, string message, Exception? innerException = null)
            : this(result, null, message, innerException)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SharpGenException" /> class.
        /// </summary>
        /// <param name="descriptor">The result descriptor.</param>
        /// <param name="message">The message describing the exception.</param>
        /// <param name="innerException">The exception that caused this exception.</param>
        public SharpGenException(ResultDescriptor descriptor, string? message = null, Exception? innerException = null)
            : this(descriptor.Result, descriptor, message ?? descriptor.ToString(), innerException)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SharpGenException" /> class.
        /// </summary>
        /// <param name="result">The error result code.</param>
        /// <param name="message">The message describing the exception.</param>
        /// <param name="innerException">The exception that caused this exception.</param>
        /// <param name="args">The message formatting arguments</param>
        public SharpGenException(Result result, string message, Exception? innerException = null, params object[] args)
            : this(result, null, string.Format(CultureInfo.InvariantCulture, message, args), innerException)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SharpGenException" /> class.
        /// </summary>
        /// <param name="descriptor">The result descriptor.</param>
        /// <param name="message">The message describing the exception.</param>
        /// <param name="innerException">The exception that caused this exception.</param>
        /// <param name="args">The message formatting arguments</param>
        public SharpGenException(ResultDescriptor descriptor, string message, Exception? innerException = null,
                                 params object[] args)
            : this(descriptor.Result, descriptor, string.Format(CultureInfo.InvariantCulture, message, args),
                   innerException)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SharpGenException" /> class.
        /// </summary>
        /// <param name="message">The message describing the exception.</param>
        /// <param name="innerException">The exception that caused this exception.</param>
        /// <param name="args">The message formatting arguments</param>
        public SharpGenException(string message, Exception? innerException = null, params object[] args)
            : this(Result.Fail, message, innerException, args)
        {
        }
    }
}