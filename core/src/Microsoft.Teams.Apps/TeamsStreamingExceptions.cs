// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Apps;

/// <summary>
/// Base class for streaming errors (HTTP 403) that should not be retried.
/// See the Teams streaming error codes:
/// https://learn.microsoft.com/en-us/microsoftteams/platform/bots/streaming-ux?tabs=csharp#error-codes
/// </summary>
public class TerminalStreamException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalStreamException"/> class.
    /// </summary>
    public TerminalStreamException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalStreamException"/> class with a message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public TerminalStreamException(string? message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalStreamException"/> class with a message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public TerminalStreamException(string? message, Exception? innerException) : base(message, innerException) { }
}

/// <summary>
/// Raised when the bot failed to complete streaming within the two-minute limit.
/// </summary>
public class StreamTimedOutException : TerminalStreamException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StreamTimedOutException"/> class.
    /// </summary>
    public StreamTimedOutException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamTimedOutException"/> class with a message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public StreamTimedOutException(string? message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamTimedOutException"/> class with a message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public StreamTimedOutException(string? message, Exception? innerException) : base(message, innerException) { }
}

/// <summary>
/// Raised when streaming is not allowed for this user or bot.
/// </summary>
public class StreamNotAllowedException : TerminalStreamException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StreamNotAllowedException"/> class.
    /// </summary>
    public StreamNotAllowedException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamNotAllowedException"/> class with a message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public StreamNotAllowedException(string? message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamNotAllowedException"/> class with a message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public StreamNotAllowedException(string? message, Exception? innerException) : base(message, innerException) { }
}

/// <summary>
/// Raised when Teams cancels a stream (for example, when the user presses the Stop button).
/// </summary>
public class StreamCancelledException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StreamCancelledException"/> class.
    /// </summary>
    public StreamCancelledException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamCancelledException"/> class with a message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public StreamCancelledException(string? message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamCancelledException"/> class with a message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public StreamCancelledException(string? message, Exception? innerException) : base(message, innerException) { }
}
