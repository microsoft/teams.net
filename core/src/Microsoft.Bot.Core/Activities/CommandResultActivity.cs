// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Core.Activities;

/// <summary>
/// Asynchronous external command result activity.
/// </summary>
public class CommandResultActivity : Activity
{
    /// <summary>
    /// Gets or sets the name of the command.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the value for this command result.
    /// </summary>
    [JsonPropertyName("value")]
    public CommandResultValue? Value { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandResultActivity"/> class.
    /// </summary>
    public CommandResultActivity() : base(ActivityTypes.CommandResult)
    {
    }
}

/// <summary>
/// The value field of a <see cref="CommandResultActivity"/> contains metadata related to a command result.
/// An optional extensible data payload may be included if defined by the command result activity name. 
/// The presence of an error field indicates that the original command failed to complete.
/// </summary>
public class CommandResultValue
{
    /// <summary>
    /// Gets or sets the ID of the command.
    /// </summary>
    [JsonPropertyName("commandId")]
    public string? CommandId { get; set; }

    /// <summary>
    /// Gets or sets the data field containing optional parameters specific to this command result activity,
    /// as defined by the name. The value of the data field is a complex type.
    /// </summary>
    [JsonPropertyName("data")]
    public object? Data { get; set; }

    /// <summary>
    /// Gets or sets the optional error, if the command result indicates a failure.
    /// </summary>
    [JsonPropertyName("error")]
    public Error? Error { get; set; }
}
