using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api;

/// <summary>
/// An interface representing FileUploadInfo.
/// Information about the file to be uploaded.
/// </summary>
public class FileUploadInfo
{
    /// <summary>
    /// Name of the file.
    /// </summary>
    [JsonPropertyName("name")]
    [JsonPropertyOrder(0)]
    public string? Name { get; set; }

    /// <summary>
    /// URL to an upload session that the bot can use
    /// to set the file contents.
    /// </summary>
    [JsonPropertyName("uploadUrl")]
    [JsonPropertyOrder(1)]
    public string? UploadUrl { get; set; }

    /// <summary>
    /// URL to file.
    /// </summary>
    [JsonPropertyName("contentUrl")]
    [JsonPropertyOrder(2)]
    public string? ContentUrl { get; set; }

    /// <summary>
    /// ID that uniquely identifies the file.
    /// </summary>
    [JsonPropertyName("uniqueId")]
    [JsonPropertyOrder(3)]
    public string? UniqueId { get; set; }

    /// <summary>
    /// Type of the file.
    /// </summary>
    [JsonPropertyName("fileType")]
    [JsonPropertyOrder(4)]
    public string? FileType { get; set; }
}