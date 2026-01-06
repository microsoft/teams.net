// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.BotApps.Handlers;

/// <summary>
/// Represents a method that handles an invocation request and returns a response asynchronously.
/// </summary>
/// <param name="context">The context for the invocation, containing request data and metadata required to process the operation. Cannot be
/// null.</param>
/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation. The default value is <see
/// cref="CancellationToken.None"/>.</param>
/// <returns>A task that represents the asynchronous operation. The task result contains the response to the invocation.</returns>
public delegate Task<InvokeResponse> InvokeHandler(Context context, CancellationToken cancellationToken = default);


/// <summary>
/// Represents the response returned from an invocation handler.
/// </summary>
public class InvokeResponse
{
    /// <summary>
    /// Status code of the response.
    /// </summary>
    [JsonPropertyName("status")]
    public int Status { get; set; }

    /// <summary>
    /// Gets or sets the message body content.
    /// </summary>
    [JsonPropertyName("body")]
    public object? Body { get; set; }

    /// <summary>
    /// Gets or Sets the Type
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Creates a new instance of the <see cref="InvokeResponse"/> class with the specified status code and optional body.
    /// </summary>
    /// <param name="status"></param>
    /// <param name="body"></param>
    public InvokeResponse(int status, object? body = null)
    {
        Status = status;
        Body = body;
    }
}
