// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Apps.HtmlWidget;

/// <summary>
/// A request from a widget to call a tool on the bot.
/// Sent as the value of an <c>htmlwidget/calltool</c> invoke activity.
/// </summary>
[Experimental("ExperimentalTeamsHtmlWidget")]
public class CallToolRequest
{
    /// <summary>
    /// The name of the tool to call.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The arguments to pass to the tool.
    /// The Teams client adds <c>__msteams__</c> with <c>mcpUrl</c> and <c>correlationId</c> to every request and overwrites any value the widget supplied.
    /// Handlers should ignore <c>__msteams__</c>, validate every argument as untrusted input, never throw, and not authorize from <c>activity.from</c>.
    /// The value is passed through unmodified.
    /// </summary>
    [JsonPropertyName("arguments")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Arguments { get; set; }
}
