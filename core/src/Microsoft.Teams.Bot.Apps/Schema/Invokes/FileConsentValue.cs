// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Bot.Apps.Schema;

/// <summary>
/// Represents the value of the invoke activity sent when the user acts on a
/// file consent card.
/// </summary>
public class FileConsentValue
{
    /// <summary>
    /// The type of file consent activity. Typically "fileUpload".
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// The action the user took. Possible values: 'accept', 'decline'.
    /// </summary>
    [JsonPropertyName("action")]
    public string? Action { get; set; }

    /// <summary>
    /// The context associated with the action.
    /// </summary>
    [JsonPropertyName("context")]
    public object? Context { get; set; }

    /// <summary>
    /// If the user accepted the file,
    /// contains information about the file to be uploaded.
    /// </summary>
    [JsonPropertyName("uploadInfo")]
    public FileUploadInfo? UploadInfo { get; set; }
}

/// <summary>
/// File upload info for accepted file consent.
/// </summary>
public class FileUploadInfo
{
    /// <summary>
    /// Name of the file.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// URL to upload file content.
    /// </summary>
    [JsonPropertyName("uploadUrl")]
    public Uri? UploadUrl { get; set; }

    /// <summary>
    /// URL to file content after upload.
    /// </summary>
    [JsonPropertyName("contentUrl")]
    public Uri? ContentUrl { get; set; }

    /// <summary>
    /// Unique ID for the file.
    /// </summary>
    [JsonPropertyName("uniqueId")]
    public string? UniqueId { get; set; }

    /// <summary>
    /// Type of the file.
    /// </summary>
    [JsonPropertyName("fileType")]
    public string? FileType { get; set; }
}
