// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Teams.Bot.Apps.Schema.MessageActivities;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.Schema.Entities;

/// <summary>
/// Extension methods for Activity to handle mentions.
/// </summary>
public static class ActivityMentionExtensions
{
    /// <summary>
    /// Gets the MentionEntity from the activity's entities.
    /// </summary>
    /// <param name="activity">The activity to extract the mention from.</param>
    /// <returns>The MentionEntity if found; otherwise, null.</returns>
    public static IEnumerable<MentionEntity> GetMentions(this TeamsActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        if (activity.Entities == null)
        {
            return [];
        }
        return activity.Entities.Where(e => e is MentionEntity).Cast<MentionEntity>();
    }

    /// <summary>
    /// Adds a mention (@ mention) of a user or bot to the activity.
    /// </summary>
    /// <param name="activity">The activity to add the mention to. Cannot be null.</param>
    /// <param name="account">The conversation account being mentioned. Cannot be null.</param>
    /// <param name="text">Optional custom text for the mention. If null, uses the account name.</param>
    /// <param name="addText">If true, prepends the mention text to the activity's existing text content. Defaults to true.</param>
    /// <returns>The created MentionEntity that was added to the activity.</returns>
    public static MentionEntity AddMention(this TeamsActivity activity, ConversationAccount account, string? text = null, bool addText = true)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(account);
        string? mentionText = text ?? account.Name;
        if (addText)
        {
            string? currentText = activity.Properties.TryGetValue("text", out var t) ? t?.ToString() : null;
            activity.Properties["text"] = $"<at>{mentionText}</at> {currentText}";
        }
        activity.Entities ??= [];
        MentionEntity mentionEntity = new(account, $"<at>{mentionText}</at>");
        activity.Entities.Add(mentionEntity);
        activity.Rebase();
        return mentionEntity;
    }
}

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
        get => base.Properties.TryGetValue("mentioned", out var value) ? value as ConversationAccount : null;
        set => base.Properties["mentioned"] = value;
    }

    /// <summary>
    /// Text of the mention.
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text
    {
        get => base.Properties.TryGetValue("text", out var value) ? value?.ToString() : null;
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
