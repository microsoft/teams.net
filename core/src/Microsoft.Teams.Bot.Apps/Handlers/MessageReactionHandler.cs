// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Teams.Bot.Apps.Schema;

namespace Microsoft.Teams.Bot.Apps.Handlers;

/// <summary>
/// Delegate for handling message reaction activities.
/// </summary>
/// <param name="reactionActivity"></param>
/// <param name="context"></param>
/// <param name="cancellationToken"></param>
/// <returns></returns>
public delegate Task MessageReactionHandler(MessageReactionArgs reactionActivity, Context context, CancellationToken cancellationToken = default);


/// <summary>
/// Message reaction activity arguments.
/// </summary>
/// <param name="act"></param>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227: Collection Properties should be read only", Justification = "<Pending>")]
public class MessageReactionArgs(TeamsActivity act)
{
    /// <summary>
    /// Activity for the message reaction.
    /// </summary>
    public TeamsActivity Activity { get; set; } = act;

    /// <summary>
    /// Reactions added to the message.
    /// </summary>
    public IList<MessageReaction>? ReactionsAdded { get; set; } =
        act.Properties.TryGetValue("reactionsAdded", out object? value)
            && value is JsonElement je
            && je.ValueKind == JsonValueKind.Array
                ? JsonSerializer.Deserialize<IList<MessageReaction>>(je.GetRawText())
                : null;

    /// <summary>
    /// Reactions removed from the message.
    /// </summary>
    public IList<MessageReaction>? ReactionsRemoved { get; set; } =
        act.Properties.TryGetValue("reactionsRemoved", out object? value2)
            && value2 is JsonElement je2
            && je2.ValueKind == JsonValueKind.Array
                ? JsonSerializer.Deserialize<IList<MessageReaction>>(je2.GetRawText())
                : null;
}

/// <summary>
/// Message reaction schema.
/// </summary>
public class MessageReaction
{
    /// <summary>
    /// Type of the reaction (e.g., "like", "heart").
    /// </summary>
    [JsonPropertyName("type")] public string? Type { get; set; }
}
