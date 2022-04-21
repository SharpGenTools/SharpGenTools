// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;

namespace SharpGenTools.Sdk.Extensibility;

public sealed class ExtensionLoadFailureEventArgs : EventArgs
{
    public enum FailureErrorCode
    {
        None = 0,
        UnableToLoadExtension = 1,
        UnableToCreateExtension = 2,
        InternalExtensionEntryPointCastError = 3
    }

    /// <summary>
    /// If a specific extension failed to load the namespace-qualified name of its type, null otherwise.
    /// </summary>
    public string? TypeName { get; }

    /// <summary>
    /// Error message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Error code.
    /// </summary>
    public FailureErrorCode ErrorCode { get; }

    /// <summary>
    /// Exception that was thrown while loading the extension. May be null.
    /// </summary>
    public Exception? Exception { get; }

    public ExtensionLoadFailureEventArgs(FailureErrorCode errorCode, string message, Exception? exceptionOpt = null, string? typeNameOpt = null)
    {
        if (errorCode <= FailureErrorCode.None || errorCode > FailureErrorCode.InternalExtensionEntryPointCastError)
        {
            throw new ArgumentOutOfRangeException(nameof(errorCode));
        }

        ErrorCode = errorCode;
        Message = message ?? throw new ArgumentNullException(nameof(message));
        TypeName = typeNameOpt;
        Exception = exceptionOpt;
    }
}