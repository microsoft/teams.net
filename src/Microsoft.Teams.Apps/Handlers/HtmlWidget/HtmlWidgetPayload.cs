// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Apps.HtmlWidget;

/// <summary>
/// The security policy for an HTML widget, controlling allowed origins
/// for network requests, static resources, nested iframes, and base URIs.
/// </summary>
[Experimental("ExperimentalTeamsHtmlWidget")]
public class HtmlWidgetSecurityPolicy
{
    /// <summary>
    /// Allowed origins for network requests.
    /// </summary>
    [JsonPropertyName("connectDomains")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IList<string>? ConnectDomains { get; set; }

    /// <summary>
    /// Allowed origins for static resources.
    /// </summary>
    [JsonPropertyName("resourceDomains")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IList<string>? ResourceDomains { get; set; }

    /// <summary>
    /// Allowed origins for nested iframes.
    /// </summary>
    [JsonPropertyName("frameDomains")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IList<string>? FrameDomains { get; set; }

    /// <summary>
    /// Allowed base URIs for the document.
    /// </summary>
    [JsonPropertyName("baseUriDomains")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IList<string>? BaseUriDomains { get; set; }
}

/// <summary>
/// Permissions that the widget may request from the host.
/// Presence of a field means the permission is requested (value should be an empty object).
/// </summary>
[Experimental("ExperimentalTeamsHtmlWidget")]
public class HtmlWidgetPermissions
{
    /// <summary>
    /// Request camera access. Set to an empty dictionary to request.
    /// </summary>
    [JsonPropertyName("camera")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IDictionary<string, object>? Camera { get; set; }

    /// <summary>
    /// Request microphone access. Set to an empty dictionary to request.
    /// </summary>
    [JsonPropertyName("microphone")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IDictionary<string, object>? Microphone { get; set; }

    /// <summary>
    /// Request geolocation access. Set to an empty dictionary to request.
    /// </summary>
    [JsonPropertyName("geolocation")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IDictionary<string, object>? Geolocation { get; set; }

    /// <summary>
    /// Request clipboard write access. Set to an empty dictionary to request.
    /// </summary>
    [JsonPropertyName("clipboardWrite")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IDictionary<string, object>? ClipboardWrite { get; set; }
}

/// <summary>
/// The JSON payload for an HTML widget, sent inside a ```html-widget code block
/// within a Markdown message.
/// </summary>
[Experimental("ExperimentalTeamsHtmlWidget")]
public class HtmlWidgetPayload
{
    /// <summary>
    /// The widget type identifier. Currently only "widget/mcp-ui" is supported.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "widget/mcp-ui";

    /// <summary>
    /// The display name of the MCP app.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// A description of the MCP app.
    /// </summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    /// <summary>
    /// The HTML content that makes up the widget.
    /// </summary>
    [JsonPropertyName("html")]
    public string Html { get; set; } = string.Empty;

    /// <summary>
    /// The domain associated with the widget, applied to sandbox metadata.
    /// Must be a valid domain URL (e.g. 'https://example.com').
    /// This is informational metadata, not a verified identity claim.
    /// The platform does not authenticate this value.
    /// </summary>
    [JsonPropertyName("domain")]
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// Optional security policy controlling allowed origins.
    /// </summary>
    [JsonPropertyName("securityPolicy")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public HtmlWidgetSecurityPolicy? SecurityPolicy { get; set; }

    /// <summary>
    /// Optional data that was passed as input to the tool that produced this widget.
    /// </summary>
    [JsonPropertyName("toolInput")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? ToolInput { get; set; }

    /// <summary>
    /// Optional data that the tool produced alongside this widget.
    /// </summary>
    [JsonPropertyName("toolOutput")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? ToolOutput { get; set; }

    /// <summary>
    /// Optional permissions the widget requests from the host.
    /// </summary>
    [JsonPropertyName("permissions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public HtmlWidgetPermissions? Permissions { get; set; }

    /// <summary>
    /// Optional MCP server URL.
    /// When set, the Teams client combines it with the owner id ("ownerId-mcpUrl") as the input for deriving the widget hosting origin.
    /// A widget produced by an MCP server must set it to that server's URL to get its own hosting scope.
    /// </summary>
    [JsonPropertyName("mcpUrl")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [SuppressMessage("Design", "CA1056:URI-like properties should not be strings", Justification = "Sent on the wire as a string and may not be an absolute URI")]
    public string? McpUrl { get; set; }

    /// <summary>
    /// Optional correlation vector for the widget.
    /// The Teams client also returns it to the bot on widget requests.
    /// </summary>
    [JsonPropertyName("correlationId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CorrelationId { get; set; }
}
