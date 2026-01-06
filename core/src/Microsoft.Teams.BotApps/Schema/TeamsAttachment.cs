// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Bot.Core.Schema;

namespace Microsoft.Teams.BotApps.Schema;


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
            attachments.Add(JsonSerializer.Deserialize<TeamsAttachment>(item)!);
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
#pragma warning disable CA2227 // Collection properties should be read only
    [JsonExtensionData] public ExtendedPropertiesDictionary Properties { get; set; } = [];
#pragma warning restore CA2227 // Collection properties should be read only
}
