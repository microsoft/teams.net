using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api;

/// <summary>
/// An interface representing FileConsentCard.
/// File consent card attachment.
/// </summary>
public class FileConsentCard
{
    /// <summary>
    /// File description.
    /// </summary>
    [JsonPropertyName("description")]
    [JsonPropertyOrder(0)]
    public string? Description { get; set; }

    /// <summary>
    /// Size of the file to be uploaded in Bytes.
    /// </summary>
    [JsonPropertyName("sizeInBytes")]
    [JsonPropertyOrder(1)]
    public int? SizeInBytes { get; set; }

    /// <summary>
    /// Context sent back to the Bot if user
    /// consented to upload. This is free flow schema and is sent back in Value
    /// field of Activity.
    /// </summary>
    [JsonPropertyName("acceptContext")]
    [JsonPropertyOrder(2)]
    public object? AcceptContext { get; set; }

    /// <summary>
    /// Context sent back to the Bot if user
    /// declined. This is free flow schema and is sent back in Value field of
    /// Activity.
    /// </summary>
    [JsonPropertyName("declineContext")]
    [JsonPropertyOrder(3)]
    public object? DeclineContext { get; set; }
}

/// <summary>
/// An interface representing FileConsentCardResponse.
/// Represents the value of the invoke activity sent when the user acts on a
/// file consent card
/// </summary>
public class FileConsentCardResponse
{
    /// <summary>
    /// The action the user took.
    /// </summary>
    [JsonPropertyName("action")]
    [JsonPropertyOrder(0)]
    public required Action Action { get; set; }

    /// <summary>
    /// The context associated with the action.
    /// </summary>
    [JsonPropertyName("context")]
    [JsonPropertyOrder(1)]
    public object? Context { get; set; }

    /// <summary>
    /// If the user accepted the file,
    /// contains information about the file to be uploaded.
    /// </summary>
    [JsonPropertyName("uploadInfo")]
    [JsonPropertyOrder(2)]
    public FileUploadInfo? UploadInfo { get; set; }
}