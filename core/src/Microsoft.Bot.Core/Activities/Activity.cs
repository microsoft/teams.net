// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Core.Activities;

/// <summary>
/// Base class for bot activities.
/// </summary>
#pragma warning disable CA1056 // URI properties should not be strings
public class Activity
#pragma warning restore CA1056 // URI properties should not be strings
{
    /// <summary>
    /// Gets or sets the unique identifier for the activity.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the type of the activity.
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the ID of the activity to which this activity is a reply.
    /// </summary>
    [JsonPropertyName("replyToId")]
    public string? ReplyToId { get; set; }

    /// <summary>
    /// Gets or sets the channel identifier.
    /// </summary>
    [JsonPropertyName("channelId")]
    public string? ChannelId { get; set; }

    /// <summary>
    /// Gets or sets the account that sent this activity.
    /// </summary>
    [JsonPropertyName("from")]
    public Account? From { get; set; }

    /// <summary>
    /// Gets or sets the account that should receive this activity.
    /// </summary>
    [JsonPropertyName("recipient")]
    public Account? Recipient { get; set; }

    /// <summary>
    /// Gets or sets the conversation in which this activity is taking place.
    /// </summary>
    [JsonPropertyName("conversation")]
    public Conversation? Conversation { get; set; }

    /// <summary>
    /// Gets or sets a reference to another conversation or activity.
    /// </summary>
    [JsonPropertyName("relatesTo")]
    public ConversationReference? RelatesTo { get; set; }

    /// <summary>
    /// Gets or sets the URL of the service endpoint.
    /// </summary>
    [JsonPropertyName("serviceUrl")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1056:URI properties should not be strings", Justification = "Activity schema uses string for ServiceUrl")]
    public string? ServiceUrl { get; set; }

    /// <summary>
    /// Gets or sets the locale of the activity.
    /// </summary>
    [JsonPropertyName("locale")]
    public string? Locale { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of when the activity was sent.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime? Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the local timestamp of when the activity was sent.
    /// </summary>
    [JsonPropertyName("localTimestamp")]
    public DateTime? LocalTimestamp { get; set; }

    /// <summary>
    /// Gets the collection of entities included in the activity.
    /// </summary>
    [JsonPropertyName("entities")]
    public IList<Entity>? Entities { get; init; }

    /// <summary>
    /// Gets or sets channel-specific data associated with this activity.
    /// </summary>
    [JsonPropertyName("channelData")]
    public ChannelData? ChannelData { get; set; }

    /// <summary>
    /// Gets or sets extension data for additional properties.
    /// </summary>
    [JsonExtensionData]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "JsonExtensionData requires a setter")]
    public IDictionary<string, object?>? Properties { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Activity"/> class.
    /// </summary>
    public Activity()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Activity"/> class with the specified type.
    /// </summary>
    /// <param name="type">The activity type.</param>
    public Activity(string type)
    {
        Type = type;
    }
}

/// <summary>
/// Represents an account.
/// </summary>
#pragma warning disable CA1724 // Type names should not match namespaces
public class Account
#pragma warning restore CA1724 // Type names should not match namespaces
{
    /// <summary>
    /// Gets or sets the unique identifier for the account.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the Azure Active Directory object ID.
    /// </summary>
    [JsonPropertyName("aadObjectId")]
    public string? AadObjectId { get; set; }

    /// <summary>
    /// Gets or sets the role of the account. See <see cref="Roles"/> for common values.
    /// </summary>
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    /// <summary>
    /// Gets or sets the name of the account.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets additional properties.
    /// </summary>
    [JsonPropertyName("properties")]
#pragma warning disable CA2227 // Collection properties should be read only
    public Dictionary<string, object>? Properties { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
}

/// <summary>
/// String constants for account roles.
/// </summary>
public static class Roles
{
    /// <summary>
    /// Indicates the account is a bot.
    /// </summary>
    public const string Bot = "bot";

    /// <summary>
    /// Indicates the account is a user.
    /// </summary>
    public const string User = "user";
}

/// <summary>
/// Represents a conversation.
/// </summary>
public class Conversation
{
    /// <summary>
    /// Gets or sets the unique identifier for the conversation.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the conversation.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets additional properties.
    /// </summary>
    [JsonPropertyName("properties")]
#pragma warning disable CA2227 // Collection properties should be read only
    public Dictionary<string, object>? Properties { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
}

/// <summary>
/// Represents a reference to another conversation or activity.
/// </summary>
#pragma warning disable CA1056 // URI properties should not be strings
public class ConversationReference
#pragma warning restore CA1056 // URI properties should not be strings
{
    /// <summary>
    /// Gets or sets the activity ID.
    /// </summary>
    [JsonPropertyName("activityId")]
    public string? ActivityId { get; set; }

    /// <summary>
    /// Gets or sets the user account.
    /// </summary>
    [JsonPropertyName("user")]
    public Account? User { get; set; }

    /// <summary>
    /// Gets or sets the bot account.
    /// </summary>
    [JsonPropertyName("bot")]
    public Account? Bot { get; set; }

    /// <summary>
    /// Gets or sets the conversation.
    /// </summary>
    [JsonPropertyName("conversation")]
    public Conversation? Conversation { get; set; }

    /// <summary>
    /// Gets or sets the channel ID.
    /// </summary>
    [JsonPropertyName("channelId")]
    public string? ChannelId { get; set; }

    /// <summary>
    /// Gets or sets the service URL.
    /// </summary>
    [JsonPropertyName("serviceUrl")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1056:URI properties should not be strings", Justification = "Activity schema uses string for ServiceUrl")]
    public string? ServiceUrl { get; set; }
}

/// <summary>
/// Represents channel-specific data.
/// </summary>
public class ChannelData
{
    /// <summary>
    /// Gets or sets extension data for additional properties.
    /// </summary>
    [JsonExtensionData]
#pragma warning disable CA2227 // Collection properties should be read only
    public IDictionary<string, object?>? Properties { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
}

/// <summary>
/// Represents an entity.
/// </summary>
public class Entity
{
    /// <summary>
    /// Gets or sets the type of the entity.
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets extension data for additional properties.
    /// </summary>
    [JsonExtensionData]
#pragma warning disable CA2227 // Collection properties should be read only
    public IDictionary<string, object?>? Properties { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
}

/// <summary>
/// Represents an error.
/// </summary>
#pragma warning disable CA1716 // Identifiers should not match keywords
public class Error
#pragma warning restore CA1716 // Identifiers should not match keywords
{
    /// <summary>
    /// Gets or sets the error code.
    /// </summary>
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets inner HTTP error details.
    /// </summary>
    [JsonPropertyName("innerHttpError")]
    public InnerHttpError? InnerHttpError { get; set; }
}

/// <summary>
/// Represents inner HTTP error details.
/// </summary>
public class InnerHttpError
{
    /// <summary>
    /// Gets or sets the HTTP status code.
    /// </summary>
    [JsonPropertyName("statusCode")]
    public int? StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the response body.
    /// </summary>
    [JsonPropertyName("body")]
    public object? Body { get; set; }
}
