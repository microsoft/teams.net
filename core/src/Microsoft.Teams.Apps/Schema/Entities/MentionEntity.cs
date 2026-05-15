// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.Schema.Entities;

/// <summary>
/// Mention entity.
/// </summary>
public class MentionEntity : Entity
{
    /// <summary>
    /// Creates a new instance of <see cref="MentionEntity"/>.
    /// </summary>
    public MentionEntity() : base("mention") { }

    /// <summary>
    /// Creates a new instance of <see cref="MentionEntity"/> with the specified mentioned account and text.
    /// </summary>
    /// <param name="mentioned">The conversation account being mentioned.</param>
    /// <param name="text">The text representation of the mention, typically formatted as "&lt;at&gt;name&lt;/at&gt;".</param>
    public MentionEntity(ConversationAccount mentioned, string? text) : base("mention")
    {
        Mentioned = mentioned;
        Text = text;
    }

    /// <summary>
    /// Mentioned conversation account.
    /// </summary>
    [JsonPropertyName("mentioned")]
    public ConversationAccount? Mentioned
    {
        get => base.Properties.Get<ConversationAccount>("mentioned");
        set => base.Properties["mentioned"] = value;
    }

    /// <summary>
    /// Text of the mention.
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text
    {
        get => base.Properties.TryGetValue("text", out object? value) ? value?.ToString() : null;
        set => base.Properties["text"] = value;
    }

    /// <summary>
    /// Creates a new instance of the MentionEntity class from the specified JSON node.
    /// </summary>
    /// <param name="jsonNode">A JsonNode containing the data to deserialize. Must include a 'mentioned' property representing a
    /// ConversationAccount.</param>
    /// <returns>A MentionEntity object populated with values from the provided JSON node.</returns>
    /// <exception cref="ArgumentNullException">Thrown if jsonNode is null or does not contain the required 'mentioned' property.</exception>
    public static MentionEntity FromJsonElement(JsonNode? jsonNode)
    {
        MentionEntity res = new()
        {
            // TODO: Verify if throwing exceptions is okay here
            Mentioned = jsonNode?["mentioned"] != null
                ? JsonSerializer.Deserialize<ConversationAccount>(jsonNode["mentioned"]!.ToJsonString())!
                : throw new ArgumentNullException(nameof(jsonNode), "mentioned property is required"),
            Text = jsonNode?["text"]?.GetValue<string>()
        };
        return res;
    }
}

/// <summary>
/// Mention entity extension methods.
/// </summary>
public static class MentionEntityExtensions
{
    /// <summary>
    /// Gets all mention entities from the activity.
    /// </summary>
    public static IEnumerable<MentionEntity> GetMentions(this TeamsActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        if (activity.Entities == null)
        {
            return [];
        }

        return activity.Entities.Where(e => e is MentionEntity).Cast<MentionEntity>();
    }
}
