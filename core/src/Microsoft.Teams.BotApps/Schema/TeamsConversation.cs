// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.BotApps.Schema;

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
        Id = string.Empty;
    }

    /// <summary>
    /// Creates a new instance of the TeamsConversation class from the specified Conversation object.
    /// </summary>
    /// <param name="conversation"></param>
    public TeamsConversation(Conversation conversation)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        Id = conversation.Id ?? string.Empty;
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
    }

    /// <summary>
    /// Tenant Id.
    /// </summary>
    [JsonPropertyName("tenantId")] public string? TenantId { get; set; }

    /// <summary>
    /// Conversation Type. See <see cref="Schema.ConversationType"/> for known values.
    /// </summary>
    [JsonPropertyName("conversationType")] public string? ConversationType { get; set; }
}
