// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Core.Http;

using CustomHeaders = Dictionary<string, string>;

/// <summary>
/// Options for configuring a bot HTTP request.
/// </summary>
public record BotRequestOptions
{
    /// <summary>
    /// Gets the per-request properties to stamp onto the outbound request's options, where a
    /// <see cref="System.Net.Http.DelegatingHandler"/> can read them. See <see cref="BotRequestProperties"/>
    /// for well-known keys (agentic identity, bot app id) and helpers.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? RequestProperties { get; init; }

    /// <summary>
    /// Gets the custom headers to include in the request.
    /// </summary>
    public CustomHeaders? CustomHeaders { get; init; }

    /// <summary>
    /// Gets a value indicating whether to return null instead of throwing on 404 responses.
    /// </summary>
    public bool ReturnNullOnNotFound { get; init; }

    /// <summary>
    /// Gets a description of the operation for logging and error messages.
    /// </summary>
    public string? OperationDescription { get; init; }
}
