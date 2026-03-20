// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Bot.Apps.Schema;

/// <summary>
/// Represents a clickable action associated with a card or suggested action.
/// </summary>
public class CardAction
{
    /// <summary>
    /// Default constructor for JSON deserialization.
    /// </summary>
    public CardAction()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CardAction"/> class with the specified type and title.
    /// </summary>
    /// <param name="type">The type of action. See <see cref="CardActionTypes"/> for common values.</param>
    /// <param name="title">The text description displayed on the button.</param>
    public CardAction(string type, string title)
    {
        Type = type;
        Title = title;
    }

    /// <summary>
    /// Gets or sets the type of action implemented by this button.
    /// See <see cref="CardActionTypes"/> for common values.
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the text description which appears on the button.
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the image URL which will appear on the button, next to the text label.
    /// </summary>
    [JsonPropertyName("image")]
    public string? Image { get; set; }

    /// <summary>
    /// Gets or sets the text for this action.
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the text to display in the chat feed if the button is clicked.
    /// </summary>
    [JsonPropertyName("displayText")]
    public string? DisplayText { get; set; }

    /// <summary>
    /// Gets or sets the supplementary parameter for the action.
    /// The content of this property depends on the action type.
    /// </summary>
    [JsonPropertyName("value")]
    public object? Value { get; set; }

    /// <summary>
    /// Gets or sets the channel-specific data associated with this action.
    /// </summary>
    [JsonPropertyName("channelData")]
    public object? ChannelData { get; set; }

    /// <summary>
    /// Gets or sets the alternate image text to be used in place of the image.
    /// </summary>
    [JsonPropertyName("imageAltText")]
    public string? ImageAltText { get; set; }
}

/// <summary>
/// String constants for card action types.
/// </summary>
public static class CardActionTypes
{
    /// <summary>
    /// Opens the specified URL in the browser.
    /// </summary>
    public const string OpenUrl = "openUrl";

    /// <summary>
    /// Sends a message back to the bot as if the user typed it (visible to all conversation members).
    /// </summary>
    public const string IMBack = "imBack";

    /// <summary>
    /// Sends a message back to the bot privately (not visible to other conversation members).
    /// </summary>
    public const string PostBack = "postBack";

    /// <summary>
    /// Plays the specified audio content.
    /// </summary>
    public const string PlayAudio = "playAudio";

    /// <summary>
    /// Plays the specified video content.
    /// </summary>
    public const string PlayVideo = "playVideo";

    /// <summary>
    /// Displays the specified image.
    /// </summary>
    public const string ShowImage = "showImage";

    /// <summary>
    /// Downloads the specified file.
    /// </summary>
    public const string DownloadFile = "downloadFile";

    /// <summary>
    /// Initiates a sign-in flow.
    /// </summary>
    public const string SignIn = "signin";

    /// <summary>
    /// Initiates a phone call.
    /// </summary>
    public const string Call = "call";
}
