#nullable enable
using System;
using System.Net;

namespace DevOp.Toon.Client;

/// <summary>
/// Represents a request or response failure raised by <see cref="ToonClient"/>.
/// </summary>
/// <remarks>
/// Exceptions created by <see cref="ToonClient"/> carry the raw response body in <see cref="Content"/>,
/// the response media type in <see cref="ContentType"/>, and the HTTP status code in <see cref="StatusCode"/>.
/// Use <see cref="Decode{T}"/> to deserialize a structured error body directly from the exception.
/// </remarks>
public sealed class ToonClientException : InvalidOperationException
{
    private readonly Func<Type, object?>? _decoder;

    /// <summary>
    /// Creates a new client exception with the supplied message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ToonClientException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Creates a new client exception with the supplied message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that caused this failure.</param>
    public ToonClientException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    internal ToonClientException(
        string message,
        string? content,
        string? contentType,
        HttpStatusCode? statusCode,
        Func<Type, object?>? decoder,
        Exception? innerException)
        : base(message, innerException)
    {
        Content = content;
        ContentType = contentType;
        StatusCode = statusCode;
        _decoder = decoder;
    }

    /// <summary>
    /// The raw response body, if available.
    /// </summary>
    /// <value>
    /// The response body as a string, or <see langword="null"/> when the response had no body
    /// or the exception was not created by <see cref="ToonClient"/>.
    /// </value>
    public string? Content { get; }

    /// <summary>
    /// The response <c>Content-Type</c> media type, if available.
    /// </summary>
    /// <value>
    /// The media type string (e.g. <c>application/toon</c>, <c>application/json</c>),
    /// or <see langword="null"/> when the response carried no content type or the exception
    /// was not created by <see cref="ToonClient"/>.
    /// </value>
    public string? ContentType { get; }

    /// <summary>
    /// The HTTP status code of the response that triggered this exception, if available.
    /// </summary>
    /// <value>
    /// The <see cref="HttpStatusCode"/>, or <see langword="null"/> when the exception was
    /// created via a public constructor rather than by <see cref="ToonClient"/>.
    /// </value>
    public HttpStatusCode? StatusCode { get; }

    /// <summary>
    /// Deserializes <see cref="Content"/> as <typeparamref name="T"/>, using <see cref="ContentType"/>
    /// to select the appropriate decoder (TOON or JSON).
    /// </summary>
    /// <typeparam name="T">The type to deserialize the response body into.</typeparam>
    /// <returns>
    /// The deserialized value, or <see langword="null"/> when <see cref="Content"/> is empty.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the exception was not created by <see cref="ToonClient"/> and carries no decoder.
    /// </exception>
    /// <exception cref="ToonClientException">
    /// Thrown when <see cref="ContentType"/> is unsupported or deserialization of <see cref="Content"/> fails.
    /// </exception>
    /// <remarks>
    /// This is most useful for reading structured error bodies (e.g. <c>ProblemDetails</c>) from failed requests:
    /// <code>
    /// catch (ToonClientException ex) when (ex.StatusCode == HttpStatusCode.UnprocessableEntity)
    /// {
    ///     var errors = ex.Decode&lt;ValidationErrors&gt;();
    /// }
    /// </code>
    /// </remarks>
    public T? Decode<T>()
    {
        if (_decoder == null)
            throw new InvalidOperationException(
                "Decode<T>() is only available on exceptions thrown by ToonClient.");
        return (T?)_decoder(typeof(T));
    }
}
