// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.Schema;

/// <summary>
/// Defines known conversation types for Teams.
/// </summary>
public static class ConversationType
{
    /// <summary>
    /// One-to-one conversation between a user and a bot.
    /// </summary>
    public const string Personal = "personal";

    /// <summary>
    /// Group chat conversation.
    /// </summary>
    public const string GroupChat = "groupChat";

    /// <summary>
    /// Channel conversation
    /// </summary>
    public const string Channel = "channel";
}

/// <summary>
/// Teams Conversation schema.
/// </summary>
public class TeamsConversation : Conversation
{
    /// <summary>
    /// Initializes a new instance of the TeamsConversation class.
    /// </summary>
    [JsonConstructor]
    public TeamsConversation()
    {
    }

    /// <summary>
    /// Creates a new instance of the TeamsConversation class from the specified Conversation object.
    /// </summary>
    /// <param name="conversation"></param>
    public TeamsConversation(Conversation conversation)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        Id = conversation.Id;
        if (conversation.Properties == null)
        {
            return;
        }
        if (conversation.Properties.TryGetValue("tenantId", out object? tenantObj) && tenantObj is JsonElement je && je.ValueKind == JsonValueKind.String)
        {
            TenantId = je.GetString();
        }
        if (conversation.Properties.TryGetValue("conversationType", out object? convTypeObj) && convTypeObj is JsonElement je2 && je2.ValueKind == JsonValueKind.String)
        {
            ConversationType = je2.GetString();
        }
        if (conversation.Properties.TryGetValue("isGroup", out object? isGroupObj) && isGroupObj is JsonElement je3)
        {
            if (je3.ValueKind == JsonValueKind.True)
            {
                IsGroup = true;
            }
            else if (je3.ValueKind == JsonValueKind.False)
            {
                IsGroup = false;
            }
        }
    }

    /// <summary>
    /// Tenant Id.
    /// </summary>
    [JsonPropertyName("tenantId")] public string? TenantId { get; set; }

    /// <summary>
    /// Conversation Type. See <see cref="ConversationType"/> for known values.
    /// </summary>
    [JsonPropertyName("conversationType")] public string? ConversationType { get; set; }

    /// <summary>
    /// Indicates whether the conversation is a group conversation.
    /// </summary>
    [JsonPropertyName("isGroup")] public bool? IsGroup { get; set; }
}
