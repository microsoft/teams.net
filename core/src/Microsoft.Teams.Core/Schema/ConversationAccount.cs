// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Core.Schema;

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
    /// Gets or sets a value indicating whether this is a targeted message visible only to this recipient.
    /// </summary>
    [JsonPropertyName("isTargeted")]
    public bool? IsTargeted { get; set; }

    /// <summary>
    /// Gets or sets the agentic application ID for user-delegated token acquisition.
    /// </summary>
    [JsonPropertyName("agenticAppId")]
    public string? AgenticAppId { get; set; }

    /// <summary>
    /// Gets or sets the agentic user ID for user-delegated token acquisition.
    /// </summary>
    [JsonPropertyName("agenticUserId")]
    public string? AgenticUserId { get; set; }

    /// <summary>
    /// Gets or sets the agentic application blueprint ID.
    /// </summary>
    [JsonPropertyName("agenticAppBlueprintId")]
    public string? AgenticAppBlueprintId { get; set; }

    /// <summary>
    /// Gets the extension data dictionary for storing additional properties not defined in the schema.
    /// </summary>
    [JsonExtensionData]
    public ExtendedPropertiesDictionary Properties { get; set; } = [];

    /// <summary>
    /// Gets the agentic identity from the account's typed properties.
    /// </summary>
    /// <returns>An AgenticIdentity instance if agentic identity information is present; otherwise, null.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1024:Use properties where appropriate")]
    public AgenticIdentity? GetAgenticIdentity()
    {
        if (AgenticAppId is null && AgenticUserId is null && AgenticAppBlueprintId is null)
        {
            return null;
        }

        return new AgenticIdentity
        {
            AgenticAppId = AgenticAppId,
            AgenticUserId = AgenticUserId,
            AgenticAppBlueprintId = AgenticAppBlueprintId
        };
    }
}
