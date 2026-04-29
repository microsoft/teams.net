// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
    /// Creates a Teams Conversation from a Conversation
    /// </summary>
    /// <param name="conversation"></param>
    /// <returns></returns>
    public static TeamsConversation? FromConversation(Conversation? conversation)
    {
        if (conversation is null)
        {
            return null;
        }
        TeamsConversation result = new();
        result.Id = conversation.Id;
        if (conversation.Properties == null)
        {
            return result;
        }
        if (conversation.Properties.TryGetValue("tenantId", out object? tenantObj))
        {
            result.TenantId = tenantObj?.ToString();
        }
        if (conversation.Properties.TryGetValue("conversationType", out object? convTypeObj))
        {
            result.ConversationType = convTypeObj?.ToString();
        }
        if (conversation.Properties.TryGetValue("isGroup", out object? isGroupObj))
        {
            result.IsGroup = Convert.ToBoolean(isGroupObj?.ToString());
        }
        return result;
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
