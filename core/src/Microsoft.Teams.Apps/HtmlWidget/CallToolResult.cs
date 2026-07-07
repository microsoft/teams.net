// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Apps.HtmlWidget;

/// <summary>
/// A content item in an MCP UI call tool result.
/// </summary>
[Experimental("ExperimentalTeamsHtmlWidget")]
public class McpUiCallToolResultContent
{
    /// <summary>
    /// The type of content. MCP defines: "text", "image", "audio", "resource".
    /// Teams currently only renders "text" content.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";

    /// <summary>
    /// The text content.
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// The result of a widget's <c>tools/call</c> request, returned by the bot
/// in response to an <c>htmlwidget/calltool</c> invoke activity.
/// </summary>
[Experimental("ExperimentalTeamsHtmlWidget")]
public class McpUiCallToolResult
{
    /// <summary>
    /// An array of content items to return to the widget.
    /// </summary>
    [JsonPropertyName("content")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IList<McpUiCallToolResultContent>? Content { get; set; }

    /// <summary>
    /// Structured data that the widget can render from.
    /// </summary>
    [JsonPropertyName("structuredContent")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? StructuredContent { get; set; }

    /// <summary>
    /// Whether the tool call resulted in an error.
    /// </summary>
    [JsonPropertyName("isError")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsError { get; set; }
}

/// <summary>
/// The wire-format response body for an <c>htmlwidget/calltool</c> invoke.
/// Teams expects this shape (with <c>responseType</c> discriminator) rather than
/// a bare <see cref="McpUiCallToolResult"/>.
/// </summary>
[Experimental("ExperimentalTeamsHtmlWidget")]
public class HtmlWidgetCallToolResponse
{
    /// <summary>
    /// Discriminator that tells Teams how to interpret the response.
    /// </summary>
    [JsonPropertyName("responseType")]
    public string ResponseType { get; set; } = "htmlwidget/calltoolresult";

    /// <summary>
    /// The tool call result payload.
    /// </summary>
    [JsonPropertyName("callToolResult")]
    public McpUiCallToolResult CallToolResult { get; set; } = new();

    /// <summary>
    /// Creates a successful response with text content.
    /// </summary>
    /// <param name="text">The text to return to the widget.</param>
    /// <returns>A new <see cref="HtmlWidgetCallToolResponse"/>.</returns>
    public static HtmlWidgetCallToolResponse FromText(string text)
    {
        return new HtmlWidgetCallToolResponse
        {
            CallToolResult = new McpUiCallToolResult
            {
                Content = [new McpUiCallToolResultContent { Type = "text", Text = text }]
            }
        };
    }

    /// <summary>
    /// Creates an error response with a message.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>A new <see cref="HtmlWidgetCallToolResponse"/> with IsError set.</returns>
    public static HtmlWidgetCallToolResponse FromError(string message)
    {
        return new HtmlWidgetCallToolResponse
        {
            CallToolResult = new McpUiCallToolResult
            {
                Content = [new McpUiCallToolResultContent { Type = "text", Text = message }],
                IsError = true
            }
        };
    }
}
