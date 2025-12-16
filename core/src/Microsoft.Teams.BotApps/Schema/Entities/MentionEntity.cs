// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Bot.Core.Schema;

namespace Microsoft.Teams.BotApps.Schema.Entities;

/// <summary>
/// Mention entity.
/// </summary>
public class MentionEntity : Entity
{
    /// <summary>
    /// Creates a new instance of <see cref="MentionEntity"/>.
    /// </summary>
    public MentionEntity() : base("mention"){}

    /// <summary>
    /// Creates a new instance of <see cref="MentionEntity"/> with the specified mentioned account and text.
    /// </summary>
    /// <param name="mentioned"></param>
    /// <param name="text"></param>
    public MentionEntity(ConversationAccount mentioned, string? text) : base("mention")
    {
        Mentioned = mentioned;
        Text = text;
        ToProperties();
    }

    /// <summary>
    /// Mentioned conversation account.
    /// </summary>
    [JsonPropertyName("mentioned")] public ConversationAccount? Mentioned { get; set; }

    /// <summary>
    /// Text of the mention.
    /// </summary>
    [JsonPropertyName("text")] public string? Text { get; set; }

    /// <summary>
    /// Creates a new instance of the MentionEntity class from the specified JSON node.
    /// </summary>
    /// <param name="jsonNode">A JsonNode containing the data to deserialize. Must include a 'mentioned' property representing a
    /// ConversationAccount.</param>
    /// <returns>A MentionEntity object populated with values from the provided JSON node.</returns>
    /// <exception cref="ArgumentNullException">Thrown if jsonNode is null or does not contain the required 'mentioned' property.</exception>
    public static MentionEntity FromJsonElement(JsonNode? jsonNode)
    {
        var res = new MentionEntity
        {
            Mentioned = jsonNode?["mentioned"] != null
                ? JsonSerializer.Deserialize<ConversationAccount>(jsonNode["mentioned"]!.ToJsonString())!
                : throw new ArgumentNullException(nameof(jsonNode), "mentioned property is required"),
            Text = jsonNode?["text"]?.GetValue<string>()
        };
        res.ToProperties();
        return res;
    }

    /// <summary>
    /// Adds custom fields as properties.
    /// </summary>
    protected override void ToProperties()
    {
        base.Properties.Add("mentioned", Mentioned);
        base.Properties.Add("text", Text);
    }
}
