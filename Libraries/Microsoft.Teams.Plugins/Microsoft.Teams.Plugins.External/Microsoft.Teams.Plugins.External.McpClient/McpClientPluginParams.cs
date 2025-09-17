namespace Microsoft.Teams.Plugins.External.McpClient;

public class McpClientPluginParams
{
    /// <summary>
    /// Transport protocol for MCP connection
    /// </summary>
    public McpClientTransport Transport { get; set; } = McpClientTransport.StreamableHttp;

    /// <summary>
    /// Pre-defined tools (skips server fetch)
    /// </summary>
    public IList<McpToolDetails>? AvailableTools { get; set; }

    /// <summary>
    /// Additional headers to include in the MCP requests
    /// </summary>
    public IDictionary<string, string>? Headers { get; set; }

    /// <summary>
    /// Continue if server is unavailable
    /// </summary>
    public bool SkipIfUnavailable { get; set; } = false;

    /// <summary>
    /// Override default cache timeout
    /// </summary>
    public int? RefetchTimeoutMs { get; set; }
}

public enum McpClientTransport
{
    StreamableHttp,
    Sse
}
