namespace Microsoft.Teams.Plugins.External.McpClient;

/// <summary>
/// Cached value for MCP server data.
/// 
/// Stores fetched tool information from MCP servers along with metadata for cache management and expiration handling.
/// </summary>
public class McpCachedValue
{
    /// <summary>
    /// Transport protocol used for this server
    /// </summary>
    public McpClientTransport? Transport { get; set; }

    /// <summary>
    /// Cached tools from the server
    /// </summary>
    public IList<McpToolDetails>? AvailableTools { get; set; }

    /// <summary>
    /// Timestamp when tools were last fetched (UTC)
    /// </summary>
    public DateTimeOffset? LastFetched { get; set; }
}