// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Bot.Core;

/// <summary>
/// Represents the response returned from an invocation handler.
/// </summary>
/// <remarks>
/// Creates a new instance of the <see cref="InvokeResponse"/> class with the specified status code and optional body.
/// </remarks>
/// <param name="status"></param>
/// <param name="body"></param>
public class InvokeResponse(int status, object? body = null)
{

    /// <summary>
    /// Id of the response
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Status code of the response.
    /// </summary>
    [JsonPropertyName("status")]
    public int Status { get; set; } = status;

    // TODO: This is strange - Should this be Value or Body?
    /// <summary>
    /// Gets or sets the message body content.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Body { get; set; } = body;

    // TODO: Get confirmation that this should be "Type"
    // This particular type should be for AC responses
    /// <summary>
    /// Gets or Sets the Type
    /// </summary>
    [JsonPropertyName("type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Type { get; set; }
}
