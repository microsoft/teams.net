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
    /// Status code of the response.
    /// </summary>
    [JsonPropertyName("status")]
    public int Status { get; set; } = status;

    /// <summary>
    /// Gets or sets the message body content.
    /// </summary>
    [JsonPropertyName("value")]
    public object? Body { get; set; } = body;

    /// <summary>
    /// Gets or Sets the Type
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }
}
