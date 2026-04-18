#nullable enable
using System;

namespace DevOp.Toon.Client;

/// <summary>
/// Represents request or decode failures raised by <see cref="ToonClient"/>.
/// </summary>
public sealed class ToonClientException : InvalidOperationException
{
    /// <summary>
    /// Creates a new client exception with the supplied message.
    /// </summary>
    public ToonClientException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Creates a new client exception with the supplied message and inner exception.
    /// </summary>
    public ToonClientException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
