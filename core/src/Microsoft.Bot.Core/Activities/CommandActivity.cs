// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Core.Activities;

/// <summary>
/// Represents an error.
/// </summary>
public class ActivityError
{
    /// <summary>
    /// Gets or sets the error code.
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the inner HTTP error.
    /// </summary>
    public InnerHttpError? InnerHttpError { get; set; }
}

/// <summary>
/// Represents an inner HTTP error.
/// </summary>
public class InnerHttpError
{
    /// <summary>
    /// Gets or sets the HTTP status code.
    /// </summary>
    public int? StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the response body.
    /// </summary>
    public object? Body { get; set; }
}

/// <summary>
/// The value field of a command activity contains metadata related to a command.
/// </summary>
public class CommandValue
{
    /// <summary>
    /// Gets or sets the channel identifier of the command.
    /// </summary>
    public string? ChannelId { get; set; }

    /// <summary>
    /// Gets or sets the data field containing optional parameters specific to this command activity.
    /// </summary>
    public object? Data { get; set; }
}

/// <summary>
/// Represents a command activity.
/// </summary>
public class CommandActivity : Activity
{
    /// <summary>
    /// Gets or sets the name of the command.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the value for this command.
    /// </summary>
    public CommandValue? Value { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandActivity"/> class.
    /// </summary>
    public CommandActivity() : base(ActivityTypes.Command)
    {
    }
}

/// <summary>
/// The value field of a command result activity contains metadata related to a command result.
/// </summary>
public class CommandResultValue
{
    /// <summary>
    /// Gets or sets the identifier of the command.
    /// </summary>
    public string? CommandId { get; set; }

    /// <summary>
    /// Gets or sets the data field containing optional parameters specific to this command result activity.
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// Gets or sets the optional error, if the command result indicates a failure.
    /// </summary>
    public ActivityError? Error { get; set; }
}

/// <summary>
/// Represents an asynchronous external command result activity.
/// </summary>
public class CommandResultActivity : Activity
{
    /// <summary>
    /// Gets or sets the name of the command result.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the value for this command result.
    /// </summary>
    public CommandResultValue? Value { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandResultActivity"/> class.
    /// </summary>
    public CommandResultActivity() : base(ActivityTypes.CommandResult)
    {
    }
}
