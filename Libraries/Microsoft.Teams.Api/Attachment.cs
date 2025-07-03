// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api;

/// <summary>
/// Attachment
/// </summary>
public class Attachment
{
    /// <summary>
    /// The id of the attachment.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonPropertyOrder(0)]
    public string? Id { get; set; }

    /// <summary>
    /// (OPTIONAL) The name of the attachment
    /// </summary>
    [JsonPropertyName("name")]
    [JsonPropertyOrder(1)]
    public string? Name { get; set; }

    /// <summary>
    /// mimetype/Contenttype for the file
    /// </summary>
    [JsonPropertyName("contentType")]
    [JsonPropertyOrder(2)]
    public ContentType ContentType { get; set; }

    /// <summary>
    /// Content Url
    /// </summary>
    [JsonPropertyName("contentUrl")]
    [JsonPropertyOrder(3)]
    public string? ContentUrl { get; set; }

    /// <summary>
    /// Embedded content
    /// </summary>
    [JsonPropertyName("content")]
    [JsonPropertyOrder(4)]
    public object? Content { get; set; }

    /// <summary>
    /// (OPTIONAL) Thumbnail associated with attachment
    /// </summary>
    [JsonPropertyName("thumbnailUrl")]
    [JsonPropertyOrder(5)]
    public string? ThumbnailUrl { get; set; }

    [JsonConstructor]
    public Attachment(object? content = null)
    {
        Content = content;
    }

    public Attachment(string contentType, object? content = null)
    {
        ContentType = new(contentType);
        Content = content;
    }

    public Attachment(ContentType contentType, object? content = null)
    {
        ContentType = contentType;
        Content = content;
    }

    public Attachment(Teams.Cards.AdaptiveCard card)
    {
        ContentType = ContentType.AdaptiveCard;
        Content = card;
    }

    public Attachment(Cards.AnimationCard card)
    {
        ContentType = ContentType.AnimationCard;
        Content = card;
    }

    public Attachment(Cards.AudioCard card)
    {
        ContentType = ContentType.AudioCard;
        Content = card;
    }

    public Attachment(Cards.HeroCard card)
    {
        ContentType = ContentType.HeroCard;
        Content = card;
    }

    public Attachment(Cards.OAuthCard card)
    {
        ContentType = ContentType.OAuthCard;
        Content = card;
    }

    public Attachment(Cards.SignInCard card)
    {
        ContentType = ContentType.SignInCard;
        Content = card;
    }

    public Attachment(Cards.ThumbnailCard card)
    {
        ContentType = ContentType.ThumbnailCard;
        Content = card;
    }

    public Attachment(Cards.VideoCard card)
    {
        ContentType = ContentType.VideoCard;
        Content = card;
    }

    /// <summary>
    /// Attachment Layout
    /// </summary>
    public class Layout(string value) : StringEnum(value)
    {
        public static readonly Layout List = new("list");
        public bool IsList => List.Equals(Value);

        public static readonly Layout Carousel = new("carousel");
        public bool IsCarousel => Carousel.Equals(Value);
    }
}