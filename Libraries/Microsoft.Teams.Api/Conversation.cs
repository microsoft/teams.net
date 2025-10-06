// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api;

public class Conversation
{
    /// <summary>
    /// Conversation ID
    /// </summary>
    [JsonPropertyName("id")]
    [JsonPropertyOrder(0)]
    public required string Id { get; set; }

    /// <summary>
    /// This account's object ID within Azure Active Directory (AAD).
    /// </summary>
    [JsonPropertyName("aadObjectId")]
    [JsonPropertyOrder(1)]
    public string? AadObjectId { get; set; }

    /// <summary>
    /// Conversation Tenant ID
    /// </summary>
    [JsonPropertyName("tenantId")]
    [JsonPropertyOrder(2)]
    public string? TenantId { get; set; }

    /// <summary>
    /// The Conversations Type
    /// </summary>
    [JsonPropertyName("conversationType")]
    [JsonPropertyOrder(3)]
    public required ConversationType Type { get; set; }

    /// <summary>
    /// The Conversations Name
    /// </summary>
    [JsonPropertyName("name")]
    [JsonPropertyOrder(4)]
    public string? Name { get; set; }

    /// <summary>
    /// If the Conversation supports multiple participants
    /// </summary>
    [JsonPropertyName("isGroup")]
    [JsonPropertyOrder(5)]
    public bool? IsGroup { get; set; }

    /// <summary>
    /// List of members in this conversation
    /// </summary>
    [JsonPropertyName("members")]
    [JsonPropertyOrder(6)]
    public IList<Account>? Members { get; set; }

    /// <summary>
    /// The Conversation Thread Id
    /// </summary>
    [JsonIgnore]
    public string ThreadId
    {
        get
        {
            var parts = Id.Split(';');
            return parts.Length > 1 ? parts.First() : Id;
        }
    }

    public object Clone() => MemberwiseClone();
    public Conversation Copy() => (Conversation)Clone();
}

[JsonConverter(typeof(JsonConverter<ConversationType>))]
public class ConversationType(string value) : StringEnum(value)
{
    public static readonly ConversationType Personal = new("personal");
    public bool IsPersonal => Personal.Equals(Value);

    public static readonly ConversationType GroupChat = new("groupChat");
    public bool IsGroupChat => GroupChat.Equals(Value);

    public static readonly ConversationType Channel = new("channel");
    public bool IsChannel => Channel.Equals(Value);
}

/// <summary>
/// An object relating to a particular point in a conversation
/// </summary>
public class ConversationReference : ICloneable
{
    /// <summary>
    /// (Optional) ID of the activity to refer to
    /// </summary>
    [JsonPropertyName("activityId")]
    [JsonPropertyOrder(0)]
    public string? ActivityId { get; set; }

    /// <summary>
    /// (Optional) User participating in this conversation
    /// </summary>
    [JsonPropertyName("user")]
    [JsonPropertyOrder(1)]
    public Account? User { get; set; }

    /// <summary>
    /// A locale name for the contents of the text field.
    /// The locale name is a combination of an ISO 639 two- or three-letter
    /// culture code associated with a language and an ISO 3166 two-letter
    /// subculture code associated with a country or region.
    /// The locale name can also correspond to a valid BCP-47 language tag.
    /// </summary>
    [JsonPropertyName("locale")]
    [JsonPropertyOrder(2)]
    public string? Locale { get; set; }

    /// <summary>
    /// Bot participating in this conversation
    /// </summary>
    [JsonPropertyName("bot")]
    [JsonPropertyOrder(3)]
    public required Account Bot { get; set; }

    /// <summary>
    /// Conversation
    /// </summary>
    [JsonPropertyName("conversation")]
    [JsonPropertyOrder(4)]
    public required Conversation Conversation { get; set; }

    /// <summary>
    /// Channel ID
    /// </summary>
    [JsonPropertyName("channelId")]
    [JsonPropertyOrder(5)]
    public required ChannelId ChannelId { get; set; }

    /// <summary>
    /// Service endpoint where operations concerning the referenced conversation may be performed
    /// </summary>
    [JsonPropertyName("serviceUrl")]
    [JsonPropertyOrder(6)]
    public required string ServiceUrl { get; set; }

    public object Clone() => MemberwiseClone();
    public ConversationReference Copy() => (ConversationReference)Clone();
}

/// <summary>
/// A response containing a resource
/// </summary>
public class ConversationResource
{
    /// <summary>
    /// Id of the resource
    /// </summary>
    [JsonPropertyName("id")]
    [JsonPropertyOrder(0)]
    public required string Id { get; set; }

    /// <summary>
    /// ID of the Activity (if sent)
    /// </summary>
    [JsonPropertyName("activityId")]
    [JsonPropertyOrder(1)]
    public string? ActivityId { get; set; }

    /// <summary>
    /// Service endpoint where operations concerning the conversation may be performed
    /// </summary>
    [JsonPropertyName("serviceUrl")]
    [JsonPropertyOrder(2)]
    public string? ServiceUrl { get; set; }

    public void Deconstruct(out string id, out string? activityId, out string? serviceUrl)
    {
        id = Id;
        activityId = ActivityId;
        serviceUrl = ServiceUrl;
    }
}