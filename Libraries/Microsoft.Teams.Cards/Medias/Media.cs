using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

public partial class CardType : StringEnum
{
    public static readonly CardType Media = new("Media");
    public bool IsMedia => Media.Equals(Value);
}

/// <summary>
/// Defines a source for a Media element
/// </summary>
public class MediaSource
{
    /// <summary>
    /// URL to media. Supports data URI in version 1.2+
    /// </summary>
    [JsonPropertyName("url")]
    [JsonPropertyOrder(0)]
    public required string Url { get; set; }

    /// <summary>
    /// Mime type of associated media (e.g. "video/mp4"). For YouTube and other Web video URLs, mimeType can be omitted.
    /// </summary>
    [JsonPropertyName("mimeType")]
    [JsonPropertyOrder(1)]
    public string? MimeType { get; set; }
}

/// <summary>
/// Defines a source for captions
/// </summary>
public class CaptionSource
{
    /// <summary>
    /// Label of this caption to show to the user.
    /// </summary>
    [JsonPropertyName("label")]
    [JsonPropertyOrder(0)]
    public required string Label { get; set; }

    /// <summary>
    /// URL to captions.
    /// </summary>
    [JsonPropertyName("url")]
    [JsonPropertyOrder(1)]
    public required string Url { get; set; }

    /// <summary>
    /// Mime type of associated caption file (e.g. "vtt"). For rendering in JavaScript, only "vtt" is supported, for rendering in UWP, "vtt" and "srt" are supported.
    /// </summary>
    [JsonPropertyName("mimeType")]
    [JsonPropertyOrder(2)]
    public required string MimeType { get; set; }
}

/// <summary>
/// Displays a media player for audio or video content.
/// </summary>
public class Media(params MediaSource[] sources) : Element(CardType.Media)
{
    /// <summary>
    /// URL of an image to display before playing. Supports data URI in version 1.2+. If poster is omitted, the Media element will either use a default poster (controlled by the host application) or will attempt to automatically pull the poster from the target video service when the source URL points to a video from a Web provider such as YouTube.
    /// </summary>
    [JsonPropertyName("poster")]
    [JsonPropertyOrder(13)]
    public string? Poster { get; set; }

    /// <summary>
    /// Alternate text describing the audio or video.
    /// </summary>
    [JsonPropertyName("altText")]
    [JsonPropertyOrder(14)]
    public string? AltText { get; set; }

    /// <summary>
    /// Array of media sources to attempt to play.
    /// </summary>
    [JsonPropertyName("sources")]
    [JsonPropertyOrder(12)]
    public IList<MediaSource> Sources { get; set; } = sources;

    /// <summary>
    /// Array of captions sources for the media element to provide.
    /// </summary>
    [JsonPropertyName("captionSources")]
    [JsonPropertyOrder(15)]
    public IList<CaptionSource>? CaptionSources { get; set; }

    public Media WithPoster(string value)
    {
        Poster = value;
        return this;
    }

    public Media WithAltText(string value)
    {
        AltText = value;
        return this;
    }

    public Media AddSources(params MediaSource[] value)
    {
        foreach (var source in value)
        {
            Sources.Add(source);
        }

        return this;
    }

    public Media AddCaptionSources(params CaptionSource[] value)
    {
        CaptionSources ??= [];

        foreach (var source in value)
        {
            CaptionSources.Add(source);
        }

        return this;
    }
}