// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.Api.Clients;

/// <summary>
/// Options for Apps API requests that need per-call settings beyond the required method parameters.
/// </summary>
public readonly record struct RequestOptions
{
    /// <summary>
    /// Gets the agentic identity to authenticate as for this request.
    /// </summary>
    public AgenticIdentity? AgenticIdentity { get; init; }
}
