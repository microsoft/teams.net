using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Activities;

public partial class ActivityType : StringEnum
{
    public static readonly ActivityType CommandResult = new("commandResult");
    public bool IsCommandResult => CommandResult.Equals(Value);
}

/// <summary>
/// Asynchronous external command result.
/// </summary>
public class CommandResultActivity() : Activity(ActivityType.CommandResult)
{
    /// <summary>
    /// The name of the event.
    /// </summary>
    [JsonPropertyName("name")]
    [JsonPropertyOrder(0)]
    public required string Name { get; set; }

    /// <summary>
    /// The value for this command.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonPropertyOrder(1)]
    public CommandResultValue? Value { get; set; }
}

/// <summary>
/// The value field of a <see cref="CommandResultActivity"/> contains metadata related to a command result.
/// An optional extensible data payload may be included if defined by the command result activity name. 
/// The presence of an error field indicates that the original command failed to complete.
/// </summary>
public class CommandResultValue
{
    /// <summary>
    /// Gets or sets the id of the command.
    /// </summary>
    /// <value>
    /// Id of the command.
    /// </value>
    [JsonPropertyName("commandId")]
    public required string CommandId { get; set; }

    /// <summary>
    /// Gets or sets the data field containing optional parameters specific to this command result activity,
    /// as defined by the name. The value of the data field is a complex type.
    /// </summary>
    /// <value>
    /// Open-ended value.
    /// </value>
    [JsonPropertyName("data")]
    public object? Data { get; set; }

    /// <summary>
    /// Gets or sets the optional error, if the command result indicates a failure.
    /// </summary>
    /// <value>
    /// Error which occurred during processing of the command.
    /// </value>
    [JsonPropertyName("error")]
    public Error? Error { get; set; }
}