// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Apps.Schema.Entities;

/// <summary>
/// Microsoft Purview sensitivity label information.
/// Used both as the message-level label on the root message entity and as a citation's usage info.
/// </summary>
public class SensitivityUsageInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SensitivityUsageInfo"/> class.
    /// </summary>
    public SensitivityUsageInfo()
    {
    }

    // Deserialization does not enforce Id, so an inbound label without "@id" does not fail the whole activity.
    [JsonConstructor]
    [SetsRequiredMembers]
    internal SensitivityUsageInfo(string? id)
    {
        Id = id ?? string.Empty;
    }

    /// <summary>
    /// Gets or sets the schema.org type. Defaults to "CreativeWork".
    /// </summary>
    [JsonPropertyName("@type")]
    public string Type { get; set; } = "CreativeWork";

    /// <summary>
    /// Gets or sets the Purview sensitivity label ID, a GUID such as <c>00000000-0000-0000-0000-000000000000</c>.
    /// Teams resolves the label's display name and description from this ID.
    /// </summary>
    [JsonPropertyName("@id")]
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the display name of the label.
    /// Used as a fallback when Teams cannot resolve <see cref="Id"/>.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the description of the label.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the priority of the label, used to pick the most restrictive label.
    /// Applies to the message-level label and requires <see cref="Id"/>.
    /// </summary>
    [JsonPropertyName("position")]
    public int? Position { get; set; }

    internal SensitivityUsageInfo Copy() => (SensitivityUsageInfo)MemberwiseClone();
}
