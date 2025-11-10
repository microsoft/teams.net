using Microsoft.Extensions.Logging;

namespace Microsoft.Teams.Plugins.External.McpClient;

public class McpClientPluginOptions
{
    public static readonly int REFETCH_TIMEOUT_MS = 24 * 60 * 60 * 1000;

    /// <summary>
    /// Plugin identifier
    /// </summary>
    public string Name { get; set; } = "McpClient";

    /// <summary>
    /// Plugin version
    /// </summary>
    public string Version { get; set; } = "0.0.0";

    /// <summary>
    /// How long to cache tools before refetching (default: 1 day)
    /// </summary>
    public int RefetchTimeoutMs { get; set; } = REFETCH_TIMEOUT_MS;

    /// <summary>
    /// Cache for storing fetched tools
    public IDictionary<string, McpCachedValue>? Cache { get; set; }

    /// <summary>
    /// Logger instance
    /// </summary>
    public ILoggerFactory? LoggerFactory { get; set; }
}