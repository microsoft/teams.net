// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace A365Mcp;

/// <summary>
/// Configuration options for the <see cref="Agent"/>, including
/// the MCP server endpoints to connect to.
/// </summary>
internal sealed class AgentOptions
{
    public const string SectionName = "Agent";

    /// <summary>
    /// The MCP server URLs the agent connects to for tool discovery.
    /// </summary>
    public string[] McpServerUrls { get; set; } =
    [
        "https://agent365.svc.cloud.microsoft/agents/servers/mcp_TeamsServer",
        "https://agent365.svc.cloud.microsoft/agents/servers/mcp_MailTools",
        "https://agent365.svc.cloud.microsoft/agents/servers/mcp_CalendarTools",
        "https://agent365.svc.cloud.microsoft/agents/servers/mcp_MeServer",
    ];
}
