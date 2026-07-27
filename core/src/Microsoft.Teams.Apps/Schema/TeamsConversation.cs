// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Teams.Core.Schema;
using Microsoft.Teams.Apps.Utils;

namespace Microsoft.Teams.Apps.Schema;

/// <summary>
/// String enum for Teams conversation types.
/// </summary>
[JsonConverter(typeof(StringEnumJsonConverter<ConversationType>))]
public class ConversationType(string value) : StringEnum(value)
{
    /// <summary>Personal conversation type.</summary>
    public static readonly ConversationType Personal = new("personal");
    /// <summary>Group chat conversation type.</summary>
    public static readonly ConversationType GroupChat = new("groupChat");
    /// <summary>Channel conversation type.</summary>
    public static readonly ConversationType Channel = new("channel");

}

/// <summary>
/// Common Teams conversation type values.
/// </summary>
public static class ConversationTypes
{
    /// <summary>Gets the personal conversation type.</summary>
    public static ConversationType Personal => ConversationType.Personal;

    /// <summary>Gets the group chat conversation type.</summary>
    public static ConversationType GroupChat => ConversationType.GroupChat;

    /// <summary>Gets the channel conversation type.</summary>
    public static ConversationType Channel => ConversationType.Channel;
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

        if (conversation is TeamsConversation teamsConversation)
        {
            return teamsConversation;
        }

        TeamsConversation result = new();
        result.Id = conversation.Id;
        if (conversation.Properties == null)
        {
            return result;
        }

        result.Properties = new ExtendedPropertiesDictionary(conversation.Properties);
        result.TenantId = result.Properties.Extract<string>("tenantId");
        result.ConversationType = result.Properties.Extract<ConversationType>("conversationType");
        result.IsGroup = result.Properties.Extract<bool?>("isGroup");

        return result;
    }

    /// <summary>
    /// Tenant Id.
    /// </summary>
    [JsonPropertyName("tenantId")] public string? TenantId { get; set; }

    /// <summary>
    /// Conversation Type. See <see cref="ConversationTypes"/> for known values.
    /// </summary>
    [JsonPropertyName("conversationType")] public ConversationType? ConversationType { get; set; }

    /// <summary>
    /// Indicates whether the conversation is a group conversation.
    /// </summary>
    [JsonPropertyName("isGroup")] public bool? IsGroup { get; set; }
}
