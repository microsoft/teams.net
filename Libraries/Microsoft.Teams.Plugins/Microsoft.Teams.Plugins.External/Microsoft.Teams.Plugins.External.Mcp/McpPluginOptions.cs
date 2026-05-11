// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;

namespace Microsoft.Teams.Plugins.External.Mcp;

public class McpPluginOptions
{
    /// <summary>
    /// Optional callback that gates inbound MCP requests. Return <c>true</c> to
    /// allow the request; return <c>false</c> or throw to reject with HTTP 401.
    /// When unset, all MCP requests are accepted and a warning is emitted at
    /// plugin startup.
    /// </summary>
    public Func<HttpContext, Task<bool>>? RequireAuth { get; set; }
}