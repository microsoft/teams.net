// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Bot.Apps.Schema.Entities;

/// <summary>
/// Represents an entity that describes the usage of sensitive content, including its name, description, and associated
/// pattern.
/// </summary>
public class SensitiveUsageEntity : OMessageEntity
{
    /// <summary>
    /// Creates a new instance of <see cref="SensitiveUsageEntity"/>.
    /// </summary>
    public SensitiveUsageEntity() : base() => OType = "CreativeWork";

    /// <summary>
    /// Gets or sets the name of the sensitive usage.
    /// </summary>
    [JsonPropertyName("name")] public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the description of the sensitive usage.
    /// </summary>
    [JsonPropertyName("description")] public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the pattern associated with the sensitive usage.
    /// </summary>
    [JsonPropertyName("pattern")] public DefinedTerm? Pattern { get; set; }
}

/// <summary>
/// Defined term.
/// </summary>
public class DefinedTerm
{
    /// <summary>
    /// Type of the defined term.
    /// </summary>
    [JsonPropertyName("@type")] public string Type { get; set; } = "DefinedTerm";

    /// <summary>
    /// OData type of the defined term.
    /// </summary>
    [JsonPropertyName("inDefinedTermSet")] public required string InDefinedTermSet { get; set; }

    /// <summary>
    /// Gets or sets the name associated with the object.
    /// </summary>
    [JsonPropertyName("name")] public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the code that identifies the academic term.
    /// </summary>
    [JsonPropertyName("termCode")] public required string TermCode { get; set; }
}
