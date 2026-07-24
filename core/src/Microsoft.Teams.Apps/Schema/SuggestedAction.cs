// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Teams.Apps.Utils;

namespace Microsoft.Teams.Apps.Schema;

/// <summary>
/// Represents a clickable action
/// </summary>
public class SuggestedAction
{
    /// <summary>
    /// Default constructor for JSON deserialization.
    /// </summary>
    public SuggestedAction()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SuggestedAction"/> class with the specified type, title, and value.
    /// </summary>
    /// <param name="type">The type of action. See <see cref="ActionTypes"/> for common values.</param>
    /// <param name="title">The text description displayed on the button.</param>
    /// <param name="value">The value sent when the button is clicked. Accepts strings, anonymous objects, or <see cref="JsonNode"/> instances. Defaults to <paramref name="title"/> when not specified.</param>
    public SuggestedAction(ActionType type, string title, object? value = null)
    {
        Type = type;
        Title = title;
        Value = value switch
        {
            null => JsonValue.Create(title),
            JsonNode n => n,
            _ => JsonSerializer.SerializeToNode(value)
        };
    }

    /// <summary>
    /// Gets or sets the type of action implemented by this button.
    /// See <see cref="ActionTypes"/> for common values.
    /// </summary>
    [JsonPropertyName("type")]
    public ActionType? Type { get; set; }

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
    public JsonNode? Value { get; set; }

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
/// String enum for card action types.
/// </summary>
[JsonConverter(typeof(StringEnumJsonConverter<ActionType>))]
public class ActionType(string value) : StringEnum(value)
{
    /// <summary>Gets the <c>openUrl</c> action type.</summary>
    public static readonly ActionType OpenUrl = new("openUrl");
    /// <summary>Gets the <c>imBack</c> action type.</summary>
    public static readonly ActionType IMBack = new("imBack");
    /// <summary>Gets the <c>postBack</c> action type.</summary>
    public static readonly ActionType PostBack = new("postBack");
    /// <summary>Gets the <c>playAudio</c> action type.</summary>
    public static readonly ActionType PlayAudio = new("playAudio");
    /// <summary>Gets the <c>playVideo</c> action type.</summary>
    public static readonly ActionType PlayVideo = new("playVideo");
    /// <summary>Gets the <c>showImage</c> action type.</summary>
    public static readonly ActionType ShowImage = new("showImage");
    /// <summary>Gets the <c>downloadFile</c> action type.</summary>
    public static readonly ActionType DownloadFile = new("downloadFile");
    /// <summary>Gets the <c>signin</c> action type.</summary>
    public static readonly ActionType SignIn = new("signin");
    /// <summary>Gets the <c>call</c> action type.</summary>
    public static readonly ActionType Call = new("call");
    /// <summary>Gets the experimental <c>Action.Submit</c> action type.</summary>
    public static readonly ActionType Submit = new("Action.Submit");

}

/// <summary>
/// Common card action type values.
/// </summary>
public static class ActionTypes
{
    /// <summary>Gets the <c>openUrl</c> action type.</summary>
    public static ActionType OpenUrl => ActionType.OpenUrl;

    /// <summary>Gets the <c>imBack</c> action type.</summary>
    public static ActionType IMBack => ActionType.IMBack;

    /// <summary>Gets the <c>postBack</c> action type.</summary>
    public static ActionType PostBack => ActionType.PostBack;

    /// <summary>Gets the <c>playAudio</c> action type.</summary>
    public static ActionType PlayAudio => ActionType.PlayAudio;

    /// <summary>Gets the <c>playVideo</c> action type.</summary>
    public static ActionType PlayVideo => ActionType.PlayVideo;

    /// <summary>Gets the <c>showImage</c> action type.</summary>
    public static ActionType ShowImage => ActionType.ShowImage;

    /// <summary>Gets the <c>downloadFile</c> action type.</summary>
    public static ActionType DownloadFile => ActionType.DownloadFile;

    /// <summary>Gets the <c>signin</c> action type.</summary>
    public static ActionType SignIn => ActionType.SignIn;

    /// <summary>Gets the <c>call</c> action type.</summary>
    public static ActionType Call => ActionType.Call;

    /// <summary>Gets the experimental <c>Action.Submit</c> action type.</summary>
    [System.Diagnostics.CodeAnalysis.Experimental("ExperimentalTeamsSuggestedAction")]
    public static ActionType Submit => ActionType.Submit;
}
