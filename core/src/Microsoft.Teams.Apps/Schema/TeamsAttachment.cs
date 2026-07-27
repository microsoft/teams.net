// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Teams.Core.Schema;
using Microsoft.Teams.Apps.Utils;

namespace Microsoft.Teams.Apps.Schema;

/// <summary>
/// String enum for Teams attachment content types.
/// </summary>
[JsonConverter(typeof(StringEnumJsonConverter<AttachmentContentType>))]
public class AttachmentContentType(string value) : StringEnum(value)
{
    /// <summary>Adaptive card content type.</summary>
    public static readonly AttachmentContentType AdaptiveCard = new("application/vnd.microsoft.card.adaptive");
    /// <summary>Hero card content type.</summary>
    public static readonly AttachmentContentType HeroCard = new("application/vnd.microsoft.card.hero");
    /// <summary>Thumbnail card content type.</summary>
    public static readonly AttachmentContentType ThumbnailCard = new("application/vnd.microsoft.card.thumbnail");
    /// <summary>O365 connector card content type.</summary>
    public static readonly AttachmentContentType O365ConnectorCard = new("application/vnd.microsoft.teams.card.o365connector");
    /// <summary>File consent card content type.</summary>
    public static readonly AttachmentContentType FileConsentCard = new("application/vnd.microsoft.teams.card.file.consent");
    /// <summary>File info card content type.</summary>
    public static readonly AttachmentContentType FileInfoCard = new("application/vnd.microsoft.teams.card.file.info");
    /// <summary>OAuth card content type.</summary>
    public static readonly AttachmentContentType OAuthCard = new("application/vnd.microsoft.card.oauth");

}

/// <summary>
/// Common Teams attachment content types.
/// </summary>
public static class AttachmentContentTypes
{
    /// <summary>Gets the adaptive card content type.</summary>
    public static AttachmentContentType AdaptiveCard => AttachmentContentType.AdaptiveCard;

    /// <summary>Gets the hero card content type.</summary>
    public static AttachmentContentType HeroCard => AttachmentContentType.HeroCard;

    /// <summary>Gets the thumbnail card content type.</summary>
    public static AttachmentContentType ThumbnailCard => AttachmentContentType.ThumbnailCard;

    /// <summary>Gets the O365 connector card content type.</summary>
    public static AttachmentContentType O365ConnectorCard => AttachmentContentType.O365ConnectorCard;

    /// <summary>Gets the file consent card content type.</summary>
    public static AttachmentContentType FileConsentCard => AttachmentContentType.FileConsentCard;

    /// <summary>Gets the file info card content type.</summary>
    public static AttachmentContentType FileInfoCard => AttachmentContentType.FileInfoCard;

    /// <summary>Gets the OAuth card content type.</summary>
    public static AttachmentContentType OAuthCard => AttachmentContentType.OAuthCard;
}

/// <summary>
/// String enum for attachment layouts.
/// </summary>
[JsonConverter(typeof(StringEnumJsonConverter<AttachmentLayoutType>))]
public class AttachmentLayoutType(string value) : StringEnum(value)
{
    /// <summary>List layout.</summary>
    public static readonly AttachmentLayoutType List = new("list");
    /// <summary>Grid layout.</summary>
    public static readonly AttachmentLayoutType Grid = new("grid");
    /// <summary>Carousel layout.</summary>
    public static readonly AttachmentLayoutType Carousel = new("carousel");

}

/// <summary>
/// Common Teams attachment layout values.
/// </summary>
public static class TeamsAttachmentLayouts
{
    /// <summary>Gets the list layout.</summary>
    public static AttachmentLayoutType List => AttachmentLayoutType.List;

    /// <summary>Gets the grid layout.</summary>
    public static AttachmentLayoutType Grid => AttachmentLayoutType.Grid;

    /// <summary>Gets the carousel layout.</summary>
    public static AttachmentLayoutType Carousel => AttachmentLayoutType.Carousel;
}

/// <summary>
/// Extension methods for TeamsAttachment.
/// </summary>
public static class TeamsAttachmentExtensions
{
    static internal JsonArray ToJsonArray(this IList<TeamsAttachment> attachments)
    {
        JsonArray jsonArray = [];
        foreach (TeamsAttachment attachment in attachments)
        {
            JsonNode jsonNode = JsonSerializer.SerializeToNode(attachment)!;
            jsonArray.Add(jsonNode);
        }
        return jsonArray;
    }
}

/// <summary>
/// Teams attachment model.
/// </summary>
public class TeamsAttachment
{
    static internal IList<TeamsAttachment>? FromJArray(JsonArray? jsonArray)
    {
        if (jsonArray is null)
        {
            return null;
        }
        List<TeamsAttachment> attachments = [];
        foreach (JsonNode? item in jsonArray)
        {
            attachments.Add(item.Deserialize<TeamsAttachment>()!);
        }
        return attachments;
    }

    /// <summary>
    /// Content of the attachment.
    /// </summary>
    [JsonPropertyName("contentType")] public AttachmentContentType ContentType { get; set; } = AttachmentContentType.AdaptiveCard;

    /// <summary>
    /// Content URL of the attachment.
    /// </summary>
    [JsonPropertyName("contentUrl")] public Uri? ContentUrl { get; set; }

    /// <summary>
    /// Content for the Attachment
    /// </summary>
    [JsonPropertyName("content")] public object? Content { get; set; }

    /// <summary>
    /// Gets or sets the name of the attachment.
    /// </summary>
    [JsonPropertyName("name")] public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the thumbnail URL of the attachment.
    /// </summary>
    [JsonPropertyName("thumbnailUrl")] public Uri? ThumbnailUrl { get; set; }

    /// <summary>
    /// Extension data for additional properties not explicitly defined by the type.
    /// </summary>
    [JsonExtensionData] public ExtendedPropertiesDictionary Properties { get; set; } = [];

    /// <summary>
    /// Creates a builder for constructing a <see cref="TeamsAttachment"/> instance.
    /// </summary>
    public static TeamsAttachmentBuilder CreateBuilder() => new();

    /// <summary>
    /// Creates a builder initialized with an existing <see cref="TeamsAttachment"/> instance.
    /// </summary>
    /// <param name="attachment">The attachment to wrap.</param>
    public static TeamsAttachmentBuilder CreateBuilder(TeamsAttachment attachment) => new(attachment);
}
