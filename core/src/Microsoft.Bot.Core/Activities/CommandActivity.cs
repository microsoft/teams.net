// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Core.Activities;

/// <summary>
/// Represents a command activity.
/// </summary>
public class CommandActivity : Activity
{
    /// <summary>
    /// Gets or sets the name of the command.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the value for this command.
    /// </summary>
    [JsonPropertyName("value")]
    public CommandValue? Value { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandActivity"/> class.
    /// </summary>
    public CommandActivity() : base(ActivityTypes.Command)
    {
    }
}

/// <summary>
/// The value field of a command activity contains metadata related to a command.
/// An optional extensible data payload may be included if defined by the command activity name.
/// </summary>
public class CommandValue
{
    /// <summary>
    /// Gets or sets the ID of the command.
    /// </summary>
    [JsonPropertyName("channelId")]
    public string? ChannelId { get; set; }

    /// <summary>
    /// Gets or sets the data field containing optional parameters specific to this command activity,
    /// as defined by the name. The value of the data field is a complex type.
    /// </summary>
    [JsonPropertyName("data")]
    public object? Data { get; set; }
}
