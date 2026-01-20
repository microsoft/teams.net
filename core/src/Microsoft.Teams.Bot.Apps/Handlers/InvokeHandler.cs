// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Bot.Apps.Handlers;

/// <summary>
/// Represents a method that handles an invocation request and returns a response asynchronously.
/// </summary>
/// <param name="context">The context for the invocation, containing request data and metadata required to process the operation. Cannot be
/// null.</param>
/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation. The default value is <see
/// cref="CancellationToken.None"/>.</param>
/// <returns>A task that represents the asynchronous operation. The task result contains the response to the invocation.</returns>
public delegate Task<CoreInvokeResponse> InvokeHandler(Context context, CancellationToken cancellationToken = default);



/// <summary>
/// Represents the response returned from an invocation handler, typically used for Adaptive Card actions and task module operations.
/// </summary>
/// <remarks>
/// This class encapsulates the HTTP-style response sent back to Teams when handling invoke activities.
/// Common status codes include 200 for success, 400 for bad request, and 500 for errors.
/// The Body property contains the response payload, which is serialized to JSON and returned to the client.
/// </remarks>
/// <param name="status">The HTTP status code indicating the result of the invoke operation (e.g., 200 for success).</param>
/// <param name="body">Optional response payload that will be serialized and sent to the client.</param>
public class CoreInvokeResponse(int status, object? body = null)
{
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
