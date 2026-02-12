// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Teams.Bot.Apps.Schema.MessageActivities;

namespace Microsoft.Teams.Bot.Apps.Schema.Invokes;

/// <summary>
/// Message extension command context values.
/// </summary>
public static class MessageExtensionCommandContext
{
    /// <summary>
    /// Command invoked from a message (message action).
    /// </summary>
    public const string Message = "message";

    /// <summary>
    /// Command invoked from the compose box.
    /// </summary>
    public const string Compose = "compose";

    /// <summary>
    /// Command invoked from the command box.
    /// </summary>
    public const string CommandBox = "commandbox";
}

/// <summary>
/// Bot message preview action values.
/// </summary>
public static class BotMessagePreviewAction
{
    /// <summary>
    /// User clicked edit on the preview.
    /// </summary>
    public const string Edit = "edit";

    /// <summary>
    /// User clicked send on the preview.
    /// </summary>
    public const string Send = "send";
}

/// <summary>
/// Context information for message extension actions.
/// </summary>
public class MessageExtensionContext
{
    /// <summary>
    /// The theme of the Teams client. Common values: "default", "dark", "contrast".
    /// </summary>
    [JsonPropertyName("theme")]
    public string? Theme { get; set; }
}

/// <summary>
/// Message extension action payload for submit action and fetch task activities.
/// </summary>
public class MessageExtensionAction
{
    /// <summary>
    /// Id of the command assigned by the bot.
    /// </summary>
    [JsonPropertyName("commandId")]
    public required string CommandId { get; set; }

    /// <summary>
    /// The context from which the command originates.
    /// See <see cref="MessageExtensionCommandContext"/> for common values.
    /// </summary>
    [JsonPropertyName("commandContext")]
    public required string CommandContext { get; set; }

    /// <summary>
    /// Bot message preview action taken by user.
    /// See <see cref="BotMessagePreviewAction"/> for common values.
    /// </summary>
    [JsonPropertyName("botMessagePreviewAction")]
    public string? BotMessagePreviewAction { get; set; }

    /// <summary>
    /// The activity preview that was originally sent to Teams when showing the bot message preview.
    /// This is sent back by Teams when the user clicks 'edit' or 'send' on the preview.
    /// </summary>
    // TODO : this needs to be activity type or something else - format is type, attachments[]
    [JsonPropertyName("botActivityPreview")]
    public TeamsActivity[]? BotActivityPreview { get; set; }

    /// <summary>
    /// Data included with the submit action.
    /// </summary>
    [JsonPropertyName("data")]
    public object? Data { get; set; }

    /// <summary>
    /// Message content sent as part of the command request when the command is invoked from a message.
    /// </summary>
    [JsonPropertyName("messagePayload")]
    public MessagePayload? MessagePayload { get; set; }

    /// <summary>
    /// Context information for the action.
    /// </summary>
    [JsonPropertyName("context")]
    public MessageExtensionContext? Context { get; set; }
}
