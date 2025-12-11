
using Microsoft.Bot.Core.Schema;

namespace Microsoft.Bot.Core;

/// <summary>
/// Represents errors that occur during bot activity processing and provides context about the associated activity.
/// </summary>
/// <remarks>Use this exception to capture and propagate errors that occur during bot activity handling, along
/// with contextual information about the activity involved. This can aid in debugging and error reporting
/// scenarios.</remarks>
public class BotHandlerException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BotHandlerException"/> class.
    /// </summary>
    public BotHandlerException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BotHandlerException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that describes the reason for the exception.</param>
    public BotHandlerException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BotHandlerException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The error message that describes the reason for the exception.</param>
    /// <param name="innerException">The underlying exception that caused this exception, or null if no inner exception is specified.</param>
    public BotHandlerException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BotHandlerException"/> class with a specified error message, inner exception, and activity.
    /// </summary>
    /// <param name="message">The error message that describes the reason for the exception.</param>
    /// <param name="innerException">The underlying exception that caused this exception, or null if no inner exception is specified.</param>
    /// <param name="activity">The bot activity associated with the error. Cannot be null.</param>
    public BotHandlerException(string message, Exception innerException, CoreActivity activity) : base(message, innerException)
    {
        Activity = activity;
    }

    /// <summary>
    /// Accesses the bot activity associated with the exception.
    /// </summary>
    public CoreActivity? Activity { get; }
}