// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Bot.Apps.Schema;


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
public class InvokeResponse(int status, object? body = null)
{
    /// <summary>
    /// Status code of the response.
    /// </summary>
    [JsonPropertyName("status")]
    public int Status { get; set; } = status;

    /// <summary>
    /// Gets or sets the response body.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Body { get; set; } = body;
}

/// <summary>
/// Represents a strongly-typed response returned from an invocation handler.
/// </summary>
/// <remarks>
/// The strongly-typed Body property provides compile-time type safety while maintaining a single storage location
/// through the base class. Both the typed and untyped Body properties access the same underlying body.
/// </remarks>
/// <typeparam name="TBody">The type of the response body.</typeparam>
/// <param name="status">The HTTP status code indicating the result of the invoke operation (e.g., 200 for success).</param>
/// <param name="body">Optional strongly-typed response payload that will be serialized and sent to the client.</param>
public class InvokeResponse<TBody>(int status, TBody? body = default) : InvokeResponse(status, body) where TBody : notnull
{
    /// <summary>
    /// Gets or sets the strongly-typed response body.
    /// This property shadows the base class Body property but uses the same underlying storage,
    /// ensuring no synchronization issues between typed and untyped access.
    /// </summary>
    public new TBody? Body
    {
        get => (TBody?)base.Body;
        set => base.Body = value;
    }
}
