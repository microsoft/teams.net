// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Core.Schema;
using ModelContextProtocol.Client;

namespace A365Mcp;

/// <summary>
/// Factory for creating authenticated <see cref="McpClient"/> instances
/// bound to a specific user's agentic identity.
/// </summary>
internal interface IMcpClientFactory
{
    Task<McpClient> CreateClientAsync(AgenticIdentity agenticIdentity, CancellationToken cancellationToken = default);
}
