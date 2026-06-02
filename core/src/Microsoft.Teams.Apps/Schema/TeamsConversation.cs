// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.Schema;

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
/// <remarks>
/// Initializes a new instance of the TeamsConversation class.
/// </remarks>

/// <summary>
/// Teams Conversation schema.
/// </summary>
[method: JsonConstructor]
public class TeamsConversation() : Conversation
{

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
        TeamsConversation result = new()
        {
            Id = conversation.Id
        };
        if (conversation.Properties == null)
        {
            return result;
        }

        result.Properties = new ExtendedPropertiesDictionary(conversation.Properties);
        result.TenantId = result.Properties.Extract<string>("tenantId");
        result.ConversationType = result.Properties.Extract<string>("conversationType");
        result.IsGroup = result.Properties.Extract<bool?>("isGroup");

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
