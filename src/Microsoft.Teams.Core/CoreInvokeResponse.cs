// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Core;

/// <summary>
/// The transport-neutral result of processing an invoke activity: a status code and an optional body.
/// </summary>
/// <remarks>
/// Returned by <see cref="BotApplication.ProcessAsync(Schema.CoreActivity, System.Security.Claims.ClaimsPrincipal?, string?, CancellationToken)"/>
/// so that the caller (the HTTP endpoint, or another transport) decides how to deliver it.
/// </remarks>
/// <param name="status">The HTTP-style status code of the invoke result (for example, 200).</param>
/// <param name="body">Optional payload, serialized as JSON by the transport that delivers the response.</param>
public sealed class CoreInvokeResponse(int status, object? body = null)
{
    /// <summary>
    /// Gets the HTTP-style status code of the invoke result.
    /// </summary>
    public int Status { get; } = status;

    /// <summary>
    /// Gets the optional payload of the invoke result.
    /// </summary>
    public object? Body { get; } = body;
}
