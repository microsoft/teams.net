// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Teams.Apps.Schema.Entities;

namespace Microsoft.Teams.Apps.Schema;

/// <summary>
/// Represents an outbound message activity constructed by a builder and sent by the API clients.
/// </summary>
public class MessageActivityInput : TeamsActivityInput
{
    /// <summary>
    /// Default constructor.
    /// </summary>
    [JsonConstructor]
    internal MessageActivityInput() : base(TeamsActivityTypes.Message)
    {
    }

    /// <summary>
    /// Gets or sets the text content of the message.
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the text format. See <see cref="TextFormats"/> for common values.
    /// </summary>
    [JsonPropertyName("textFormat")]
    public string? TextFormat { get; set; }

    /// <summary>
    /// Gets or sets the attachments for the message.
    /// </summary>
    [JsonPropertyName("attachments")]
    public IList<TeamsAttachment>? Attachments { get; set; }

    /// <summary>
    /// Gets or sets the attachment layout.
    /// </summary>
    [JsonPropertyName("attachmentLayout")]
    public string? AttachmentLayout { get; set; }

    /// <summary>
    /// Serializes the current activity to a JSON string using the outbound message serializer context.
    /// </summary>
    /// <returns>A JSON string representation of the activity.</returns>
    public override string ToJson()
        => JsonSerializer.Serialize(this, TeamsActivityInputJsonContext.Default.MessageActivityInput);

    /// <summary>
    /// Creates a new <see cref="MessageActivityInputBuilder"/> to construct an outbound message activity.
    /// </summary>
    /// <returns>A new <see cref="MessageActivityInputBuilder"/> instance.</returns>
    public static new MessageActivityInputBuilder CreateBuilder() => new();
}
