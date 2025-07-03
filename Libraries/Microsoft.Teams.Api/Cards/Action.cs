// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Cards;

[JsonConverter(typeof(JsonConverter<ActionType>))]
public class ActionType(string value) : StringEnum(value)
{
    public static readonly ActionType OpenUrl = new("openUrl");
    public bool IsOpenUrl => OpenUrl.Equals(Value);

    public static readonly ActionType IMBack = new("imBack");
    public bool IsIMBack => IMBack.Equals(Value);

    public static readonly ActionType PostBack = new("postBack");
    public bool IsPostBack => PostBack.Equals(Value);

    public static readonly ActionType PlayAudio = new("playAudio");
    public bool IsPlayAudio => PlayAudio.Equals(Value);

    public static readonly ActionType PlayVideo = new("playVideo");
    public bool IsPlayVideo => PlayVideo.Equals(Value);

    public static readonly ActionType ShowImage = new("showImage");
    public bool IsShowImage => ShowImage.Equals(Value);

    public static readonly ActionType DownloadFile = new("downloadFile");
    public bool IsDownloadFile => DownloadFile.Equals(Value);

    public static readonly ActionType SignIn = new("signin");
    public bool IsSignIn => SignIn.Equals(Value);

    public static readonly ActionType Call = new("call");
    public bool IsCall => Call.Equals(Value);
}

public class Action(ActionType type)
{
    /// <summary>
    /// The type of action implemented by this button. Possible values include: 'openUrl', 'imBack',
    /// 'postBack', 'playAudio', 'playVideo', 'showImage', 'downloadFile', 'signin', 'call',
    /// 'messageBack'
    /// </summary>
    [JsonPropertyName("type")]
    [JsonPropertyOrder(0)]
    public ActionType Type { get; set; } = type;

    /// <summary>
    /// Text description which appears on the button
    /// </summary>
    [JsonPropertyName("title")]
    [JsonPropertyOrder(1)]
    public required string Title { get; set; }

    /// <summary>
    /// Image URL which will appear on the button, next to text label
    /// </summary>
    [JsonPropertyName("image")]
    [JsonPropertyOrder(2)]
    public string? Image { get; set; }

    /// <summary>
    /// Text for this action
    /// </summary>
    [JsonPropertyName("text")]
    [JsonPropertyOrder(3)]
    public string? Text { get; set; }

    /// <summary>
    /// (Optional) text to display in the chat feed if the button is clicked
    /// </summary>
    [JsonPropertyName("displayText")]
    [JsonPropertyOrder(4)]
    public string? DisplayText { get; set; }

    /// <summary>
    /// Supplementary parameter for action. Content of this property depends on the ActionType
    /// </summary>
    [JsonPropertyName("value")]
    [JsonPropertyOrder(5)]
    public object? Value { get; set; }

    /// <summary>
    /// Channel-specific data associated with this action
    /// </summary>
    [JsonPropertyName("channelData")]
    [JsonPropertyOrder(6)]
    public object? ChannelData { get; set; }

    /// <summary>
    /// Alternate image text to be used in place of the `image` field
    /// </summary>
    [JsonPropertyName("imageAltText")]
    [JsonPropertyOrder(7)]
    public string? ImageAltText { get; set; }
}