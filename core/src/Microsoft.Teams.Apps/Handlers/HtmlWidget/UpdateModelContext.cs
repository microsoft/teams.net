// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Apps.HtmlWidget;

/// <summary>
/// The parameters of an MCP UI <c>ui/update-model-context</c> request.
/// The content blocks reuse the same union as <see cref="McpUiCallToolResult"/>,
/// as defined by the MCP Apps (ext-apps) specification.
/// </summary>
[Experimental("ExperimentalTeamsHtmlWidget")]
public class McpUiUpdateModelContextParams
{
    /// <summary>
    /// An array of content blocks the widget wants to add to the model context.
    /// </summary>
    [JsonPropertyName("content")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IList<McpUiCallToolResultContent>? Content { get; set; }

    /// <summary>
    /// Structured data the widget wants to add to the model context.
    /// </summary>
    [JsonPropertyName("structuredContent")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? StructuredContent { get; set; }
}

/// <summary>
/// A widget's request to update the model context, delivered on the
/// <c>value</c> of a message activity (reusing the messageBack mechanism,
/// fire-and-forget). Defined by the MCP Apps (ext-apps) specification.
/// </summary>
[Experimental("ExperimentalTeamsHtmlWidget")]
public class McpUiUpdateModelContextRequest
{
    /// <summary>
    /// The MCP method discriminator.
    /// </summary>
    [JsonPropertyName("method")]
    public string Method { get; set; } = "ui/update-model-context";

    /// <summary>
    /// The request parameters.
    /// </summary>
    [JsonPropertyName("params")]
    public McpUiUpdateModelContextParams Params { get; set; } = new();
}
