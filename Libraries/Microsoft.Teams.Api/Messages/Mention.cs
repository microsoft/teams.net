// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.Messages;

/// <summary>
/// Represents the entity that was mentioned in the message.
/// </summary>
public class Mention
{
    /// <summary>
    /// The id of the mentioned entity.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonPropertyOrder(0)]
    public required int Id { get; set; }

    /// <summary>
    /// The plaintext display name of the mentioned
    /// </summary>
    [JsonPropertyName("mentionText")]
    [JsonPropertyOrder(1)]
    public string? MentionText { get; set; }

    /// <summary>
    /// Provides more details on the mentioned entity.
    /// </summary>
    [JsonPropertyName("mentioned")]
    [JsonPropertyOrder(2)]
    public From? Mentioned { get; set; }
}