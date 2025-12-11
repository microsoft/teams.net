// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Bot.Core.Schema;

/// <summary>
/// Represents a conversation account, including its unique identifier, display name, and any additional properties
/// associated with the conversation.
/// </summary>
/// <remarks>This class is typically used to model the account information for a conversation in messaging or chat
/// applications. The additional properties dictionary allows for extensibility to support custom metadata or
/// protocol-specific fields.</remarks>
public class ConversationAccount()
{
    /// <summary>
    /// Gets or sets the unique identifier for the object.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the display name of the conversation account.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets the extension data dictionary for storing additional properties not defined in the schema.
    /// </summary>
    [JsonExtensionData]
#pragma warning disable CA2227 // Collection properties should be read only
    public ExtendedPropertiesDictionary Properties { get; set; } = [];
#pragma warning restore CA2227 // Collection properties should be read only
}
