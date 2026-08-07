// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Apps.Schema;

/// <summary>
/// The content of a <c>file.download.info</c> attachment, describing an uploaded file received in a personal (1:1) chat. The file is fetched from the short-lived, pre-authorized <see cref="DownloadUrl"/> with a plain GET (no bearer token).
/// </summary>
public class FileDownloadInfo
{
    /// <summary>
    /// Pre-authorized, short-lived URL the file can be fetched from with a plain GET (no bearer token).
    /// </summary>
    [JsonPropertyName("downloadUrl")]
    public Uri? DownloadUrl { get; set; }

    /// <summary>
    /// The OneDrive/ODSP drive-item id for the file. This is the storage-specific file identity a Graph fetch keys off.
    /// </summary>
    [JsonPropertyName("uniqueId")]
    public string? UniqueId { get; set; }

    /// <summary>
    /// Type of file (extension, e.g. <c>pdf</c>, <c>docx</c>).
    /// </summary>
    [JsonPropertyName("fileType")]
    public string? FileType { get; set; }

    /// <summary>
    /// A server-assigned version tag identifying this version of the file's contents, for detecting whether the file changed between reads.
    /// Read-only; populated when Teams provides it with the file.
    /// </summary>
    [JsonPropertyName("etag")]
    public string? Etag { get; set; }
}
