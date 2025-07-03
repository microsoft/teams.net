// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Activities;

public partial class ActivityType : StringEnum
{
    public static readonly ActivityType Command = new("command");
    public bool IsCommand => Command.Equals(Value);
}

public class CommandActivity() : Activity(ActivityType.Command)
{
    /// <summary>
    /// The name of the event.
    /// </summary>
    [JsonPropertyName("name")]
    [JsonPropertyOrder(31)]
    public required string Name { get; set; }

    /// <summary>
    /// The value for this command.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonPropertyOrder(32)]
    public CommandValue? Value { get; set; }
}

/// <summary>
/// The value field of a ICommandActivity contains metadata related to a command.
/// An optional extensible data payload may be included if defined by the command activity name.
/// </summary>
public class CommandValue
{
    /// <summary>
    /// ID of the command.
    /// </summary>
    [JsonPropertyName("channelId")]
    [JsonPropertyOrder(0)]
    public required string ChannelId { get; set; }

    /// <summary>
    /// The data field containing optional parameters specific to this command activity,
    /// as defined by the name. The value of the data field is a complex type.
    /// </summary>
    [JsonPropertyName("data")]
    [JsonPropertyOrder(1)]
    public object? Data { get; set; }
}