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
    public Func<IDictionary<string, string>> HeadersFactory { get; set; } = () => new Dictionary<string, string>();

    /// <summary>
    /// Continue if server is unavailable
    /// </summary>
    public bool SkipIfUnavailable { get; set; } = true;

    /// <summary>
    /// Override default cache timeout of 1 day
    /// </summary>
    public int? RefetchTimeoutMs { get; set; }
}

public enum McpClientTransport
{
    StreamableHttp,
    Sse
}
