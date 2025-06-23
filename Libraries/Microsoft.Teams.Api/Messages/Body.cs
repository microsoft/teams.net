// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.Messages;

/// <summary>
/// Plaintext/HTML representation of the content of the message.
/// </summary>
public class Body
{
    /// <summary>
    /// Type of the content. Possible values include: 'html', 'text'
    /// </summary>
    [JsonPropertyName("contentType")]
    [JsonPropertyOrder(0)]
    public ContentType? ContentType { get; set; }

    /// <summary>
    /// The content of the body.
    /// </summary>
    [JsonPropertyName("content")]
    [JsonPropertyOrder(1)]
    public string? Content { get; set; }

    /// <summary>
    /// The text content of the body after stripping HTML tags.
    /// </summary>
    [JsonPropertyName("textContent")]
    [JsonPropertyOrder(2)]
    public string? TextContent { get; set; }
}