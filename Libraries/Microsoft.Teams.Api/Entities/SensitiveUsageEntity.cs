// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.Entities;

public class SensitiveUsageEntity : OMessageEntity, IMessageEntity
{
    [JsonPropertyName("name")]
    [JsonPropertyOrder(3)]
    public required string Name { get; set; }

    [JsonPropertyName("description")]
    [JsonPropertyOrder(4)]
    public string? Description { get; set; }

    [JsonPropertyName("pattern")]
    [JsonPropertyOrder(5)]
    public DefinedTerm? Pattern { get; set; }

    public SensitiveUsageEntity() : base()
    {
        OType = "CreativeWork";
    }
}

public class DefinedTerm
{
    [JsonPropertyName("@type")]
    [JsonPropertyOrder(0)]
    public string Type { get; set; } = "DefinedTerm";

    [JsonPropertyName("inDefinedTermSet")]
    [JsonPropertyOrder(1)]
    public required string InDefinedTermSet { get; set; }

    [JsonPropertyName("name")]
    [JsonPropertyOrder(2)]
    public required string Name { get; set; }

    [JsonPropertyName("termCode")]
    [JsonPropertyOrder(3)]
    public required string TermCode { get; set; }
}