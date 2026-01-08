// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.BotApps.Schema;

/// <summary>
/// Represents a Microsoft Teams channel, including its identifier, type, and display name.
/// </summary>
/// <remarks>This class is typically used to serialize or deserialize channel information when interacting with
/// Microsoft Teams APIs or webhooks. All properties are optional and may be null if the corresponding data is not
/// available.</remarks>
public class TeamsChannel
{
    /// <summary>
    /// Represents the unique identifier of the channel.
    /// </summary>
    [JsonPropertyName("id")] public string? Id { get; set; }

    /// <summary>
    /// Azure Active Directory (AAD) Object ID associated with the channel.
    /// </summary>
    [JsonPropertyName("aadObjectId")] public string? AadObjectId { get; set; }

    /// <summary>
    /// Type identifier for the channel.
    /// </summary>
    [JsonPropertyName("type")] public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the name associated with the object.
    /// </summary>
    [JsonPropertyName("name")] public string? Name { get; set; }
}
