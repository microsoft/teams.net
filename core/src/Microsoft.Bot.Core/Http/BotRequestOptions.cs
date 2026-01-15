// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Core.Schema;

namespace Microsoft.Bot.Core.Http;

using CustomHeaders = Dictionary<string, string>;

/// <summary>
/// Options for configuring a bot HTTP request.
/// </summary>
public record BotRequestOptions
{
    /// <summary>
    /// Gets the agentic identity for authentication.
    /// </summary>
    public AgenticIdentity? AgenticIdentity { get; init; }

    /// <summary>
    /// Gets the custom headers to include in the request.
    /// These headers override default headers if the same key exists.
    /// </summary>
    public CustomHeaders? CustomHeaders { get; init; }

    /// <summary>
    /// Gets the default custom headers that will be included in all requests.
    /// </summary>
    public CustomHeaders? DefaultHeaders { get; init; }

    /// <summary>
    /// Gets a value indicating whether to return null instead of throwing on 404 responses.
    /// </summary>
    public bool ReturnNullOnNotFound { get; init; }

    /// <summary>
    /// Gets a description of the operation for logging and error messages.
    /// </summary>
    public string? OperationDescription { get; init; }
}
