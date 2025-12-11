// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Bot.Core.Schema;

/// <summary>
/// Represents a conversation, including its unique identifier and associated extended properties.
/// </summary>
public class Conversation()
{
    /// <summary>
    /// Gets or sets the unique identifier for the object.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets the extension data dictionary for storing additional properties not defined in the schema.
    /// </summary>
    [JsonExtensionData]
#pragma warning disable CA2227 // Collection properties should be read only
    public ExtendedPropertiesDictionary Properties { get; set; } = [];
#pragma warning restore CA2227 // Collection properties should be read only
}
