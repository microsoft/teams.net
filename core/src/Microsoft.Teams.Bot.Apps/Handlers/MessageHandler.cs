// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Teams.Bot.Apps.Schema;

namespace Microsoft.Teams.Bot.Apps.Handlers;

// TODO: Handlers should just have context instead of args + context.

/// <summary>
/// Delegate for handling message activities.
/// </summary>
/// <param name="messageArgs"></param>
/// <param name="context"></param>
/// <param name="cancellationToken"></param>
/// <returns></returns>
public delegate Task MessageHandler(MessageArgs messageArgs, Context context, CancellationToken cancellationToken = default);


/// <summary>
/// Message activity arguments.
/// </summary>
/// <param name="act"></param>
public class MessageArgs(TeamsActivity act)
{
    /// <summary>
    /// Activity for the message.
    /// </summary>
    public TeamsActivity Activity { get; set; } = act;

    /// <summary>
    /// Gets or sets the text content of the message.
    /// </summary>
    public string? Text { get; set; } =
        act.Properties.TryGetValue("text", out object? value)
            && value is JsonElement je
            && je.ValueKind == JsonValueKind.String
                ? je.GetString()
                : act.Properties.TryGetValue("text", out object? value2)
                    ? value2?.ToString()
                    : null;

    /// <summary>
    /// Gets or sets the text format of the message (e.g., "plain", "markdown", "xml").
    /// </summary>
    public string? TextFormat { get; set; } =
        act.Properties.TryGetValue("textFormat", out object? value)
            && value is JsonElement je
            && je.ValueKind == JsonValueKind.String
                ? je.GetString()
                : act.Properties.TryGetValue("textFormat", out object? value2)
                    ? value2?.ToString()
                    : null;
}
