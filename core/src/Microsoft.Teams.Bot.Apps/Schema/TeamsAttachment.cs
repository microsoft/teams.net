// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.Schema;

/// <summary>
/// Teams attachment content types.
/// </summary>
public static class AttachmentContentType
{
    /// <summary>
    /// Adaptive Card content type.
    /// </summary>
    public const string AdaptiveCard = "application/vnd.microsoft.card.adaptive";

    /// <summary>
    /// Hero Card content type.
    /// </summary>
    public const string HeroCard = "application/vnd.microsoft.card.hero";

    /// <summary>
    /// Thumbnail Card content type.
    /// </summary>
    public const string ThumbnailCard = "application/vnd.microsoft.card.thumbnail";

    /// <summary>
    /// Office 365 Connector Card content type.
    /// </summary>
    public const string O365ConnectorCard = "application/vnd.microsoft.teams.card.o365connector";

    /// <summary>
    /// File consent card content type.
    /// </summary>
    public const string FileConsentCard = "application/vnd.microsoft.teams.card.file.consent";

    /// <summary>
    /// File info card content type.
    /// </summary>
    public const string FileInfoCard = "application/vnd.microsoft.teams.card.file.info";

    //TODO : verify these
    /*
    /// <summary>
    /// Receipt Card content type.
    /// </summary>
    public const string ReceiptCard = "application/vnd.microsoft.card.receipt";

    /// <summary>
    /// Signin Card content type.
    /// </summary>
    public const string SigninCard = "application/vnd.microsoft.card.signin";

    /// <summary>
    /// Animation content type.
    /// </summary>
    public const string Animation = "application/vnd.microsoft.card.animation";

    /// <summary>
    /// Audio content type.
    /// </summary>
    public const string Audio = "application/vnd.microsoft.card.audio";

    /// <summary>
    /// Video content type.
    /// </summary>
    public const string Video = "application/vnd.microsoft.card.video";
    */
}

/// <summary>
/// Attachment layout types.
/// </summary>
public static class TeamsAttachmentLayout
{
    /// <summary>
    /// List layout - displays attachments in a vertical list.
    /// </summary>
    public const string List = "list";

    /// <summary>
    /// Grid layout - displays attachments in a grid.
    /// </summary>
    public const string Grid = "grid";

    /// <summary>
    /// Carousel layout - displays attachments in a horizontal carousel.
    /// </summary>
    public const string Carousel = "carousel";
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
    static internal IList<TeamsAttachment> FromJArray(JsonArray? jsonArray)
    {
        if (jsonArray is null)
        {
            return [];
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
    [JsonPropertyName("contentType")] public string ContentType { get; set; } = string.Empty;

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
